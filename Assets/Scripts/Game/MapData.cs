using System;
using System.Collections.Generic;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 맵 정의 (Resources/Maps/map_XX.json). 월드는 exits로 연결된 그래프 (양방향 이동, D-17).
    /// version 1 (탑다운) terrain 문자: '.'=풀, '-'=길, '#'=나무/집(통행 불가), '~'=물(통행 불가)
    /// version 2 (사이드뷰, 사이드뷰 전환 기획 3장) terrain 문자: '.'=공기, '#'=솔리드, '='=원웨이 발판, 'H'=사다리.
    ///   v2는 layers(전경 playfield + 배경 backdrop)로 지형을 정의할 수 있고, 생략 시 terrain을 전경으로 쓴다.
    /// 진행 규칙: 최초 진입 시 tutorialStages를 순서대로 강제 → 완료 시 전체 지점 개방.
    /// 각 출구는 "이 맵에서 required개 클리어" 시 개방.
    /// </summary>
    [Serializable]
    public class MapJson
    {
        public int id;
        public int version = 1;  // 1=탑다운(MapWorld), 2=사이드뷰(SideWorld) — 공존 스위치 (기획 10장)
        public string title = "";
        public string theme = "";
        public string bgm = "";   // 배경음악 트랙 (선택, Resources/Audio/{bgm}. 생략 시 bgm_forest)
        public string[] terrain;
        public LayerJson[] layers; // v2 전용 (선택): playfield 1장 + backdrop n장 (기획 14장-5)
        public int[] spawn;
        public int[] tutorialStages;
        public SpotJson[] spots;
        public ExitJson[] exits;
        public int[] shop;      // 상점 위치 (선택)
        public BossJson boss;   // 사천왕 (선택)
        public RoomJson[] rooms; // 자음 회수 방 (선택, D-23)
        public DecorationJson[] decorations; // v2 전용 (선택): 충돌 없는 전경 장식
    }

    /// <summary>
    /// v2 전경 장식 (충돌 없음): 바위·그루터기·통나무·장승 등을 지면에 세운다.
    /// 좌표는 <b>월드 좌표</b>다 — spots/exits의 "위에서부터 센 행 번호"와 다르다.
    /// 데코는 칸 단위가 아니라 반 칸 단위로 놓이므로(승인 룩) 행 뒤집기를 쓰지 않는 편이 읽기 쉽다.
    /// y = 데코가 서는 지면 상면 (바닥 칸 윗변 = 0.5). 스프라이트는 발치 피벗이라 y에 그대로 세운다.
    /// </summary>
    [Serializable]
    public class DecorationJson
    {
        public string art = "";  // Resources/Art/Forest/Props/{art}
        public float x;
        public float y = 0.5f;
        public bool flip;        // 좌우 반전
        public int order;        // 데코끼리 앞뒤 정렬 (0=기본, 클수록 앞). 데코 편집기에서 조절
    }

    /// <summary>
    /// v2 지형 레이어 (사이드뷰 전환 기획 14장-5): 맨 앞 playfield에서 플레이하고
    /// 뒤에 backdrop(패럴랙스 배경)을 쌓는다. 신규 지역·대형 맵은 레이어 추가만으로 확장.
    /// </summary>
    [Serializable]
    public class LayerJson
    {
        public string type = "playfield"; // "playfield"(충돌 있음, 1장) | "backdrop"(장식, n장)
        public float parallax = 0.5f;     // backdrop 전용: 카메라 이동 대비 배경 스크롤 비율 (1=전경과 동일, 0=고정)
        public string[] terrain;
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

    /// <summary>v2 배경 레이어 (파싱 결과): 충돌 없는 장식 지형 + 패럴랙스 계수.</summary>
    public class BackdropLayer
    {
        public int width, height;
        public char[] tiles;
        public float parallax = 0.5f;

        public char Tile(int x, int y)
            => (x < 0 || x >= width || y < 0 || y >= height) ? '.' : tiles[y * width + x];
    }

    /// <summary>파싱된 맵 (좌표계: x 오른쪽+, y 위쪽+).</summary>
    public class MapData
    {
        public int id;
        public int version = 1;
        public string title;
        public string theme;
        public string bgm;
        public int width, height;
        public char[] tiles;   // v1: 지형, v2: 전경(playfield) 충돌 그리드
        public Vector2Int spawn;
        public int[] tutorialStages;
        public List<(int stageId, Vector2Int pos)> spots = new List<(int, Vector2Int)>();
        public List<ExitData> exits = new List<ExitData>();
        public Vector2Int? shop;
        public Vector2Int? bossPos;
        public string bossConfig;
        public List<RoomJson> rooms = new List<RoomJson>();
        public List<BackdropLayer> backdrops = new List<BackdropLayer>(); // v2 전용
        public List<DecorationJson> decorations = new List<DecorationJson>(); // v2 전용

        public char Tile(int x, int y)
            => (x < 0 || x >= width || y < 0 || y >= height) ? '#' : tiles[y * width + x];

        public bool Walkable(int x, int y)
        {
            char t = Tile(x, y);
            return t == '.' || t == '-';
        }

        // ── v2 사이드뷰 충돌 질의 (사이드뷰 전환 기획 3.2장) ──

        /// <summary>솔리드: 전방향 충돌 (지면·벽·천장). 맵 밖도 솔리드 취급.</summary>
        public bool IsSolid(int x, int y) => Tile(x, y) == '#';

        /// <summary>사다리 칸 (통과, W/S로 부착).</summary>
        public bool IsLadder(int x, int y) => Tile(x, y) == 'H';

        /// <summary>
        /// 사다리 꼭대기 규칙: 'H' 칸의 좌 또는 우가 발판('='/'#')이면 그 칸 자체를
        /// 원웨이 발판으로 취급 (발판을 사다리가 관통하는 메이플 구조를 한 문자로 표현).
        /// </summary>
        public bool IsLadderTop(int x, int y)
        {
            if (!IsLadder(x, y)) return false;
            char l = Tile(x - 1, y), r = Tile(x + 1, y);
            return l == '=' || l == '#' || r == '=' || r == '#';
        }

        /// <summary>원웨이 발판: 상면만, 하강 중만 충돌 ('=' + 사다리 꼭대기 'H').</summary>
        public bool IsOneWay(int x, int y) => Tile(x, y) == '=' || IsLadderTop(x, y);
    }

    public static class MapLoader
    {
        public const string ResourceFolder = "Maps";

        public static MapData FromJson(string json)
        {
            var mj = JsonUtility.FromJson<MapJson>(json);
            if (mj == null)
                throw new ArgumentException("맵 JSON 형식 오류");

            // v2: layers에서 전경(playfield)을 찾는다. layers 생략 시 terrain을 전경으로 사용 (기획 3.1장)
            string[] playfield = mj.terrain;
            if (mj.version >= 2 && mj.layers != null)
                foreach (var l in mj.layers)
                    if (l != null && l.type == "playfield" && l.terrain != null && l.terrain.Length > 0)
                    {
                        playfield = l.terrain;
                        break;
                    }
            if (playfield == null || playfield.Length == 0)
                throw new ArgumentException("맵 JSON 형식 오류: terrain/playfield 없음");

            int h = playfield.Length;
            int w = playfield[0].Length;
            var map = new MapData
            {
                id = mj.id,
                version = mj.version,
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
                if (playfield[row].Length != w)
                    throw new ArgumentException($"맵 {mj.id}: 행 길이 불일치 (행 {row})");
                int y = h - 1 - row;
                for (int x = 0; x < w; x++)
                    map.tiles[y * w + x] = playfield[row][x];
            }

            // v2 배경 레이어 (충돌 없음, 패럴랙스 계수는 데이터 소관 — 기획 14장-5)
            if (mj.version >= 2 && mj.layers != null)
                foreach (var l in mj.layers)
                {
                    if (l == null || l.type != "backdrop" || l.terrain == null || l.terrain.Length == 0) continue;
                    int bh = l.terrain.Length, bw = l.terrain[0].Length;
                    var bd = new BackdropLayer { width = bw, height = bh, tiles = new char[bw * bh], parallax = l.parallax };
                    for (int row = 0; row < bh; row++)
                    {
                        if (l.terrain[row].Length != bw)
                            throw new ArgumentException($"맵 {mj.id}: backdrop 행 길이 불일치 (행 {row})");
                        int y = bh - 1 - row;
                        for (int x = 0; x < bw; x++)
                            bd.tiles[y * bw + x] = l.terrain[row][x];
                    }
                    map.backdrops.Add(bd);
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
            if (mj.decorations != null)
                foreach (var d in mj.decorations)
                    if (d != null && !string.IsNullOrEmpty(d.art))
                        map.decorations.Add(d);

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
        /// 건너뛴다 — 사다리를 이탈해 진입한 맵에서 튜토리얼이 자음 게이트에 막혀 잠기는 것 방지.
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
