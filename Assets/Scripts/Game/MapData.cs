using System;
using System.Collections.Generic;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 맵 정의 (Resources/Maps/map_XX.json). 월드는 exits로 연결된 그래프 (양방향 이동, D-17).
    /// terrain 문자: '.'=풀, '-'=길, '#'=나무/집(통행 불가), '~'=물(통행 불가)
    /// 진행 규칙: 최초 진입 시 tutorialStages를 순서대로 강제 → 완료 시 전체 지점 개방.
    /// 각 출구는 "이 맵에서 required개 클리어" 시 개방.
    /// </summary>
    [Serializable]
    public class MapJson
    {
        public int id;
        public string title = "";
        public string theme = "";
        public string bgm = "";   // 배경음악 트랙 (선택, Resources/Audio/{bgm}. 생략 시 bgm_forest)
        public string[] terrain;
        public int[] spawn;
        public int[] tutorialStages;
        public SpotJson[] spots;
        public ExitJson[] exits;
        public int[] shop;      // 상점 위치 (선택)
        public BossJson boss;   // 사천왕 (선택)
        public RoomJson[] rooms; // 자음 회수 방 (선택, D-23)
    }

    /// <summary>자음 회수 방 (D-23): 묶인 스테이지를 전부 클리어하면 자음 하나를 되찾는다.</summary>
    [Serializable]
    public class RoomJson
    {
        public int[] stages;
        public string reward = ""; // 되찾는 자음 1글자
        public string label = "";  // 연출용 이름 (예: "웃마을")
    }

    [Serializable]
    public class BossJson
    {
        public int[] pos;
        public string config; // Resources/Battles/{config}.json
    }

    [Serializable]
    public class SpotJson
    {
        public int stage;
        public int[] pos;
    }

    [Serializable]
    public class ExitJson
    {
        public int[] pos;      // 출구 위치
        public int toMap;      // 목적지 맵 id
        public int[] arrive;   // 도착 위치 (생략 시 목적지 spawn)
        public string label = "";
        public int required;   // 이 맵에서 필요한 클리어 수 (0 = 항상 열림)
    }

    public class ExitData
    {
        public Vector2Int pos;
        public int toMapId;
        public Vector2Int? arrive;
        public string label;
        public int required;
    }

    /// <summary>파싱된 맵 (좌표계: x 오른쪽+, y 위쪽+).</summary>
    public class MapData
    {
        public int id;
        public string title;
        public string theme;
        public string bgm;
        public int width, height;
        public char[] tiles;
        public Vector2Int spawn;
        public int[] tutorialStages;
        public List<(int stageId, Vector2Int pos)> spots = new List<(int, Vector2Int)>();
        public List<ExitData> exits = new List<ExitData>();
        public Vector2Int? shop;
        public Vector2Int? bossPos;
        public string bossConfig;
        public List<RoomJson> rooms = new List<RoomJson>();

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
                bgm = mj.bgm ?? "",
                width = w,
                height = h,
                tiles = new char[w * h],
                tutorialStages = mj.tutorialStages ?? Array.Empty<int>(),
            };

            for (int row = 0; row < h; row++)
            {
                if (mj.terrain[row].Length != w)
                    throw new ArgumentException($"맵 {mj.id}: 행 길이 불일치 (행 {row})");
                int y = h - 1 - row;
                for (int x = 0; x < w; x++)
                    map.tiles[y * w + x] = mj.terrain[row][x];
            }

            Vector2Int P(int[] a) => new Vector2Int(a[0], h - 1 - a[1]); // JSON은 위에서부터 행 번호
            map.spawn = P(mj.spawn);
            if (mj.spots != null)
                foreach (var s in mj.spots)
                    map.spots.Add((s.stage, P(s.pos)));
            if (mj.exits != null)
                foreach (var e in mj.exits)
                    map.exits.Add(new ExitData
                    {
                        pos = P(e.pos),
                        toMapId = e.toMap,
                        arrive = (e.arrive != null && e.arrive.Length == 2) ? P2(e.arrive, e, mj) : (Vector2Int?)null,
                        label = e.label ?? "",
                        required = e.required,
                    });
            if (mj.shop != null && mj.shop.Length == 2)
                map.shop = P(mj.shop);
            if (mj.boss != null && mj.boss.pos != null && mj.boss.pos.Length == 2)
            {
                map.bossPos = P(mj.boss.pos);
                map.bossConfig = mj.boss.config;
            }
            if (mj.rooms != null)
                foreach (var r in mj.rooms)
                    if (r?.stages != null && r.stages.Length > 0 && !string.IsNullOrEmpty(r.reward))
                        map.rooms.Add(r);

            return map;

            // arrive는 "목적지 맵" 좌표라 이 맵의 높이로 뒤집으면 안 됨 — 목적지 로드 시 뒤집기 위해
            // JSON 행 좌표 그대로 보관하고 음수 y로 표시하는 대신, 여기서는 원시값 보관용 헬퍼 사용.
            static Vector2Int P2(int[] a, ExitJson e, MapJson mj2) => new Vector2Int(a[0], a[1]);
        }

        /// <summary>arrive(JSON 행 좌표)를 목적지 맵의 월드 좌표로 변환.</summary>
        public static Vector2Int ResolveArrive(MapData destination, Vector2Int rawArrive)
            => new Vector2Int(rawArrive.x, destination.height - 1 - rawArrive.y);

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

    /// <summary>맵 진행 판정 (스테이지 별 기록 기반).</summary>
    public static class MapProgress
    {
        public static int NextTutorialStage(MapData map) => NextTutorialStage(map, null);

        /// <summary>
        /// 다음 강제 튜토리얼 스테이지. stages를 주면 자음 게이트(D-22)에 잠긴 스테이지는
        /// 건너뛴다 — 사다리를 이탈해 진입한 맵에서 튜토리얼이 ▒에 막혀 잠기는 것 방지.
        /// </summary>
        public static int NextTutorialStage(MapData map, List<Engine.StageData> stages)
        {
            foreach (int id in map.tutorialStages)
            {
                if (ProgressStore.GetStars(id) > 0) continue;
                if (stages != null)
                {
                    var stage = stages.Find(s => s.id == id);
                    if (stage != null && ProgressStore.MissingConsonants(stage).Length > 0) continue;
                }
                return id;
            }
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

        /// <summary>출구 개방: 이 맵에서 required개 클리어 (개발자 모드는 전부).</summary>
        public static bool ExitOpen(MapData map, ExitData exit)
            => ProgressStore.DevMode || ClearedCount(map) >= exit.required;

        /// <summary>
        /// 방 보상 (D-23): 방의 스테이지를 전부 클리어했는데 아직 회수 안 된 자음이 있으면 그 방 반환.
        /// 지급(RecoverConsonant)은 호출측에서 연출과 함께 수행.
        /// </summary>
        public static RoomJson PendingRoomReward(MapData map)
        {
            foreach (var room in map.rooms)
            {
                if (ProgressStore.RecoveredConsonants.IndexOf(room.reward[0]) >= 0) continue;
                bool allClear = true;
                foreach (int id in room.stages)
                    if (ProgressStore.GetStars(id) == 0) { allClear = false; break; }
                if (allClear) return room;
            }
            return null;
        }
    }
}
