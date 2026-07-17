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

        // ---- 조선풍 (PixelLab 생성, Art/Joseon — M3-6) ----

        private const string JoseonRoot = "Art/Joseon";

        public static bool JoseonAvailable
            => Resources.Load<Texture2D>($"{JoseonRoot}/Tilesets/TilesetGrassDirt") != null;

        /// <summary>조선풍 타일: JoseonTile("TilesetGrassDirt", 1, 2) = 순수 풀밭, (3, 0) = 순수 흙길/물</summary>
        public static Sprite JoseonTile(string sheet, int row, int col)
            => Get($"{JoseonRoot}/Tilesets/{sheet}", $"{sheet}_{row}_{col}");

        /// <summary>수풀 벽 (단일 스프라이트, PPU 32 = 한 칸)</summary>
        public static Sprite JoseonBush()
            => Get($"{JoseonRoot}/Tilesets/BushWall", "BushWall");

        /// <summary>선비 캐릭터: JoseonSeonbi("Walk", 행=방향(남0 동1 북2 서3), 열=프레임). Idle은 1행 4열(남동북서)</summary>
        public static Sprite JoseonSeonbi(string anim, int row, int col)
            => Get($"{JoseonRoot}/Character/Seonbi/{anim}", $"{anim}_{row}_{col}");

        // ---- 사이드뷰 시작의 숲 (PixelLab 생성, Art/Forest — M4-7) ----
        // PPU 64. 피벗은 임포트 설정(ForestImportTools)이 쥐고 있다 — 호출부는 좌표만 주면 된다:
        //   Terrain 지면 청크 = 표면선 피벗 / 단차·발판 = TopLeft / 사다리·수풀·Prop·나무 = 발치(BottomCenter)

        private const string ForestRoot = "Art/Forest";

        public static bool ForestAvailable
            => Resources.Load<Sprite>($"{ForestRoot}/Terrain/ground_flat_03") != null;

        /// <summary>지형: ForestTerrain("ground_flat_03"), ("ladder_body_seg") 등</summary>
        public static Sprite ForestTerrain(string name)
            => Get($"{ForestRoot}/Terrain/{name}", name);

        /// <summary>프롭·팻말·문: ForestProp("spot_sign_hanji")</summary>
        public static Sprite ForestProp(string name)
            => Get($"{ForestRoot}/Props/{name}", name);

        /// <summary>배경: ForestBackdrop("tree_pine_large"), ("sky_gradient"), ("bg_ridge"), ("fog_band")</summary>
        public static Sprite ForestBackdrop(string name)
            => Get($"{ForestRoot}/Backdrop/{name}", name);

        /// <summary>캐릭터 프레임: ForestChar("SeonbiHanbok", "Walk", 2). 스트립은 1행이라 행은 항상 0.</summary>
        public static Sprite ForestChar(string who, string anim, int frame)
            => Get($"{ForestRoot}/Char/{who}/{anim}", $"{anim}_0_{frame}");

        /// <summary>애니메이션 프레임 수 (0부터 없을 때까지). 없으면 0.</summary>
        public static int ForestCharFrames(string who, string anim)
        {
            int n = 0;
            while (ForestChar(who, anim, n) != null) n++;
            return n;
        }

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
