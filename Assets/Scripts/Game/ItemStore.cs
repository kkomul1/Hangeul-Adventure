using System;
using System.Collections.Generic;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 아이템 정의 (Resources/Items/items.json)와 보유/장착 저장.
    /// 무기/방어구의 공격력·방어력은 이후 보스전/전투 시스템을 전제로 한 스탯 (D-18) —
    /// 현재는 구매·장착·표시까지만 동작한다.
    /// mobility(이동 능력)는 장착 슬롯 없는 패시브 — 보유 즉시 적용 (사이드뷰 전환 기획 14장-3).
    /// </summary>
    [Serializable]
    public class ItemJson
    {
        public string id;
        public string name;
        public string type;   // "weapon" | "armor" | "mobility"
        public int price;
        public int atk;
        public int def;
        public string desc = "";
    }

    [Serializable]
    public class ItemListJson
    {
        public ItemJson[] items;
    }

    public static class ItemStore
    {
        private static List<ItemJson> _items;

        public static IReadOnlyList<ItemJson> All
        {
            get
            {
                if (_items == null)
                {
                    _items = new List<ItemJson>();
                    var asset = Resources.Load<TextAsset>("Items/items");
                    if (asset != null)
                    {
                        var parsed = JsonUtility.FromJson<ItemListJson>(asset.text);
                        if (parsed?.items != null) _items.AddRange(parsed.items);
                    }
                    else Debug.LogError("아이템 정의 없음: Resources/Items/items.json");
                }
                return _items;
            }
        }

        public static ItemJson Find(string id) => string.IsNullOrEmpty(id) ? null : All != null ? _items.Find(i => i.id == id) : null;

        public static bool IsOwned(string id) => PlayerPrefs.GetInt($"item_{id}", 0) == 1;

        /// <summary>골드가 충분하면 구매. 성공 여부 반환.</summary>
        public static bool TryBuy(ItemJson item)
        {
            if (IsOwned(item.id)) return false;
            if (!ProgressStore.SpendGold(item.price)) return false;
            PlayerPrefs.SetInt($"item_{item.id}", 1);
            PlayerPrefs.Save();
            return true;
        }

        public static string EquippedWeapon
        {
            get => PlayerPrefs.GetString("equip_weapon", "");
            set { PlayerPrefs.SetString("equip_weapon", value ?? ""); PlayerPrefs.Save(); }
        }

        public static string EquippedArmor
        {
            get => PlayerPrefs.GetString("equip_armor", "");
            set { PlayerPrefs.SetString("equip_armor", value ?? ""); PlayerPrefs.Save(); }
        }

        public static bool IsEquipped(ItemJson item)
            => item.type == "weapon" ? EquippedWeapon == item.id
             : item.type == "armor" ? EquippedArmor == item.id
             : false; // mobility 등 패시브는 장착 슬롯 없음

        public static void Equip(ItemJson item)
        {
            if (!IsOwned(item.id)) return;
            if (item.type == "weapon") EquippedWeapon = item.id;
            else if (item.type == "armor") EquippedArmor = item.id;
            // mobility는 보유 즉시 적용 — 장착 없음
        }

        public static void Unequip(ItemJson item)
        {
            if (item.type == "weapon" && EquippedWeapon == item.id) EquippedWeapon = "";
            if (item.type == "armor" && EquippedArmor == item.id) EquippedArmor = "";
        }

        public static string TypeName(string type)
            => type == "weapon" ? "무기" : type == "armor" ? "방어구" : "이동";

        // ---- 이동 능력 (사이드뷰, 기획 14장-3) ----

        /// <summary>2단 점프 아이템 id — 갖신 (상점 구매형).</summary>
        public const string DoubleJumpId = "m_gatsin";

        /// <summary>2단 점프 가능 여부. SideWorld가 공중 점프 허용 판정에 사용.</summary>
        public static bool HasDoubleJump => IsOwned(DoubleJumpId);
    }
}
