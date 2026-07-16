using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HangeulAdventure.EditorTools
{
    /// <summary>
    /// 조선풍 도트 에셋(PixelLab 생성) 임포트 설정: Point 필터, 무압축.
    /// Tilesets/* = PPU 16, 16px 그리드. Character/* = PPU 16, 24px 그리드(피벗 하단 부근).
    /// BushWall = PPU 32, 단일 스프라이트 (한 칸 벽).
    /// 슬라이스 이름 규칙은 ArtLibrary와 동일: {파일명}_{행}_{열} (행은 위에서부터).
    /// </summary>
    public static class JoseonImportTools
    {
        private const string ArtRoot = "Assets/Resources/Art/Joseon";

        [MenuItem("HangeulAdventure/아트 임포트 설정 적용 (Joseon)")]
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

                string baseName = System.IO.Path.GetFileNameWithoutExtension(path);
                bool isBush = baseName == "BushWall";
                bool isCharacter = path.Contains("/Character/");
                int cell = isCharacter ? 24 : 16;

                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = isBush ? 32 : 16;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.spriteImportMode = isBush ? SpriteImportMode.Single : SpriteImportMode.Multiple;

                if (isBush)
                {
                    importer.SaveAndReimport();
                    report.AppendLine($"{path}: 단일 (PPU 32)");
                    continue;
                }

                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                int w = tex != null ? tex.width : 0;
                int h = tex != null ? tex.height : 0;
                var metas = new List<SpriteMetaData>();
                int cols = w / cell, rows = h / cell;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        metas.Add(new SpriteMetaData
                        {
                            name = $"{baseName}_{r}_{c}", // 행(위에서부터)_열
                            rect = new Rect(c * cell, h - (r + 1) * cell, cell, cell),
                            // 캐릭터는 발 위치(캔버스 하단 약간 위)에 피벗 (manifest 권장값)
                            pivot = isCharacter ? new Vector2(0.5f, 0.1f) : new Vector2(0.5f, 0.5f),
                            alignment = (int)(isCharacter ? SpriteAlignment.Custom : SpriteAlignment.Center),
                        });
                    }
                }
#pragma warning disable 0618
                importer.spritesheet = metas.ToArray();
#pragma warning restore 0618
                importer.SaveAndReimport();
                report.AppendLine($"{path}: {cols}x{rows} 슬라이스 (셀 {cell}px)");
            }
            AssetDatabase.Refresh();
            return report.ToString();
        }
    }
}
