using System.Collections.Generic;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// Ninja Adventure(CC0) 스프라이트 로더. 슬라이스 이름 규칙: {파일명}_{행}_{열} (행은 위에서부터).
    /// 에셋이 없으면 Available=false — 호출부는 절차 생성 비주얼로 폴백한다.
    /// </summary>
    public static class ArtLibrary
    {
        private const string Root = "Art/NinjaAdventure";
        private static readonly Dictionary<string, Dictionary<string, Sprite>> _sheets
            = new Dictionary<string, Dictionary<string, Sprite>>();

        public static bool Available
            => Resources.Load<Texture2D>($"{Root}/Tilesets/TilesetField") != null;

        /// <summary>타일셋 스프라이트: Tile("TilesetField", 4, 1)</summary>
        public static Sprite Tile(string sheet, int row, int col)
            => Get($"{Root}/Tilesets/{sheet}", $"{sheet}_{row}_{col}");

        /// <summary>캐릭터 스프라이트: Character("Noble", "Walk", 1, 2)</summary>
        public static Sprite Character(string who, string anim, int row, int col)
            => Get($"{Root}/Character/{who}/{anim}", $"{anim}_{row}_{col}");

        private static Sprite Get(string path, string name)
        {
            if (!_sheets.TryGetValue(path, out var dict))
            {
                dict = new Dictionary<string, Sprite>();
                foreach (var s in Resources.LoadAll<Sprite>(path))
                    dict[s.name] = s;
                _sheets[path] = dict;
            }
            return dict.TryGetValue(name, out var sprite) ? sprite : null;
        }
    }
}
