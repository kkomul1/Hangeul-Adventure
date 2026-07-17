using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 상점/가방(인벤토리) 패널. 하나의 컴포넌트로 두 모드를 처리한다.
    /// 상점: 구매. 가방: 장착/해제 + 장착 중 스탯 합계 표시 (전투는 이후 단계).
    /// </summary>
    public class ItemPanels : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _panel;
        private RectTransform _listContent;
        private TextMeshProUGUI _title, _money, _status, _equipped;
        private bool _shopMode;

        public bool IsOpen => _panel != null && _panel.gameObject.activeSelf;
        public System.Action Closed;

        public void Build(Canvas canvas)
        {
            _canvas = canvas;
            _panel = UiFactory.CreatePanel(canvas.transform, "ItemPanel", new Color(0, 0, 0, 0.45f));
            UiFactory.Stretch(_panel);

            var box = UiFactory.CreatePanel(_panel, "Box", new Color(0.95f, 0.93f, 0.87f));
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 560));

            _title = UiFactory.CreateText(box, "Title", "", 30, UiFactory.Ink);
            UiFactory.SetRect(_title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(300, 44));

            _money = UiFactory.CreateText(box, "Money", "", 22, new Color(0.72f, 0.55f, 0.12f), TextAlignmentOptions.Right);
            UiFactory.SetRect(_money.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -26), new Vector2(240, 36));

            var closeBtn = UiFactory.CreateButton(box, "CloseBtn", "닫기", 20, UiFactory.Paper, UiFactory.Ink, Close);
            UiFactory.SetRect((RectTransform)closeBtn.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -18), new Vector2(100, 44));

            _equipped = UiFactory.CreateText(box, "Equipped", "", 18, UiFactory.Dim);
            UiFactory.SetRect(_equipped.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -66), new Vector2(660, 30));

            // 목록 (스크롤)
            var viewport = UiFactory.CreatePanel(box, "View", new Color(1, 1, 1, 0.4f));
            UiFactory.SetRect(viewport, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -34), new Vector2(670, 380));
            viewport.gameObject.AddComponent<RectMask2D>();

            _listContent = UiFactory.CreateEmpty(viewport, "Content");
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.pivot = new Vector2(0.5f, 1);
            _listContent.sizeDelta = Vector2.zero; // CreateEmpty 기본 크기 잔여 제거 (도감 버그와 동일 함정) — 남기면 행 좌우가 잘림
            var layout = _listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = _listContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = _listContent;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30;

            _status = UiFactory.CreateText(box, "Status", "", 18, UiFactory.Dim);
            UiFactory.SetRect(_status.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(660, 32));

            _panel.gameObject.SetActive(false);
        }

        public void OpenShop() => Open(shopMode: true);
        public void OpenInventory() => Open(shopMode: false);

        private void Open(bool shopMode)
        {
            _shopMode = shopMode;
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
            _status.text = "";
            Refresh();
        }

        public void Close()
        {
            _panel.gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void Refresh()
        {
            _title.text = _shopMode ? "상점" : "가방";
            _money.text = ProgressStore.Format(ProgressStore.Coins);

            var w = ItemStore.Find(ItemStore.EquippedWeapon);
            var a = ItemStore.Find(ItemStore.EquippedArmor);
            int atk = w?.atk ?? 0, def = a?.def ?? 0;
            _equipped.text = $"장착 — 무기: {(w != null ? w.name : "없음")} · 방어구: {(a != null ? a.name : "없음")}"
                + $"   (공격 {atk} / 방어 {def} — 전투는 추후 업데이트)";

            foreach (Transform c in _listContent) Destroy(c.gameObject);

            foreach (var item in ItemStore.All)
            {
                bool owned = ItemStore.IsOwned(item.id);
                if (!_shopMode && !owned) continue; // 가방에는 보유품만

                var row = UiFactory.CreatePanel(_listContent, "Row", UiFactory.Paper);
                var le = row.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 78;

                string stat = item.type == "weapon" ? $"공격 +{item.atk}"
                    : item.type == "armor" ? $"방어 +{item.def}"
                    : "2단 점프"; // mobility (갖신)
                var name = UiFactory.CreateText(row, "Name",
                    $"{item.name}  <size=70%><color=#8A8578>[{ItemStore.TypeName(item.type)} · {stat}]</color></size>",
                    21, UiFactory.Ink, TextAlignmentOptions.Left);
                UiFactory.SetRect(name.rectTransform, new Vector2(0, 0.68f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(430, 32));

                var desc = UiFactory.CreateText(row, "Desc", item.desc, 15, UiFactory.Dim, TextAlignmentOptions.Left);
                UiFactory.SetRect(desc.rectTransform, new Vector2(0, 0.26f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(430, 26));

                if (_shopMode)
                {
                    if (owned)
                    {
                        var ownedText = UiFactory.CreateText(row, "Owned", "보유 중", 18, UiFactory.Dim, TextAlignmentOptions.Right);
                        UiFactory.SetRect(ownedText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(150, 32));
                    }
                    else
                    {
                        var buy = UiFactory.CreateButton(row, "Buy", $"{ProgressStore.Format(item.price)} 구매", 18,
                            ProgressStore.Coins >= item.price ? UiFactory.Accent : SpotGray(), Color.white, () =>
                            {
                                if (ItemStore.TryBuy(item))
                                {
                                    SfxPlayer.Instance?.Collect();
                                    _status.text = $"'{item.name}' 구매 완료!";
                                    Refresh();
                                    // 팝업은 Refresh 뒤에 — 목록 재생성이 형제 순서를 건드리지 않도록
                                    if (item.type == "weapon" || item.type == "armor") ShowEquipPrompt(item);
                                }
                                else
                                {
                                    SfxPlayer.Instance?.Fail();
                                    _status.text = "돈이 부족합니다.";
                                    Refresh();
                                }
                            });
                        UiFactory.SetRect((RectTransform)buy.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(150, 50));
                    }
                }
                else if (item.type != "weapon" && item.type != "armor")
                {
                    // mobility 등 패시브: 장착 슬롯 없음 — 보유 즉시 적용 (기획 14장-3)
                    var passive = UiFactory.CreateText(row, "Passive", "효과 적용 중", 18, UiFactory.Dim, TextAlignmentOptions.Right);
                    UiFactory.SetRect(passive.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(150, 32));
                }
                else
                {
                    bool equipped = ItemStore.IsEquipped(item);
                    var btn = UiFactory.CreateButton(row, "Equip", equipped ? "해제" : "장착", 18,
                        equipped ? UiFactory.Paper : UiFactory.Accent, equipped ? UiFactory.Ink : Color.white, () =>
                        {
                            if (equipped) ItemStore.Unequip(item);
                            else ItemStore.Equip(item);
                            SfxPlayer.Instance?.Move();
                            Refresh();
                        });
                    UiFactory.SetRect((RectTransform)btn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(110, 50));
                }
            }

            if (!_shopMode)
            {
                bool any = false;
                foreach (var item in ItemStore.All) if (ItemStore.IsOwned(item.id)) { any = true; break; }
                if (!any) _status.text = "아직 아이템이 없습니다 — 시작의 마을 상점에서 구매할 수 있어요.";
            }
        }

        /// <summary>구매 직후 장착 여부를 묻는다. 빈 슬롯이어도 항상 묻는다 (mobility는 장착 슬롯이 없어 대상 밖).</summary>
        private void ShowEquipPrompt(ItemJson item)
        {
            var overlay = UiFactory.CreatePanel(_canvas.transform, "EquipPromptPopup", new Color(0, 0, 0, 0.6f));
            UiFactory.Stretch(overlay);
            var box = UiFactory.CreatePanel(overlay, "Box", UiFactory.Paper);
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 260));

            var title = UiFactory.CreateText(box, "T", "구매 완료!", 30, UiFactory.Ink);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(480, 52));

            var cur = ItemStore.Find(item.type == "weapon" ? ItemStore.EquippedWeapon : ItemStore.EquippedArmor);
            string body = cur == null
                ? $"{item.name}을(를) 장착할까요?"
                : $"{item.name}을(를) 장착할까요?\n<size=80%>(현재: {cur.name})</size>";
            var msg = UiFactory.CreateText(box, "M", body, 24, UiFactory.Dim);
            UiFactory.SetRect(msg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -4), new Vector2(460, 80));

            var equip = UiFactory.CreateButton(box, "EquipBtn", "장착", 22, UiFactory.Accent, Color.white, () =>
            {
                ItemStore.Equip(item);
                SfxPlayer.Instance?.Move();
                Destroy(overlay.gameObject);
                Refresh();
            });
            UiFactory.SetRect((RectTransform)equip.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(96, 26), new Vector2(170, 54));

            var later = UiFactory.CreateButton(box, "LaterBtn", "나중에", 22, UiFactory.Paper, UiFactory.Ink, () =>
            {
                Destroy(overlay.gameObject);
            });
            UiFactory.SetRect((RectTransform)later.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-96, 26), new Vector2(170, 54));
        }

        private static Color SpotGray() => new Color(0.72f, 0.70f, 0.66f);
    }
}
