using System;
using System.Collections.Generic;
using UnityEngine;
using GameBalap.Core;
using GameBalap.Economy;
using GameBalap.Gameplay.Player;

namespace GameBalap.Customization
{
    /// <summary>
    /// Pengelola Toko Kustomisasi dan Upgrade Karakter.
    /// Memungkinkan pemain membelanjakan koin virtual hasil balapan untuk kostum skin, efek jejak partikel,
    /// dan peningkatan status karakter (kecepatan, akselerasi, magnet).
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        private const string PREFS_UNLOCKED_KEY = "GameBalap_Shop_UnlockedItems";
        private const string PREFS_EQUIPPED_SKIN_KEY = "GameBalap_Shop_EquippedSkin";
        private const string PREFS_EQUIPPED_TRAIL_KEY = "GameBalap_Shop_EquippedTrail";

        [Header("Catalog")]
        private readonly List<ShopItemData> catalog = new List<ShopItemData>();
        private readonly HashSet<string> unlockedItemIds = new HashSet<string>();

        private string equippedSkinId = "skin_classic";
        private string equippedTrailId = "trail_default";

        // Events
        public event Action<ShopItemData> OnItemPurchased;
        public event Action<ShopItemData> OnItemEquipped;

        public IReadOnlyList<ShopItemData> Catalog => catalog;
        public string EquippedSkinId => equippedSkinId;
        public string EquippedTrailId => equippedTrailId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeCatalog();
            LoadData();
        }

        private void Start()
        {
            ApplyEquippedCustomization();
        }

        private void InitializeCatalog()
        {
            catalog.Clear();

            // ==================== SKINS ====================
            catalog.Add(new ShopItemData("skin_classic", "Pelari Klasik", "Jersey atletik biru standar turnamen.", ShopCategory.Skins, 0, isDefault: true)
            {
                shirtColor = new Color(0.12f, 0.55f, 0.95f),
                pantsColor = new Color(0.15f, 0.18f, 0.25f),
                hairColor = new Color(0.25f, 0.15f, 0.08f),
                skinColor = new Color(0.98f, 0.82f, 0.68f),
                shoeColor = new Color(0.95f, 0.95f, 0.95f)
            });

            catalog.Add(new ShopItemData("skin_cyber_ninja", "Cyber Ninja", "Kostum siluman masa depan beraksen garis neon cyan.", ShopCategory.Skins, 300)
            {
                shirtColor = new Color(0.08f, 0.08f, 0.12f),
                pantsColor = new Color(0.05f, 0.05f, 0.08f),
                hairColor = new Color(0.0f, 0.95f, 1.0f),
                skinColor = new Color(0.85f, 0.85f, 0.9f),
                shoeColor = new Color(0.0f, 0.95f, 1.0f)
            });

            catalog.Add(new ShopItemData("skin_magma_titan", "Magma Titan", "Baju zirah vulkanik berselimut batu hitam dan lahar merah membara.", ShopCategory.Skins, 600)
            {
                shirtColor = new Color(0.95f, 0.25f, 0.05f),
                pantsColor = new Color(0.15f, 0.08f, 0.05f),
                hairColor = new Color(1.0f, 0.5f, 0.05f),
                skinColor = new Color(0.35f, 0.25f, 0.22f),
                shoeColor = new Color(0.95f, 0.25f, 0.05f)
            });

            catalog.Add(new ShopItemData("skin_gold_champion", "Juara Emas", "Setelan mewah bertabur emas dan mantel kehormatan putih.", ShopCategory.Skins, 1200)
            {
                shirtColor = new Color(1.0f, 0.82f, 0.1f),
                pantsColor = new Color(0.95f, 0.95f, 0.95f),
                hairColor = new Color(0.95f, 0.85f, 0.3f),
                skinColor = new Color(0.98f, 0.84f, 0.7f),
                shoeColor = new Color(1.0f, 0.82f, 0.1f)
            });

            // ==================== TRAILS ====================
            catalog.Add(new ShopItemData("trail_default", "Debu Klasik", "Jejak kepulan debu lari standar.", ShopCategory.Trails, 0, isDefault: true)
            {
                trailColor = new Color(0.8f, 0.8f, 0.8f, 0.5f)
            });

            catalog.Add(new ShopItemData("trail_cyan_neon", "Kilat Neon", "Jejak partikel energi biru neon bertenaga tinggi.", ShopCategory.Trails, 250)
            {
                trailColor = new Color(0.0f, 0.95f, 1.0f, 0.9f)
            });

            catalog.Add(new ShopItemData("trail_fire_blaze", "Percikan Lahar", "Jejak bara api menyala yang membakar lintasan.", ShopCategory.Trails, 500)
            {
                trailColor = new Color(1.0f, 0.3f, 0.05f, 0.9f)
            });

            catalog.Add(new ShopItemData("trail_gold_spark", "Kilau Emas", "Jejak partikel butiran koin emas berkilauan.", ShopCategory.Trails, 800)
            {
                trailColor = new Color(1.0f, 0.85f, 0.1f, 0.9f)
            });

            // ==================== UPGRADES ====================
            catalog.Add(new ShopItemData("upgrade_speed_1", "Kecepatan Maksimal +10%", "Meningkatkan batas kecepatan tertinggi lari pemain sebesar 10%.", ShopCategory.Upgrades, 350)
            {
                statType = StatUpgradeType.MaxSpeed,
                statBonusValue = 0.10f
            });

            catalog.Add(new ShopItemData("upgrade_accel_1", "Akselerasi Kilat +15%", "Membuat karakter mencapai kecepatan puncak lebih cepat sebesar 15%.", ShopCategory.Upgrades, 400)
            {
                statType = StatUpgradeType.Acceleration,
                statBonusValue = 0.15f
            });

            catalog.Add(new ShopItemData("upgrade_magnet_1", "Radius Magnet +35%", "Memperluas jangkauan tarikan koin virtual saat Coin Magnet aktif.", ShopCategory.Upgrades, 300)
            {
                statType = StatUpgradeType.MagnetRadius,
                statBonusValue = 0.35f
            });
        }

        private void LoadData()
        {
            unlockedItemIds.Clear();

            // Default unlocked items
            foreach (var item in catalog)
            {
                if (item.isDefaultUnlocked) unlockedItemIds.Add(item.id);
            }

            // Load saved unlocked items
            if (PlayerPrefs.HasKey(PREFS_UNLOCKED_KEY))
            {
                string saved = PlayerPrefs.GetString(PREFS_UNLOCKED_KEY);
                string[] split = saved.Split(',');
                foreach (var id in split)
                {
                    if (!string.IsNullOrEmpty(id)) unlockedItemIds.Add(id);
                }
            }

            equippedSkinId = PlayerPrefs.GetString(PREFS_EQUIPPED_SKIN_KEY, "skin_classic");
            equippedTrailId = PlayerPrefs.GetString(PREFS_EQUIPPED_TRAIL_KEY, "trail_default");
        }

        private void SaveData()
        {
            string joined = string.Join(",", unlockedItemIds);
            PlayerPrefs.SetString(PREFS_UNLOCKED_KEY, joined);
            PlayerPrefs.SetString(PREFS_EQUIPPED_SKIN_KEY, equippedSkinId);
            PlayerPrefs.SetString(PREFS_EQUIPPED_TRAIL_KEY, equippedTrailId);
            PlayerPrefs.Save();
        }

        public bool IsItemUnlocked(string id)
        {
            return unlockedItemIds.Contains(id);
        }

        public bool IsItemEquipped(string id)
        {
            return equippedSkinId == id || equippedTrailId == id;
        }

        public ShopItemData GetItem(string id)
        {
            return catalog.Find(i => i.id == id);
        }

        public bool TryPurchaseItem(string id, out string errorMessage)
        {
            ShopItemData item = GetItem(id);
            if (item == null)
            {
                errorMessage = "Item tidak ditemukan!";
                return false;
            }

            if (IsItemUnlocked(id))
            {
                errorMessage = "Item ini sudah Anda miliki!";
                return false;
            }

            if (WalletManager.Instance == null)
            {
                errorMessage = "Wallet sistem tidak tersedia!";
                return false;
            }

            if (!WalletManager.Instance.TrySpendCoins(item.price, $"Beli {item.name} di Toko", out errorMessage))
            {
                return false;
            }

            unlockedItemIds.Add(id);
            SaveData();

            OnItemPurchased?.Invoke(item);

            // Auto-equip item jika skin atau trail
            if (item.category == ShopCategory.Skins || item.category == ShopCategory.Trails)
            {
                EquipItem(id);
            }

            return true;
        }

        public void EquipItem(string id)
        {
            ShopItemData item = GetItem(id);
            if (item == null || !IsItemUnlocked(id)) return;

            if (item.category == ShopCategory.Skins)
            {
                equippedSkinId = id;
            }
            else if (item.category == ShopCategory.Trails)
            {
                equippedTrailId = id;
            }

            SaveData();
            ApplyEquippedCustomization();
            OnItemEquipped?.Invoke(item);
        }

        public void ApplyEquippedCustomization()
        {
            var player = UnityHelper.FindInstance<PlayerController>();
            if (player == null) return;

            // 1. Terapkan Skin ke BlockyCharacterAnimator
            var skinItem = GetItem(equippedSkinId);
            if (skinItem != null)
            {
                var animator = player.GetComponent<BlockyCharacterAnimator>();
                if (animator != null)
                {
                    animator.ApplySkinColors(
                        skinItem.shirtColor,
                        skinItem.pantsColor,
                        skinItem.hairColor,
                        skinItem.skinColor,
                        skinItem.shoeColor
                    );
                }
            }

            // 2. Terapkan Trail ke RunnerParticleEffects
            var trailItem = GetItem(equippedTrailId);
            if (trailItem != null)
            {
                var particles = player.GetComponent<RunnerParticleEffects>();
                if (particles != null)
                {
                    particles.SetCustomTrailColor(trailItem.trailColor);
                }
            }
        }

        /// <summary>
        /// Menghitung total pengali stat dari upgrade yang sudah dibeli pemain.
        /// </summary>
        public float GetStatMultiplier(StatUpgradeType type)
        {
            float bonus = 0f;
            foreach (var item in catalog)
            {
                if (item.category == ShopCategory.Upgrades && item.statType == type && IsItemUnlocked(item.id))
                {
                    bonus += item.statBonusValue;
                }
            }
            return 1.0f + bonus;
        }
    }
}
