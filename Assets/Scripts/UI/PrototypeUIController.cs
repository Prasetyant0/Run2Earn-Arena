using System.Collections.Generic;
using UnityEngine;
using GameBalap.Core;
using GameBalap.Economy;
using GameBalap.Auth;
using GameBalap.Gameplay.Race;
using GameBalap.Gameplay.Player;
using GameBalap.Gameplay.PowerUps;
using GameBalap.Social;
using GameBalap.Audio;
using GameBalap.Customization;

namespace GameBalap.UI
{
    /// <summary>
    /// UI Controller bawaan prototipe yang langsung aktif otomatis di Unity Editor dan Mobile 
    /// tanpa mewajibkan setup Canvas manual yang rumit. 
    /// Menyediakan antarmuka Lobby, HUD Balapan, Status Power-Up, Toko Kustomisasi & Upgrade,
    /// Hasil Payout, Floating Coin Popups, dan Riwayat Transaksi Koin.
    /// </summary>
    public class PrototypeUIController : MonoBehaviour
    {
        private class FloatingCoinText
        {
            public string text;
            public Vector3 worldPos;
            public float timer;
            public float maxTime = 1.2f;
        }

        [Header("Modal States")]
        private bool showHistoryModal = false;
        private bool showLeaderboardModal = false;
        private bool showShopModal = false;
        private ShopCategory selectedShopTab = ShopCategory.Skins;

        private string alertMessage = "";
        private float alertTimer = 0f;
        private Vector2 historyScrollPos;
        private Vector2 leaderboardScrollPos;
        private Vector2 shopScrollPos;

        private readonly List<FloatingCoinText> floatingCoins = new List<FloatingCoinText>();

        // Cached references
        private RaceManager raceManager;
        private WalletManager walletManager;
        private AccountManager accountManager;
        private PlayerController player;
        private PlayerPowerUpManager powerUpManager;

        // Result cached data
        private RaceResultData lastResult;
        private int countdownNumber = 3;
        private string matchedOpponentName = "";
        private int currentLiveRank = 1;
        private float currentProgress = 0f;
        private float currentSpeed = 0f;

        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle cardStyle;
        private GUIStyle buttonStyle;
        private GUIStyle tierBtnStyle;
        private GUIStyle badgeStyle;
        private GUIStyle txItemStyle;
        private GUIStyle floatingTextStyle;
        private Texture2D cardBgTex;
        private Texture2D darkOverlayTex;

        private void Start()
        {
            raceManager = RaceManager.Instance != null ? RaceManager.Instance : UnityHelper.FindInstance<RaceManager>();
            walletManager = WalletManager.Instance;
            accountManager = AccountManager.Instance;

            if (raceManager != null)
            {
                raceManager.OnRaceStateChanged += HandleRaceStateChanged;
                raceManager.OnCountdownTick += HandleCountdownTick;
                raceManager.OnLiveTelemetryUpdated += HandleLiveTelemetry;
                raceManager.OnRaceFinished += HandleRaceFinished;
                raceManager.OnMatchmakingStarted += HandleMatchmakingStarted;
            }

            FindAndHookPlayer();
            InitStyles();
        }

        private void OnDestroy()
        {
            if (raceManager != null)
            {
                raceManager.OnRaceStateChanged -= HandleRaceStateChanged;
                raceManager.OnCountdownTick -= HandleCountdownTick;
                raceManager.OnLiveTelemetryUpdated -= HandleLiveTelemetry;
                raceManager.OnRaceFinished -= HandleRaceFinished;
                raceManager.OnMatchmakingStarted -= HandleMatchmakingStarted;
            }

            if (powerUpManager != null)
            {
                powerUpManager.OnCoinCollected -= HandleCoinCollected;
            }
        }

        private void FindAndHookPlayer()
        {
            if (player == null)
            {
                player = UnityHelper.FindInstance<PlayerController>();
            }

            if (player != null && powerUpManager == null)
            {
                powerUpManager = player.GetComponent<PlayerPowerUpManager>() ?? player.gameObject.AddComponent<PlayerPowerUpManager>();
                if (powerUpManager != null)
                {
                    powerUpManager.OnCoinCollected += HandleCoinCollected;
                }
            }
        }

