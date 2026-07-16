using HangeulAdventure.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 전투 화면: 행동 선택(공격/수비/기술) → 시련 퍼즐(전조 표시) → 판정 반복.
    /// 퍼즐은 기존 보드/컨트롤러를 BattleMode로 재사용한다.
    /// </summary>
    public class BattleScreen : MonoBehaviour
    {
        private GameApp _app;
        private Canvas _canvas;
        private Camera _cam;
        private BattleState _battle;

        private RectTransform _panel;
        private TextMeshProUGUI _enemyName, _enemyHpText, _playerHpText, _logText, _telegraphText;
        private RectTransform _actionRow;
        private GameObject _puzzleRoot;
        private TrialConfig _currentTrial;
        private int _currentLimit;
        private GameSession _trialSession;
        private GameController _trialController;
        private GameHud _trialHud;

        public System.Action<bool> Finished; // true=승리

        public void Begin(GameApp app, Canvas canvas, Camera cam, BattleConfig config)
        {
            _app = app;
            _canvas = canvas;
            _cam = cam;

            var weapon = ItemStore.Find(ItemStore.EquippedWeapon);
            var armor = ItemStore.Find(ItemStore.EquippedArmor);
            _battle = new BattleState(config, weapon?.atk ?? 0, armor?.def ?? 0);

            BuildUi(config);
            SetLog(config.intro);
            ShowActions(true);
            Refresh();
        }

        private void BuildUi(BattleConfig config)
        {
            _panel = UiFactory.CreatePanel(_canvas.transform, "BattlePanel", new Color(0.16f, 0.13f, 0.12f, 0.96f));
            UiFactory.Stretch(_panel);

            _enemyName = UiFactory.CreateText(_panel, "EnemyName", config.name, 40, new Color(0.95f, 0.85f, 0.70f));
            UiFactory.SetRect(_enemyName.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(600, 60));

            // 보스 초상: Resources/Art/Portraits/{portrait|id}. 일러스트 미도착 보스는 임시 초상 유지
            var portraitSprite = Resources.Load<Sprite>("Art/Portraits/" +
                (string.IsNullOrEmpty(config.portrait) ? config.id : config.portrait));
            if (portraitSprite != null)
            {
                var pGo = new GameObject("Portrait", typeof(Image));
                pGo.transform.SetParent(_panel, false);
                var img = pGo.GetComponent<Image>();
                img.sprite = portraitSprite;
                img.preserveAspect = true;
                UiFactory.SetRect((RectTransform)pGo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(180, 180));
            }
            else
            {
                var portrait = UiFactory.CreatePanel(_panel, "Portrait", new Color(0.35f, 0.22f, 0.20f));
                UiFactory.SetRect(portrait, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(180, 180));
                var face = UiFactory.CreateText(portrait, "Face", "巾", 80, new Color(0.95f, 0.85f, 0.70f));
                UiFactory.Stretch(face.rectTransform);
            }

            _enemyHpText = UiFactory.CreateText(_panel, "EnemyHp", "", 26, new Color(0.95f, 0.55f, 0.50f));
            UiFactory.SetRect(_enemyHpText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -300), new Vector2(400, 40));

            _telegraphText = UiFactory.CreateText(_panel, "Telegraph", "", 22, new Color(0.85f, 0.75f, 0.55f));
            UiFactory.SetRect(_telegraphText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -345), new Vector2(800, 36));

            _logText = UiFactory.CreateText(_panel, "Log", "", 24, new Color(0.92f, 0.90f, 0.85f));
            UiFactory.SetRect(_logText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(900, 60));

            _playerHpText = UiFactory.CreateText(_panel, "PlayerHp", "", 26, new Color(0.60f, 0.85f, 0.60f), TextAlignmentOptions.Left);
            UiFactory.SetRect(_playerHpText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 90), new Vector2(500, 40));

            _actionRow = UiFactory.CreateEmpty(_panel, "Actions");
            UiFactory.SetRect(_actionRow, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(700, 70));
            MakeActionBtn("공격", -240, () => OnAct(BattleAction.Attack));
            MakeActionBtn("수비", 0, () => OnAct(BattleAction.Guard));
            MakeActionBtn("된소리 일격", 240, () => OnAct(BattleAction.Skill));

            var flee = UiFactory.CreateButton(_panel, "FleeBtn", "도망가기", 18, UiFactory.Paper, UiFactory.Ink, () => End(false));
            UiFactory.SetRect((RectTransform)flee.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(130, 48));
        }

        private void MakeActionBtn(string label, float x, System.Action onClick)
        {
            var b = UiFactory.CreateButton(_actionRow, "Act_" + label, label, 24, UiFactory.Accent, Color.white, onClick);
            UiFactory.SetRect((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(210, 64));
        }

        private void Refresh()
        {
            _enemyHpText.text = $"{_battle.Config.name}  HP {_battle.EnemyHp}/{_battle.Config.maxHp}";
            _playerHpText.text = $"나  HP {_battle.PlayerHp}/{_battle.PlayerMaxHp}   (공 {_battle.PlayerAtk} / 방 {_battle.PlayerDef})";
        }

        private void SetLog(string text) => _logText.text = text;

        private void ShowActions(bool on) => _actionRow.gameObject.SetActive(on);

        private void OnAct(BattleAction action)
        {
            _battle.Act(action);
            Refresh();
            SetLog(_battle.Log);

            if (_battle.Phase == BattlePhase.Victory) { End(true); return; }

            // 시련 시작 (전조 표시)
            ShowActions(false);
            var (trial, limit) = _battle.NextTrial();
            _currentTrial = trial;
            _currentLimit = limit;
            _telegraphText.text = $"⚔ 시련: '{trial.goals}' 만들기 — 제한 {limit}수 (초과하면 반격당한다!)";
            StartTrialPuzzle(trial);
        }

        private void StartTrialPuzzle(TrialConfig trial)
        {
            var groups = new System.Collections.Generic.List<GoalGroup>();
            foreach (string part in trial.goals.Split(','))
            {
                string g = part.Trim();
                if (g.Length > 0) groups.Add(new GoalGroup { display = g, slots = g.ToCharArray() });
            }
            var stage = StageBuilder.FromRows(trial.rows, groups.ToArray());
            stage.title = "시련";
            stage.minMoves = 1; // 루비 무관 (전투 퍼즐)
            stage.starThresholds = StageData.DefaultStarThresholds(1);

            _trialSession = new GameSession(stage);
            _puzzleRoot = new GameObject("TrialPuzzle");

            var boardGo = new GameObject("Board", typeof(BoardView));
            boardGo.transform.SetParent(_puzzleRoot.transform, false);
            var board = boardGo.GetComponent<BoardView>();
            board.Bind(_trialSession);

            // 보드가 전투 패널 위에 보이도록 카메라 세팅 (패널은 반투명 아님 — 보드 영역만 비워줌)
            _cam.orthographicSize = Mathf.Max(stage.height * 0.5f + 2.4f, 3.5f);
            _cam.transform.position = new Vector3(0, -0.3f, -10);

            _trialHud = _puzzleRoot.AddComponent<GameHud>();
            _trialHud.Build(_canvas);
            _trialHud.Bind(_trialSession, 0, 0, "계속");

            _trialController = _puzzleRoot.AddComponent<GameController>();
            _trialController.BattleMode = true;
            _trialController.Bind(_trialSession, board, _trialHud, _cam);
            _trialController.PuzzleCleared += OnTrialCleared;
            _trialController.ActionTaken += OnTrialAction;

            // 퍼즐 동안 전투 패널을 살짝 위로 접기 (보드 가리지 않게)
            _panel.gameObject.SetActive(false);
        }

        private void OnTrialAction()
        {
            // 제한 초과 실시간 경고는 HUD 이동 수로 충분 — 필요 시 여기서 강조 연출 추가
        }

        private void OnTrialCleared(GameSession session)
        {
            int moves = session.MoveCount;
            EndTrialPuzzle();

            int dmg = _battle.ResolveTrial(moves, _currentLimit, cleared: true);
            Refresh();
            SetLog(_battle.Log + (dmg > 0 ? $"  (내 HP -{dmg})" : ""));

            if (_battle.Phase == BattlePhase.Victory) { End(true); return; }
            if (_battle.Phase == BattlePhase.Defeat) { End(false); return; }
            ShowActions(true);
            _telegraphText.text = "";
        }

        private void EndTrialPuzzle()
        {
            if (_puzzleRoot != null) Destroy(_puzzleRoot);
            _puzzleRoot = null;
            _panel.gameObject.SetActive(true);
        }

        private void End(bool victory)
        {
            EndTrialPuzzle();
            if (victory)
            {
                SfxPlayer.Instance?.Clear();
                if (!string.IsNullOrEmpty(_battle.Config.rewardConsonant))
                    ProgressStore.RecoverConsonant(_battle.Config.rewardConsonant[0]);
                ProgressStore.SetBossDefeated(_battle.Config.id);
            }
            Finished?.Invoke(victory);
            Destroy(_panel.gameObject);
            Destroy(gameObject);
        }
    }
}
