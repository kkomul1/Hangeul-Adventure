using System;
using System.Collections.Generic;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 맵 정의 (Resources/Maps/map_XX.json).
    /// terrain 문자: '.'=풀, '-'=길, '#'=나무/집(통행 불가), '~'=물(통행 불가)
    /// 진행 규칙: 최초 진입 시 tutorialStages를 순서대로 강제 → 완료 시 전체 지점 개방
    /// → unlockCount개 클리어 시 출구(다음 맵) 개방.
    /// </summary>
    [Serializable]
    public class MapJson
    {
        public int id;
        public string title = "";
        public string theme = "";
        public string[] terrain;
        public int[] spawn;          // [x, y] (y는 위에서부터 행 번호가 아니라 월드 y — 로더가 변환)
        public int[] tutorialStages; // 스테이지 id, 직렬 순서
        public SpotJson[] spots;     // 퍼즐 지점
        public int unlockCount;      // 다음 맵 해금에 필요한 클리어 수 (튜토리얼 포함)
        public int[] exit;           // 출구 위치 [x, y]
    }

    [Serializable]
    public class SpotJson
    {
        public int stage;  // 스테이지 id
        public int[] pos;  // [x, y]
    }

    /// <summary>파싱된 맵 (좌표계: x 오른쪽+, y 위쪽+ — 엔진/월드와 동일).</summary>
    public class MapData
    {
        public int id;
        public string title;
        public string theme;
        public int width, height;
        public char[] tiles;                  // index = y*width + x
        public Vector2Int spawn;
        public int[] tutorialStages;
        public List<(int stageId, Vector2Int pos)> spots = new List<(int, Vector2Int)>();
        public int unlockCount;
        public Vector2Int exit;

        public char Tile(int x, int y)
            => (x < 0 || x >= width || y < 0 || y >= height) ? '#' : tiles[y * width + x];

        public bool Walkable(int x, int y)
        {
            char t = Tile(x, y);
            return t == '.' || t == '-';
        }
    }

    public static class MapLoader
    {
        public const string ResourceFolder = "Maps";

        public static MapData FromJson(string json)
        {
            var mj = JsonUtility.FromJson<MapJson>(json);
            if (mj?.terrain == null || mj.terrain.Length == 0)
                throw new ArgumentException("맵 JSON 형식 오류: terrain 없음");

            int h = mj.terrain.Length;
            int w = mj.terrain[0].Length;
            var map = new MapData
            {
                id = mj.id,
                title = mj.title ?? "",
                theme = mj.theme ?? "",
                width = w,
                height = h,
                tiles = new char[w * h],
                tutorialStages = mj.tutorialStages ?? Array.Empty<int>(),
                unlockCount = mj.unlockCount,
            };

            for (int row = 0; row < h; row++)
            {
                if (mj.terrain[row].Length != w)
                    throw new ArgumentException($"맵 {mj.id}: 행 길이 불일치 (행 {row})");
                int y = h - 1 - row; // 첫 행이 가장 위
                for (int x = 0; x < w; x++)
                    map.tiles[y * w + x] = mj.terrain[row][x];
            }

            Vector2Int P(int[] a) => new Vector2Int(a[0], h - 1 - a[1]); // JSON은 위에서부터 행 번호
            map.spawn = P(mj.spawn);
            map.exit = P(mj.exit);
            if (mj.spots != null)
                foreach (var s in mj.spots)
                    map.spots.Add((s.stage, P(s.pos)));

            return map;
        }

        public static List<MapData> LoadAll()
        {
            var assets = Resources.LoadAll<TextAsset>(ResourceFolder);
            var list = new List<MapData>(assets.Length);
            foreach (var a in assets)
            {
                try { list.Add(FromJson(a.text)); }
                catch (Exception e) { Debug.LogError($"맵 로드 실패 ({a.name}): {e.Message}"); }
            }
            list.Sort((a, b) => a.id.CompareTo(b.id));
            return list;
        }
    }

    /// <summary>맵 진행 판정 (스테이지 별 기록 기반 — ProgressStore 재사용).</summary>
    public static class MapProgress
    {
        /// <summary>튜토리얼 직렬 진행: 아직 못 깬 첫 튜토리얼 스테이지 id, 전부 깼으면 -1.</summary>
        public static int NextTutorialStage(MapData map)
        {
            foreach (int id in map.tutorialStages)
                if (ProgressStore.GetStars(id) == 0) return id;
            return -1;
        }

        public static bool TutorialDone(MapData map) => NextTutorialStage(map) < 0;

        public static int ClearedCount(MapData map)
        {
            int n = 0;
            foreach (var (stageId, _) in map.spots)
                if (ProgressStore.GetStars(stageId) > 0) n++;
            return n;
        }

        public static bool ExitOpen(MapData map) => ClearedCount(map) >= map.unlockCount;

        /// <summary>맵 잠금: 첫 맵은 항상, 이후는 이전 맵의 출구가 열렸으면. 개발자 모드는 전부.</summary>
        public static bool IsMapUnlocked(List<MapData> maps, int index)
        {
            if (ProgressStore.DevMode) return true;
            if (index <= 0) return true;
            return ExitOpen(maps[index - 1]);
        }
    }
}
