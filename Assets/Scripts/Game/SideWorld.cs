using System.Collections.Generic;
using HangeulAdventure.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 사이드뷰 맵 월드 (맵 포맷 v2 — 사이드뷰 전환 기획, 14장 승인안 반영):
    /// Rigidbody2D(dynamic) 물리 기반 좌우 이동·점프·원웨이 발판(PlatformEffector2D)·사다리(존+중력 해제).
    /// MapWorld(탑다운, v1)와 외부 계약이 동일 — Enter / PlayerPosition / RefreshStates.
    /// GameApp.StartMap이 map.version==2일 때 이 클래스를 생성한다 (공존 스위치, 기획 10장).
    /// partial 구성: SideWorld.cs(상태·튜닝 상수·카메라·상태 갱신), Build.cs(지형 콜라이더·아트 배치·스팟·데코·플레이어·HUD),
    /// Input.cs(입력·물리 이동·사다리·상호작용·카메라 추적), Anim.cs(캐릭터 애니메이션).
    /// </summary>
    public partial class SideWorld : MonoBehaviour
    {
        // ══ 이동 튜닝 상수 (기획 1.2장 초기값 — 프로토타입 게이트에서 체감 튜닝 후 확정) ══
        private const float WalkSpeed = 6.0f;          // 걷기 속도 (u/s)
        private const float RunSpeed = 9.5f;           // 달리기 속도 (Shift, u/s)
        private const float GroundAccel = 90f;         // 지상 가속 (u/s²) — 크게 = 즉시 반응에 가깝게
        private const float AirAccel = 90f;            // 공중 가속 (기획: 공중 좌우 제어 100%)
        private const float Gravity = 40f;             // 중력 (u/s²) — gravityScale로 환산 적용
        private const float MaxFallSpeed = 18f;        // 최대 낙하 속도 (u/s)
        private const float JumpVelocity = 14.5f;      // 점프 초속 → 최고점 약 2.6u, 체공 약 0.73초
        private const float DoubleJumpVelocity = 13f;  // 2단 점프(갖신 아이템) 초속
        private const float JumpCutMultiplier = 0.5f;  // 가변 점프: 상승 중 점프 키 해제 시 수직 속도 배율
        private const float CoyoteTime = 0.10f;        // 발판 끝에서 떨어진 직후 점프 허용 (s)
        private const float JumpBufferTime = 0.10f;    // 착지 직전 점프 입력 보존 (s)
        private const float ClimbSpeed = 4.0f;         // 사다리 승강 속도 (u/s)
        private const float LadderSnapRange = 0.45f;   // 사다리 열 중심 부착 허용 오차 (u)
        private const float LadderExitJumpVy = 8.7f;   // 사다리 점프 이탈 수직 속도 (점프 초속의 60%)
        private const float LadderExitPushVx = 3.0f;   // 사다리 이탈 수평 속도
        private const float DropThroughTime = 0.25f;   // ↓+점프 원웨이 하강 시 충돌 무시 시간 (s)
        private const float PlayerWidth = 0.6f;        // 캐릭터 충돌 폭 (기획 1.1장)
        private const float PlayerHeight = 1.3f;       // 캐릭터 충돌 높이 (발 기준 위로)
        private const float InteractHalfW = 0.8f;      // 상호작용 박스 수평 반경 (기획 3.5장)
        private const float InteractHalfH = 1.2f;      // 상호작용 박스 수직 반경 — 위·아래 층 스팟 오반응 방지
        private const float CamFollowSmooth = 0.10f;   // 카메라 추적 스무딩 (s)
        private const float CamYOffset = 1.0f;         // 카메라 목표 y = 기준 y + 이 값 (발보다 약간 위를 본다)
        private const float CamFallCatchRatio = 0.25f; // 낙하로 화면 하단 이 비율 아래로 내려가면 기준 y 즉시 갱신 (기획 5장)

        // ══ 카메라 규격 (확정 사항) ══
        private const int AssetsPPU = 64;              // 사이드뷰 아트 전부 PPU 64
        private const int RefResolutionX = 1280;
        private const int RefResolutionY = 720;
        private const float OrthoSize = RefResolutionY * 0.5f / AssetsPPU; // 5.625 — 세로 시야 11.25u

        private GameApp _app;
        private MapData _map;
        private List<StageData> _stageLookup;
        private Camera _cam;
        private Canvas _canvas;
        private PixelPerfectCamera _pixelPerfect;

        // 아트 스위치: Art/Forest가 임포트돼 있으면 스프라이트, 아니면 색 블록 (Build.cs)
        private bool _art;
        private Transform _skyT, _fogT;   // 카메라를 따라다니는 하늘·안개 (Build.cs가 만들고 Input.cs가 옮긴다)
        private Vector2 _skyBase, _fogBase; // 원본 스프라이트 크기 (u) — 매 프레임 시야에 맞춰 늘리는 기준

        // 플레이어 물리 (위치 기준점 = 발 밑 중앙, 기획 1.1장)
        private Transform _playerT;
        private Rigidbody2D _rb;
        private BoxCollider2D _bodyCol;
        private float _normalGravityScale;

        // 지형 콜라이더 (Build.cs에서 채움)
        private readonly List<Collider2D> _oneWayCols = new List<Collider2D>();
        private readonly HashSet<Collider2D> _oneWaySet = new HashSet<Collider2D>();
        private readonly List<(Transform root, float parallax)> _backdropRoots = new List<(Transform, float)>();

        // 스팟·HUD (MapWorld와 동일 패턴)
        private readonly Dictionary<int, SpotView> _spotViews = new Dictionary<int, SpotView>();
        private readonly List<SpotView> _exitViews = new List<SpotView>();
        private SpotView _shopView;
        private SpotView _bossView;
        private TextMeshProUGUI _hudTitle, _hudProgress, _hudHint, _hudGold;
        private RectTransform _hudRoot;

        private class SpotView
        {
            public GameObject Go;
            public SpriteRenderer Bg;
            public TextMeshPro Label;
            public int StageId;      // 퍼즐 지점만 사용
            public int ExitIndex;    // 출구만 사용 (-1이면 출구 아님)
            public bool IsShop;
            public Vector2Int Pos;   // 발 칸 (그 칸에 서서 상호작용)

            public bool IsExit => ExitIndex >= 0;
        }

        // 색 (폴백 색 블록 — 사이드뷰 아트 도착 전 기본, 기획 리스크 3번)
        private static readonly Color SolidColor = new Color(0.36f, 0.30f, 0.25f);    // 전경 솔리드 (진한 흙·바위)
        private static readonly Color PlatformColor = new Color(0.58f, 0.43f, 0.27f); // 원웨이 발판 (나무 판)
        private static readonly Color LadderColor = new Color(0.66f, 0.51f, 0.31f);   // 사다리
        private static readonly Color BackdropSolid = new Color(0.77f, 0.81f, 0.71f); // 배경 실루엣 (연한색)
        private static readonly Color SpotLocked = new Color(0.72f, 0.70f, 0.66f);
        private static readonly Color SpotOpen = new Color(0.98f, 0.96f, 0.90f);
        private static readonly Color SpotCleared = new Color(1.00f, 0.85f, 0.45f);
        private static readonly Color SpotNext = new Color(0.95f, 0.55f, 0.35f);
        private static readonly Color ShopColor = new Color(0.62f, 0.78f, 0.92f);

        public void Enter(GameApp app, MapData map, List<StageData> stages, Camera cam, Canvas canvas, Vector2? playerPos)
        {
            _app = app;
            _map = map;
            _stageLookup = stages;
            _cam = cam;
            _canvas = canvas;
            _art = ArtLibrary.ForestAvailable;

            SetupCamera();   // 하늘·안개 크기가 시야를 읽어 정해지므로 짓기 전에 먼저
            BuildBackdrops();
            BuildTerrain();
            BuildSpots();
            BuildDecorations();
            // spawn은 발 칸 좌표 → 발 밑(칸 아래 발판 상면)으로 변환. 복귀 좌표(playerPos)는 이미 발 좌표.
            BuildPlayer(playerPos ?? new Vector2(_map.spawn.x, _map.spawn.y - 0.5f));
            BuildHud();
            RefreshStates();
            SnapCameraImmediate();
        }

        /// <summary>
        /// 세로 시야 11.25u (= 720px / PPU 64) + Pixel Perfect. 도트가 화면 픽셀에 1:1로 떨어져야
        /// 청크 이음새·픽셀 라인이 흔들리지 않는다. Pixel Perfect가 orthographicSize를 같은 값으로 다시 계산하지만,
        /// 컴포넌트가 꺼져 있는 프레임을 위해 직접도 넣어 둔다.
        /// </summary>
        private void SetupCamera()
        {
            _cam.orthographicSize = OrthoSize;
            _pixelPerfect = _cam.GetComponent<PixelPerfectCamera>();
            if (_pixelPerfect == null) _pixelPerfect = _cam.gameObject.AddComponent<PixelPerfectCamera>();
            _pixelPerfect.assetsPPU = AssetsPPU;
            _pixelPerfect.refResolutionX = RefResolutionX;
            _pixelPerfect.refResolutionY = RefResolutionY;
            _pixelPerfect.enabled = true;
        }

        /// <summary>
        /// 맵을 떠나기 직전 반드시 호출. 카메라는 퍼즐·전투·타이틀과 공유하는 자원이고,
        /// Pixel Perfect가 살아 있으면 그쪽의 orthographicSize(FitCamera 등)를 매 프레임 덮어쓴다.
        /// </summary>
        private void DisablePixelPerfect()
        {
            if (_pixelPerfect != null) _pixelPerfect.enabled = false;
        }

        public Vector2 PlayerPosition => _rb != null ? _rb.position : Vector2.zero;

        private void OnDestroy()
        {
            DisablePixelPerfect(); // 안전망 — 떠나기 직전 호출을 놓친 경로 대비
            if (_hudRoot != null) Destroy(_hudRoot.gameObject);
        }

        // ---- 상태 갱신 (MapWorld.RefreshStates와 동일 로직 — 진행 규칙 무변경 재사용, 기획 3.6장) ----

        public void RefreshStates()
        {
            int nextTutorial = MapProgress.NextTutorialStage(_map, _stageLookup);
            bool tutorialDone = nextTutorial < 0;

            foreach (var v in _spotViews.Values)
            {
                bool cleared = ProgressStore.GetStars(v.StageId) > 0;
                bool isTutorial = System.Array.IndexOf(_map.tutorialStages, v.StageId) >= 0;
                bool open = tutorialDone || (isTutorial && (cleared || v.StageId == nextTutorial));

                // 자음 게이트 (D-22): 미회수 자음 스테이지는 깨져 보임
                var stage = _stageLookup.Find(s => s.id == v.StageId);
                bool consonantLocked = stage != null && ProgressStore.MissingConsonants(stage).Length > 0;
                if (consonantLocked && !cleared)
                {
                    v.Bg.color = new Color(0.35f, 0.33f, 0.31f);
                    v.Label.text = "잠김"; // 잠금 표기 통일 (A-⑥). 자음 게이트와 진행 잠금은 색으로 구분
                    v.Label.color = new Color(0.55f, 0.52f, 0.48f);
                    continue;
                }
                v.Label.text = LabelFor(v.StageId);

                v.Bg.color = cleared ? SpotCleared
                    : v.StageId == nextTutorial ? SpotNext
                    : open ? SpotOpen
                    : SpotLocked;
                v.Label.color = open || cleared ? UiFactory.Ink : new Color(0.45f, 0.44f, 0.42f);
            }

            for (int i = 0; i < _exitViews.Count; i++)
            {
                bool open = MapProgress.ExitOpen(_map, _map.exits[i]);
                _exitViews[i].Bg.color = open ? SpotCleared : SpotLocked;
                _exitViews[i].Label.text = open ? "→" : "X";
            }

            if (_shopView != null) _shopView.Bg.color = ShopColor;
            if (_bossView != null)
                _bossView.Bg.color = ProgressStore.IsBossDefeated(_map.bossConfig?.Replace("boss_", ""))
                    ? SpotCleared : new Color(0.75f, 0.30f, 0.28f); // 미격파 = 붉은색

            // 세계의 글자는 미회수 자음이 깨져 보인다 (D-22 확장: 장소 이름·간판)
            // 지명은 깨뜨리지 않는다 (A-⑱) — 길을 찾는 정보라 읽을 수 있어야 한다. D-11 개정
            _hudTitle.text = $"{_map.title}  —  {_map.theme}";
            _hudGold.text = $"골드  {ProgressStore.Gold}";
            int cleared2 = MapProgress.ClearedCount(_map);
            _hudProgress.text = tutorialDone
                ? $"클리어 {cleared2}/{_map.spots.Count}"
                : $"기초 배우기 진행 중 ({System.Array.IndexOf(_map.tutorialStages, nextTutorial) + 1}/{_map.tutorialStages.Length}) — 주황색 지점으로 가세요";
        }

        private bool IsSpotOpen(int stageId)
        {
            int next = MapProgress.NextTutorialStage(_map, _stageLookup);
            if (next < 0) return true;
            bool isTutorial = System.Array.IndexOf(_map.tutorialStages, stageId) >= 0;
            return isTutorial && (ProgressStore.GetStars(stageId) > 0 || stageId == next);
        }
    }
}
