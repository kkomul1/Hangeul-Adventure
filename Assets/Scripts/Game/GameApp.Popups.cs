using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>GameApp의 팝업 부분: 방 보상 자음 회수, 사천왕 승리.</summary>
    public partial class GameApp
    {
        private void ShowConsonantPopup(RoomJson room)
        {
            var overlay = UiFactory.CreatePanel(_canvas.transform, "RoomRewardPopup", new Color(0, 0, 0, 0.6f));
            UiFactory.Stretch(overlay);
            var box = UiFactory.CreatePanel(overlay, "Box", UiFactory.Paper);
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 300));

            string where = string.IsNullOrEmpty(room.label) ? "이곳" : $"'{BrokenText.Apply(room.label)}'";
            var title = UiFactory.CreateText(box, "T", $"{where}의 글자 조각을 모두 맞췄다!", 30, UiFactory.Ink);
            BrokenTextFx.Ensure(title);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(520, 60));

            var msg = UiFactory.CreateText(box, "M",
                $"잃어버린 자음  '{room.reward}'  을(를) 되찾았다!\n깨져 있던 글자들이 조금 돌아왔다...", 24, UiFactory.Dim);
            UiFactory.SetRect(msg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(500, 90));

            var ok = UiFactory.CreateButton(box, "Ok", "계속", 24, UiFactory.Accent, Color.white, () =>
            {
                Destroy(overlay.gameObject);
                if (_mapWorld != null) _mapWorld.RefreshStates();
                if (_sideWorld != null) _sideWorld.RefreshStates();
                RefreshTitleBrokenness();
            });
            UiFactory.SetRect((RectTransform)ok.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(180, 58));
        }

        private void ShowVictoryPopup(BattleConfig config)
        {
            var overlay = UiFactory.CreatePanel(_canvas.transform, "VictoryPopup", new Color(0, 0, 0, 0.6f));
            UiFactory.Stretch(overlay);
            var box = UiFactory.CreatePanel(overlay, "Box", UiFactory.Paper);
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 300));

            var title = UiFactory.CreateText(box, "T", $"{config.name} 격파!", 38, UiFactory.Ink);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(500, 60));

            var msg = UiFactory.CreateText(box, "M",
                $"잃어버린 자음  '{config.rewardConsonant}'  을(를) 되찾았다!\n세상의 글자가 조금 돌아왔다...", 24, UiFactory.Dim);
            UiFactory.SetRect(msg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(500, 90));

            var ok = UiFactory.CreateButton(box, "Ok", "계속", 24, UiFactory.Accent, Color.white, () =>
            {
                Destroy(overlay.gameObject);
                ReturnToMap();
            });
            UiFactory.SetRect((RectTransform)ok.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(180, 58));
        }
    }
}
