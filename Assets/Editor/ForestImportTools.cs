using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HangeulAdventure.EditorTools
{
    /// <summary>
    /// 시작의 숲 사이드뷰 아트(PixelLab 생성, Art/Forest) 임포트 설정: PPU 64, Point 필터, 무압축.
    /// 원본은 Tools/Scripts/forest_import.py가 ArtDrop에서 골라 크롭·스트립화해 넣는다 —
    /// 이 도구는 Unity 쪽 임포터 설정(피벗 포함)만 건다. 둘은 세트로 실행할 것.
    ///
    /// 피벗 규약 (승인 합성 forest_compose.py의 앵커를 그대로 옮긴 것):
    ///   지면 청크  = (0.5, 표면선)  -> SideWorld가 y=0.5(칸 윗변)에 그냥 놓으면 지면이 맞는다
    ///   단차·발판  = TopLeft        -> 칸 좌상단에 놓는다
    ///   사다리·프롭·나무 = BottomCenter -> 크롭된 실루엣의 발치가 곧 피벗 (합성의 tight()와 동일)
    ///   캐릭터     = (0.5, 발바닥선) -> 발 밑 중앙 기준점(기획 1.1장)에 그대로 놓는다
    /// </summary>
    public static class ForestImportTools
    {
        private const string ArtRoot = "Assets/Resources/Art/Forest";
        private const int Ppu = 64;
        private const int CharCell = 136;

        /// <summary>지면 청크 피벗 y = (160 − surface_median)/160. surface_median은 chunk_manifest.json 실측값.</summary>
        private static readonly Dictionary<string, float> GroundPivotY = new Dictionary<string, float>
        {
            { "ground_flat_02", 0.5687f }, // surf 69
            { "ground_flat_03", 0.6062f }, // surf 63
            { "ground_flat_04", 0.4813f }, // surf 83
        };

        /// <summary>캐릭터별 발바닥 피벗 y (idle 프레임 알파 최하단 실측 — forest_import.py가 출력).</summary>
        private static readonly Dictionary<string, float> CharFootY = new Dictionary<string, float>
        {
            { "SeonbiHanbok", 0.1176f }, // 발 = 아래에서 16px
            { "PlayerModern", 0.0956f }, // 발 = 아래에서 13px
        };

        private static readonly HashSet<string> TopLeft = new HashSet<string>
        {
            "step_mound_a", "step_mound_b", "platform_earth_mid", "platform_earth_cap_L", "bg_ridge",
        };

        [MenuItem("HangeulAdventure/아트 임포트 설정 적용 (Forest 사이드뷰)")]
        public static void Apply()
        {
            Debug.Log(ApplyInternal());
        }

        public static string ApplyInternal()
        {
            var report = new System.Text.StringBuilder();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null) continue;

                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                bool isChar = path.Contains("/Char/");

                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = Ppu;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp; // 하늘·안개는 늘려 쓰므로 반복 번짐 방지
                importer.spriteImportMode = isChar ? SpriteImportMode.Multiple : SpriteImportMode.Single;

                if (isChar)
                {
                    report.AppendLine(SliceChar(importer, path, name));
                    continue;
                }

                Vector2 pivot = SingleSpritePivot(path, name);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                report.AppendLine($"{path}: 단일 · 피벗 ({pivot.x:0.####}, {pivot.y:0.####})");
            }
            AssetDatabase.Refresh();
            return report.ToString();
        }

        private static Vector2 SingleSpritePivot(string path, string name)
        {
            if (GroundPivotY.TryGetValue(name, out float gy)) return new Vector2(0.5f, gy);
            if (TopLeft.Contains(name)) return new Vector2(0f, 1f);
            if (name == "sky_gradient") return new Vector2(0.5f, 0.5f);
            if (name == "fog_band") return new Vector2(0.5f, 1f);      // 띠 윗변 = world y 4.2에 건다
            return new Vector2(0.5f, 0f);                               // 프롭·나무·사다리·수풀 = 발치
        }

        /// <summary>캐릭터 가로 스트립을 136px 셀로 슬라이스. 이름 = {애니}_0_{프레임} (ArtLibrary 규칙).</summary>
        private static string SliceChar(TextureImporter importer, string path, string anim)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return $"{path}: 텍스처 로드 실패";

            string who = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));
            if (!CharFootY.TryGetValue(who, out float footY)) footY = 0.1f;

            int cols = tex.width / CharCell;
            var metas = new List<SpriteMetaData>();
            for (int c = 0; c < cols; c++)
            {
                metas.Add(new SpriteMetaData
                {
                    name = $"{anim}_0_{c}",
                    rect = new Rect(c * CharCell, 0, CharCell, CharCell),
                    pivot = new Vector2(0.5f, footY),
                    alignment = (int)SpriteAlignment.Custom,
                });
            }
#pragma warning disable 0618
            importer.spritesheet = metas.ToArray();
#pragma warning restore 0618
            importer.SaveAndReimport();
            return $"{path}: {cols}프레임 (셀 {CharCell}px, 발바닥 피벗 y={footY:0.####})";
        }
    }
}
