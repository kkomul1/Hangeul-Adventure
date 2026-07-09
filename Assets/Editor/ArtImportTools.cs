using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HangeulAdventure.EditorTools
{
    /// <summary>
    /// Ninja Adventure(CC0) 픽셀아트 임포트 설정: Point 필터, PPU 16, 16x16 그리드 슬라이스.
    /// </summary>
    public static class ArtImportTools
    {
        private const string ArtRoot = "Assets/Resources/Art/NinjaAdventure";

        [MenuItem("HangeulAdventure/아트 임포트 설정 적용 (NinjaAdventure)")]
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

                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.spriteImportMode = SpriteImportMode.Multiple;

                // 16x16 그리드 슬라이스
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                int w = importer.spriteImportMode == SpriteImportMode.Multiple && tex != null ? tex.width : 0;
                int h = tex != null ? tex.height : 0;
                var metas = new List<SpriteMetaData>();
                int cell = 16;
                int cols = w / cell, rows = h / cell;
                string baseName = System.IO.Path.GetFileNameWithoutExtension(path);
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        metas.Add(new SpriteMetaData
                        {
                            name = $"{baseName}_{r}_{c}", // 행(위에서부터)_열
                            rect = new Rect(c * cell, h - (r + 1) * cell, cell, cell),
                            pivot = new Vector2(0.5f, 0.5f),
                            alignment = (int)SpriteAlignment.Center,
                        });
                    }
                }
#pragma warning disable 0618
                importer.spritesheet = metas.ToArray();
#pragma warning restore 0618
                importer.SaveAndReimport();
                report.AppendLine($"{path}: {cols}x{rows} 슬라이스");
            }
            AssetDatabase.Refresh();
            return report.ToString();
        }
    }
}