        private void Update()
        {
            if (alertTimer > 0)
            {
                alertTimer -= Time.deltaTime;
                if (alertTimer <= 0) alertMessage = "";
            }

            // Update & buang popup koin melayang yang sudah expired
            for (int i = floatingCoins.Count - 1; i >= 0; i--)
            {
                floatingCoins[i].timer -= Time.deltaTime;
                if (floatingCoins[i].timer <= 0f)
                {
                    floatingCoins.RemoveAt(i);
                }
            }

            if (player == null || powerUpManager == null)
            {
                FindAndHookPlayer();
            }
        }

        private void HandleCoinCollected(int amount, Vector3 worldPos)
        {
            floatingCoins.Add(new FloatingCoinText
            {
                text = $"+{amount} 🪙",
                worldPos = worldPos,
                timer = 1.2f,
                maxTime = 1.2f
            });
        }

        private void HandleRaceStateChanged(RaceState state)
        {
            if (state == RaceState.Idle)
            {
                lastResult = null;
            }
        }

        private void HandleCountdownTick(int tick)
        {
            countdownNumber = tick;
        }

        private void HandleLiveTelemetry(int rank, float progress, float speed)
        {
            currentLiveRank = rank;
            currentProgress = progress;
            currentSpeed = speed;
        }

        private void HandleRaceFinished(RaceResultData result)
        {
            lastResult = result;
        }

        private void HandleMatchmakingStarted(string opponent)
        {
            matchedOpponentName = opponent;
        }

        private void ShowAlert(string msg)
        {
            alertMessage = msg;
            alertTimer = 3.5f;
        }

