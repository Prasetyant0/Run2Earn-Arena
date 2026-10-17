using System;
using UnityEngine;

namespace GameBalap.Customization
{
    [Serializable]
    public class ShopItemData
    {
        public string id;
        public string name;
        public string description;
        public ShopCategory category;
        public int price;
        public bool isDefaultUnlocked;

        // Customization Visual (Skin)
        public Color shirtColor = Color.white;
        public Color pantsColor = Color.black;
        public Color hairColor = Color.black;
        public Color skinColor = new Color(0.98f, 0.82f, 0.68f);
        public Color shoeColor = Color.white;

        // Customization Visual (Trail)
        public Color trailColor = Color.cyan;

        // Upgrade Stats
        public StatUpgradeType statType = StatUpgradeType.None;
        public float statBonusValue = 0f; // Misal 0.10f untuk +10%

        public ShopItemData(string id, string name, string description, ShopCategory category, int price, bool isDefault = false)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.category = category;
            this.price = price;
            this.isDefaultUnlocked = isDefault;
        }
    }
}