        private void InitStyles()
        {
            cardBgTex = MakeTex(2, 2, new Color(0.12f, 0.14f, 0.20f, 0.92f));
            darkOverlayTex = MakeTex(2, 2, new Color(0.05f, 0.06f, 0.10f, 0.96f));
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void EnsureStyles()
        {
            if (headerStyle != null) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.95f, 1f) }
            };

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.85f, 0.95f) }
            };

            cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(16, 16, 16, 16),
                normal = { background = cardBgTex }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            tierBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            badgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            txItemStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
            };

            floatingTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.85f, 0.1f) }
            };
        }

        private void OnGUI()
        {
            EnsureStyles();

            RaceState state = raceManager != null ? raceManager.CurrentState : RaceState.Idle;

            // Gambar UI sesuai State
            switch (state)
            {
                case RaceState.Idle:
                    DrawLobbyUI();
                    break;
                case RaceState.Matchmaking:
                    DrawMatchmakingUI();
                    break;
                case RaceState.Countdown:
                    DrawCountdownUI();
                    DrawInGameHUD();
                    break;
                case RaceState.Racing:
                    DrawInGameHUD();
                    break;
                case RaceState.Finished:
                case RaceState.ResultDisplayed:
                    if (lastResult != null) DrawResultUI();
                    break;
            }

            // Gambar angka koin melayang
            DrawFloatingCoins();

            // Modal Riwayat Transaksi
            if (showHistoryModal)
            {
                DrawTransactionHistoryModal();
            }

            // Modal Leaderboard
            if (showLeaderboardModal)
            {
                DrawLeaderboardModal();
            }

            // Modal Toko Kustomisasi & Upgrade
            if (showShopModal)
            {
                DrawShopModal();
            }

            // Pesan Alert Toast
            if (!string.IsNullOrEmpty(alertMessage))
            {
                DrawAlertToast();
            }
        }

        private void DrawLobbyUI()
        {
            int screenW = Screen.width;
            int screenH = Screen.height;

            // 1. Top Bar: Saldo Koin, Audio Toggle & Profil Pemain
            int topBarH = 65;
            GUI.Box(new Rect(10, 10, screenW - 20, topBarH), "", cardStyle);

            string userName = accountManager != null ? accountManager.Profile.displayName : "Pemain";
            int userLvl = accountManager != null ? accountManager.Profile.level : 1;
            int balance = walletManager != null ? walletManager.CurrentBalance : 0;
            float winRate = accountManager != null ? accountManager.Profile.WinRate : 0f;

            GUI.Label(new Rect(25, 16, 320, 25), $"<b>{userName}</b> (Lv.{userLvl}) | Win Rate: {winRate:F0}%", subHeaderStyle);

            // Tombol Audio Mute
            bool isMuted = AudioManager.Instance != null && AudioManager.Instance.IsMuted;
            string audioIcon = isMuted ? "🔇 Muted" : "🔊 Audio";
            if (GUI.Button(new Rect(screenW - 350, 18, 90, 38), audioIcon, buttonStyle))
            {
                AudioManager.Instance?.ToggleMute();
            }

            // Badge Koin Virtual
            GUI.backgroundColor = new Color(0.9f, 0.75f, 0.1f);
            GUI.Box(new Rect(screenW - 240, 18, 210, 38), $"💰 {balance:N0} COINS", badgeStyle);
            GUI.backgroundColor = Color.white;

            // 2. Central Panel: Pilihan Balapan & Tier Masuk
            float panelW = Mathf.Min(500, screenW - 40);
            float panelH = 475;
            float panelX = (screenW - panelW) * 0.5f;
            float panelY = topBarH + 20;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "", cardStyle);

            GUILayout.BeginArea(new Rect(panelX + 20, panelY + 15, panelW - 40, panelH - 30));
            {
                GUILayout.Label("🏁 ARENA BALAP LARI 3D", headerStyle);
                GUILayout.Label("Pilih Tier Entry Fee Koin Virtual untuk Bertanding:", subHeaderStyle);
                GUILayout.Space(10);

                // Tier 1: Bronze
                DrawTierCard(RaceTier.Bronze, "BRONZE TIER\n<color=#00FFFF>[Cyberpunk City]</color>", "100", "2.0x", "200 Coins", new Color(0.8f, 0.5f, 0.2f));
                GUILayout.Space(6);

                // Tier 2: Silver
                DrawTierCard(RaceTier.Silver, "SILVER TIER\n<color=#FFAA00>[Desert Dune]</color>", "500", "2.2x", "1,100 Coins", new Color(0.75f, 0.85f, 0.95f));
                GUILayout.Space(6);

                // Tier 3: Gold
                DrawTierCard(RaceTier.Gold, "GOLD TIER\n<color=#FF4400>[Volcanic Magma]</color>", "1,000", "2.5x", "2,500 Coins", new Color(1.0f, 0.8f, 0.2f));
                GUILayout.Space(12);

                // Action Bar (Toko, Riwayat, Leaderboard, Faucet)
                GUILayout.BeginHorizontal();
                {
                    GUI.backgroundColor = new Color(0.2f, 0.65f, 1.0f);
                    if (GUILayout.Button("🛒 Toko & Skin", buttonStyle, GUILayout.Height(36)))
                    {
                        showShopModal = true;
                        showHistoryModal = false;
                        showLeaderboardModal = false;
                    }

                    GUI.backgroundColor = Color.white;
                    if (GUILayout.Button("📜 Riwayat", buttonStyle, GUILayout.Height(36)))
                    {
                        showHistoryModal = true;
                        showLeaderboardModal = false;
                        showShopModal = false;
                    }

                    if (GUILayout.Button("🏆 Peringkat", buttonStyle, GUILayout.Height(36)))
                    {
                        showLeaderboardModal = true;
                        showHistoryModal = false;
                        showShopModal = false;
                        LeaderboardManager.Instance?.GenerateMockLeaderboard();
                    }

                    GUI.backgroundColor = new Color(0.2f, 0.75f, 0.4f);
                    if (GUILayout.Button("+500 Faucet", buttonStyle, GUILayout.Height(36)))
                    {
                        walletManager?.AddTestingFaucet(500);
                        ShowAlert("Berhasil klaim +500 koin virtual gratis!");
                    }
                    GUI.backgroundColor = Color.white;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                GUILayout.Label("*Versi prototipe: Menggunakan mata uang virtual simulasi (tanpa uang riil).", subHeaderStyle);
                GUI.color = Color.white;
            }
            GUILayout.EndArea();
        }

        private void DrawTierCard(RaceTier tier, string tierTitle, string fee, string mult, string payout, Color accentCol)
        {
            GUI.color = accentCol;
            GUILayout.BeginVertical(GUI.skin.box);
            GUI.color = Color.white;
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"<b>{tierTitle}</b>", GUILayout.Width(170));
                    GUILayout.Label($"Entry: <b>{fee}</b>\nMultiplier: <b>{mult}</b>", GUILayout.Width(110));
                    GUILayout.Label($"Hadiah Juara:\n<b><color=#FFFF00>{payout}</color></b>", GUILayout.Width(110));

                    if (GUILayout.Button("MASUK", tierBtnStyle, GUILayout.Width(75), GUILayout.Height(38)))
                    {
                        if (raceManager != null)
                        {
                            if (!raceManager.StartRaceWithTier(tier, out string err))
                            {
                                ShowAlert(err);
                            }
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawMatchmakingUI()
        {
            int screenW = Screen.width;
            int screenH = Screen.height;

            GUI.DrawTexture(new Rect(0, 0, screenW, screenH), darkOverlayTex);

            float w = 380;
            float h = 180;
            float x = (screenW - w) * 0.5f;
            float y = (screenH - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), "", cardStyle);
            GUILayout.BeginArea(new Rect(x + 20, y + 20, w - 40, h - 40));
            {
                GUILayout.Label("⚔️ MENCARI LAWAN...", headerStyle);
                GUILayout.Space(10);
                GUILayout.Label($"Lawan Ditemukan: <b><color=#FF5555>{matchedOpponentName}</color></b>", subHeaderStyle);
                GUILayout.Label($"Tier Balapan: <b>{raceManager?.CurrentTier}</b> ({(int)raceManager?.CurrentTier} Coins)", subHeaderStyle);
                GUILayout.Space(10);
                GUILayout.Label("Menyiapkan lintasan balap...", subHeaderStyle);
            }
            GUILayout.EndArea();
        }

        private void DrawCountdownUI()
        {
            int screenW = Screen.width;
            int screenH = Screen.height;

            string text = countdownNumber > 0 ? countdownNumber.ToString() : "GO!";
            GUIStyle countStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 72,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = countdownNumber > 0 ? Color.yellow : Color.green }
            };

            GUI.Label(new Rect((screenW - 300) * 0.5f, (screenH - 150) * 0.5f, 300, 150), text, countStyle);
        }

        private void DrawInGameHUD()
        {
            int screenW = Screen.width;

            // Live Rank Badge
            bool isFirst = currentLiveRank == 1;
            GUI.backgroundColor = isFirst ? new Color(1f, 0.8f, 0.1f) : new Color(0.9f, 0.25f, 0.25f);
            string rankText = isFirst ? "🥇 JUARA 1 (MEMIMPIN)" : "🥈 JUARA 2 (MENGEJAR)";
            GUI.Box(new Rect(15, 15, 250, 48), rankText, badgeStyle);
            GUI.backgroundColor = Color.white;

            // Koin Terkumpul dalam Balapan Ini
            int collectedCoins = powerUpManager != null ? powerUpManager.CoinsCollectedThisRace : 0;
            GUI.backgroundColor = new Color(1f, 0.8f, 0.2f);
            GUI.Box(new Rect(15, 68, 175, 30), $"🪙 Koin: +{collectedCoins}", badgeStyle);
            GUI.backgroundColor = Color.white;

            // Indikator Power-Up Aktif
            float powerUpY = 104;
            if (powerUpManager != null)
            {
                if (powerUpManager.IsSpeedBoosting)
                {
                    GUI.backgroundColor = new Color(0.0f, 0.95f, 1.0f);
                    GUI.Box(new Rect(15, powerUpY, 215, 28), $"⚡ SPEED BOOST: {powerUpManager.SpeedBoostRemaining:F1}s", badgeStyle);
                    powerUpY += 32;
                }

                if (powerUpManager.HasShield)
                {
                    GUI.backgroundColor = new Color(0.1f, 1.0f, 0.5f);
                    GUI.Box(new Rect(15, powerUpY, 175, 28), "🛡️ PERISAI AKTIF", badgeStyle);
                    powerUpY += 32;
                }

                if (powerUpManager.IsMagnetActive)
                {
                    GUI.backgroundColor = new Color(0.9f, 0.2f, 1.0f);
                    GUI.Box(new Rect(15, powerUpY, 205, 28), $"🧲 MAGNET: {powerUpManager.MagnetRemaining:F1}s", badgeStyle);
                    powerUpY += 32;
                }
            }
            GUI.backgroundColor = Color.white;

            // Progress Bar Lintasan
            float barW = Mathf.Min(350, screenW - 290);
            GUI.Box(new Rect(screenW - barW - 15, 15, barW, 20), "");
            GUI.backgroundColor = Color.cyan;
            GUI.Box(new Rect(screenW - barW - 15, 15, barW * currentProgress, 20), "");
            GUI.backgroundColor = Color.white;

            GUI.Label(new Rect(screenW - barW - 15, 36, barW, 20), $"Jarak Tempuh: {(currentProgress * 100f):F0}% | Kec: {currentSpeed:F1} m/s", subHeaderStyle);

            // Petunjuk Kontrol di bagian bawah layar
            string helpText = "Kontrol: <b>[A/D]</b> atau <b>Swipe Kiri/Kanan</b> ganti jalur | <b>[W/Space]</b> lompat | <b>[S]</b> slide";
            GUI.Box(new Rect(15, Screen.height - 40, screenW - 30, 30), "", cardStyle);
            GUI.Label(new Rect(20, Screen.height - 38, screenW - 40, 25), helpText, subHeaderStyle);
        }

        private void DrawFloatingCoins()
        {
            Camera cam = Camera.main;
            if (cam == null || floatingCoins.Count == 0) return;

            foreach (var item in floatingCoins)
            {
                Vector3 screenPos = cam.WorldToScreenPoint(item.worldPos);
                if (screenPos.z > 0)
                {
                    // GUI Y berbanding terbalik dengan Screen Y
                    float guiY = Screen.height - screenPos.y;
                    float progress = 1f - (item.timer / item.maxTime);
                    guiY -= progress * 50f; // Melayang ke atas

                    Color oldColor = GUI.color;
                    GUI.color = new Color(1.0f, 0.85f, 0.1f, item.timer / item.maxTime);
                    GUI.Label(new Rect(screenPos.x - 75, guiY - 15, 150, 30), item.text, floatingTextStyle);
                    GUI.color = oldColor;
                }
            }
        }

        private void DrawResultUI()
        {
            int screenW = Screen.width;
            int screenH = Screen.height;

            GUI.DrawTexture(new Rect(0, 0, screenW, screenH), darkOverlayTex);

            float w = Mathf.Min(460, screenW - 30);
            float h = 470;
            float x = (screenW - w) * 0.5f;
            float y = (screenH - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), "", cardStyle);
            GUILayout.BeginArea(new Rect(x + 25, y + 20, w - 50, h - 40));
            {
                if (lastResult.isWinner)
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label("🏆 VICTORY! JUARA 1", headerStyle);
                }
                else
                {
                    GUI.color = new Color(1f, 0.4f, 0.4f);
                    GUILayout.Label("💔 DEFEAT! JUARA 2", headerStyle);
                }
                GUI.color = Color.white;

                GUILayout.Label($"Lawan: {lastResult.opponentName}", subHeaderStyle);
                GUILayout.Space(10);

                // Rincian Waktu
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label($"⏱️ Waktu Anda: <b>{lastResult.playerTime:F2} detik</b>");
                    GUILayout.Label($"⏱️ Waktu Lawan: <b>{lastResult.opponentTime:F2} detik</b>");
                    GUILayout.Label($"🛡️ Status Validasi: <color=#88FF88>{lastResult.validationToken}</color>");
                }
                GUILayout.EndVertical();

                GUILayout.Space(10);

                // Rincian Finansial Koin Virtual
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("<b>Rincian Koin Virtual:</b>");
                    GUILayout.Label($"Entry Fee Terpotong: <color=#FF8888>-{lastResult.entryFee} Coins</color>");
                    float mult = GameConstants.GetMultiplier(lastResult.tier);
                    GUILayout.Label($"Multiplier Tier: {mult}x");

                    if (lastResult.isWinner)
                    {
                        GUILayout.Label($"Hadiah Menang: <color=#88FF88>+{lastResult.payoutAmount} Coins</color>");
                        int netProfit = lastResult.payoutAmount - lastResult.entryFee;
                        GUILayout.Label($"Keuntungan Bersih: <b>+{netProfit} Coins</b>");
                    }
                    else
                    {
                        GUILayout.Label("Hadiah Menang: 0 Coins (Entry Hangus)");
                    }

                    GUILayout.Label($"<b>Saldo Koin Sekarang: {lastResult.updatedBalance:N0} Coins</b>");
                }
                GUILayout.EndVertical();

                GUILayout.Space(15);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("MAIN LAGI", buttonStyle, GUILayout.Height(42)))
                    {
                        if (raceManager != null)
                        {
                            if (!raceManager.StartRaceWithTier(lastResult.tier, out string err))
                            {
                                ShowAlert(err);
                            }
                        }
                    }

                    if (GUILayout.Button("KEMBALI KE LOBBY", buttonStyle, GUILayout.Height(42)))
                    {
                        if (raceManager != null)
                        {
                            raceManager.StartCoroutine(ResetToLobbyRoutine());
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private System.Collections.IEnumerator ResetToLobbyRoutine()
        {
            lastResult = null;
            yield return null;
            raceManager?.ResetToLobby();
        }

        private void DrawShopModal()
        {
            int screenW = Screen.width;
            int screenH = Screen.height;

            GUI.DrawTexture(new Rect(0, 0, screenW, screenH), darkOverlayTex);

            float w = Mathf.Min(560, screenW - 20);
            float h = Mathf.Min(520, screenH - 30);
            float x = (screenW - w) * 0.5f;
            float y = (screenH - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), "", cardStyle);
            GUILayout.BeginArea(new Rect(x + 20, y + 15, w - 40, h - 30));
            {
                // Header
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("🛒 TOKO KUSTOMISASI & UPGRADE", headerStyle);
                    if (GUILayout.Button("✕", GUILayout.Width(35), GUILayout.Height(30)))
                    {
                        showShopModal = false;
                    }
                }
                GUILayout.EndHorizontal();

                // Balance
                GUILayout.Label($"Saldo Koin Anda: <b><color=#FFD700>{walletManager?.CurrentBalance:N0} Coins</color></b>", subHeaderStyle);
                GUILayout.Space(6);

                // Kategori Tabs
                GUILayout.BeginHorizontal();
                {
                    GUI.backgroundColor = selectedShopTab == ShopCategory.Skins ? Color.cyan : Color.gray;
                    if (GUILayout.Button("🥋 Karakter (Skins)", buttonStyle, GUILayout.Height(32)))
                    {
                        selectedShopTab = ShopCategory.Skins;
                    }

                    GUI.backgroundColor = selectedShopTab == ShopCategory.Trails ? Color.cyan : Color.gray;
                    if (GUILayout.Button("✨ Jejak (Trails)", buttonStyle, GUILayout.Height(32)))
                    {
                        selectedShopTab = ShopCategory.Trails;
                    }

                    GUI.backgroundColor = selectedShopTab == ShopCategory.Upgrades ? Color.cyan : Color.gray;
                    if (GUILayout.Button("⚡ Status Upgrades", buttonStyle, GUILayout.Height(32)))
                    {
                        selectedShopTab = ShopCategory.Upgrades;
                    }
                    GUI.backgroundColor = Color.white;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(8);

                // Item List
                shopScrollPos = GUILayout.BeginScrollView(shopScrollPos, GUI.skin.box);
                {
                    var shop = ShopManager.Instance;
                    if (shop != null)
                    {
                        foreach (var item in shop.Catalog)
                        {
                            if (item.category != selectedShopTab) continue;

                            bool isUnlocked = shop.IsItemUnlocked(item.id);
                            bool isEquipped = shop.IsItemEquipped(item.id);

                            GUILayout.BeginVertical(GUI.skin.box);
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.BeginVertical();
                                    {
                                        GUILayout.Label($"<b>{item.name}</b>", headerStyle);
                                        GUILayout.Label(item.description, subHeaderStyle);
                                    }
                                    GUILayout.EndVertical();

                                    GUILayout.Space(10);

                                    // Action Area (Beli / Gunakan / Dipakai)
                                    if (isUnlocked)
                                    {
                                        if (item.category == ShopCategory.Upgrades)
                                        {
                                            GUI.color = new Color(0.2f, 1.0f, 0.4f);
                                            GUILayout.Label("<b>✓ AKTIF</b>", subHeaderStyle, GUILayout.Width(100));
                                            GUI.color = Color.white;
                                        }
                                        else if (isEquipped)
                                        {
                                            GUI.color = Color.yellow;
                                            GUILayout.Label("<b>★ DIPAKAI</b>", subHeaderStyle, GUILayout.Width(100));
                                            GUI.color = Color.white;
                                        }
                                        else
                                        {
                                            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
                                            if (GUILayout.Button("Gunakan", buttonStyle, GUILayout.Width(100), GUILayout.Height(35)))
                                            {
                                                shop.EquipItem(item.id);
                                                ShowAlert($"Kostum '{item.name}' berhasil dipakai!");
                                            }
                                            GUI.backgroundColor = Color.white;
                                        }
                                    }
                                    else
                                    {
                                        GUI.backgroundColor = new Color(1.0f, 0.75f, 0.1f);
                                        if (GUILayout.Button($"Beli\n{item.price} 💰", buttonStyle, GUILayout.Width(100), GUILayout.Height(35)))
                                        {
                                            if (shop.TryPurchaseItem(item.id, out string err))
                                            {
                                                ShowAlert($"Selamat! Anda berhasil membeli '{item.name}'!");
                                            }
                                            else
                                            {
                                                ShowAlert(err);
                                            }
                                        }
                                        GUI.backgroundColor = Color.white;
                                    }
                                }
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();
                            GUILayout.Space(4);
                        }
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.Space(8);
                if (GUILayout.Button("Tutup Toko", buttonStyle, GUILayout.Height(36)))
                {
                    showShopModal = false;
                }
            }
            GUILayout.EndArea();
        }

        private void DrawTransactionHistoryModal()
        {
            int screenW = Screen.width;
            int screenH = Screen.height;

            GUI.DrawTexture(new Rect(0, 0, screenW, screenH), darkOverlayTex);

            float w = Mathf.Min(520, screenW - 30);
            float h = Mathf.Min(500, screenH - 40);
            float x = (screenW - w) * 0.5f;
            float y = (screenH - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), "", cardStyle);
            GUILayout.BeginArea(new Rect(x + 20, y + 20, w - 40, h - 40));
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("📜 RIWAYAT TRANSAKSI KOIN VIRTUAL", headerStyle);
                    if (GUILayout.Button("✕", GUILayout.Width(35), GUILayout.Height(30)))
                    {
                        showHistoryModal = false;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUILayout.Label($"Saldo Saat Ini: <b>{walletManager?.CurrentBalance:N0} Coins</b>", subHeaderStyle);
                GUILayout.Space(8);

                // Scroll view riwayat transaksi
                historyScrollPos = GUILayout.BeginScrollView(historyScrollPos, GUI.skin.box);
                {
                    var history = walletManager != null ? walletManager.History : null;
                    if (history == null || history.Count == 0)
                    {
                        GUILayout.Label("Belum ada riwayat transaksi.", subHeaderStyle);
                    }
                    else
                    {
                        foreach (var tx in history)
                        {
                            string sign = tx.amount >= 0 ? "+" : "";
                            string colorTag = tx.amount >= 0 ? "#88FF88" : "#FF7777";

                            GUILayout.BeginVertical(GUI.skin.box);
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label($"<b>{tx.type}</b> ({tx.timestamp})", txItemStyle);
                                    GUILayout.Label($"<color={colorTag}><b>{sign}{tx.amount:N0} Coins</b></color>", txItemStyle, GUILayout.Width(130));
                                }
                                GUILayout.EndHorizontal();

                                GUILayout.Label($"{tx.description} | Saldo Akhir: {tx.balanceAfter:N0}", txItemStyle);
                            }
                            GUILayout.EndVertical();
                            GUILayout.Space(4);
                        }
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.Space(10);
                if (GUILayout.Button("Tutup", buttonStyle, GUILayout.Height(36)))
                {
                    showHistoryModal = false;
                }
            }
            GUILayout.EndArea();
        }

        private void DrawLeaderboardModal()
        {
            int screenW = Screen.width;
            int screenH = Screen.height;

            GUI.DrawTexture(new Rect(0, 0, screenW, screenH), darkOverlayTex);

            float w = Mathf.Min(520, screenW - 30);
            float h = Mathf.Min(480, screenH - 40);
            float x = (screenW - w) * 0.5f;
            float y = (screenH - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), "", cardStyle);
            GUILayout.BeginArea(new Rect(x + 20, y + 20, w - 40, h - 40));
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("🏆 LEADERBOARD PEMAIN", headerStyle);
                    if (GUILayout.Button("✕", GUILayout.Width(35), GUILayout.Height(30)))
                    {
                        showLeaderboardModal = false;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUILayout.Label("Peringkat Juara Balap Lari Berdasarkan Total Kemenangan", subHeaderStyle);
                GUILayout.Space(8);

                leaderboardScrollPos = GUILayout.BeginScrollView(leaderboardScrollPos, GUI.skin.box);
                {
                    var entries = LeaderboardManager.Instance != null ? LeaderboardManager.Instance.Entries : null;
                    if (entries == null || entries.Count == 0)
                    {
                        GUILayout.Label("Memuat data leaderboard...", subHeaderStyle);
                    }
                    else
                    {
                        foreach (var entry in entries)
                        {
                            Color bgCol = entry.isCurrentPlayer ? new Color(0.2f, 0.45f, 0.7f, 0.6f) : Color.clear;
                            if (entry.isCurrentPlayer) GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);

                            GUILayout.BeginVertical(GUI.skin.box);
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    string medal = entry.rank == 1 ? "🥇" : (entry.rank == 2 ? "🥈" : (entry.rank == 3 ? "🥉" : $"#{entry.rank}"));
                                    GUILayout.Label($"<b>{medal}</b>  {entry.playerName} {(entry.isCurrentPlayer ? "<b>(Kamu)</b>" : "")}", GUILayout.Width(220));
                                    GUILayout.Label($"Menang: <b>{entry.totalWins}x</b>", GUILayout.Width(100));
                                    GUILayout.Label($"💰 {entry.coinBalance:N0}", GUILayout.Width(100));
                                }
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();
                            if (entry.isCurrentPlayer) GUI.backgroundColor = Color.white;
                            GUILayout.Space(3);
                        }
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.Space(10);
                if (GUILayout.Button("Tutup", buttonStyle, GUILayout.Height(36)))
                {
                    showLeaderboardModal = false;
                }
            }
            GUILayout.EndArea();
        }

        private void DrawAlertToast()
        {
            float w = Mathf.Min(400, Screen.width - 40);
            float h = 48;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - 110;

            GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f, 0.95f);
            GUI.Box(new Rect(x, y, w, h), alertMessage, badgeStyle);
            GUI.backgroundColor = Color.white;
        }
    }
}
