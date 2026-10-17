using System;
using System.Collections.Generic;
using UnityEngine;
using GameBalap.Core;

namespace GameBalap.Economy
{
    [System.Serializable]
    public class WalletDataWrapper
    {
        public int balance;
        public List<TransactionData> transactions = new List<TransactionData>();
    }

    /// <summary>
    /// Mengelola wallet koin virtual pemain, riwayat transaksi, dan pemotongan entry fee serta pembayaran hadiah.
    /// CATATAN: Ini adalah implementasi virtual/mock testing untuk prototipe (TIDAK terhubung ke uang asli).
    /// </summary>
    public class WalletManager : MonoBehaviour
    {
        public static WalletManager Instance { get; private set; }

        private const string PREFS_WALLET_KEY = "GameBalap_VirtualWallet_Data";

        [Header("Runtime State")]
        [SerializeField] private int currentBalance = 0;
        [SerializeField] private List<TransactionData> transactionHistory = new List<TransactionData>();

        // Events
        public event Action<int> OnBalanceChanged;
        public event Action<TransactionData> OnTransactionAdded;

        public int CurrentBalance => currentBalance;
        public IReadOnlyList<TransactionData> History => transactionHistory;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadWallet();
        }

        private void LoadWallet()
        {
            if (PlayerPrefs.HasKey(PREFS_WALLET_KEY))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PREFS_WALLET_KEY);
                    WalletDataWrapper data = JsonUtility.FromJson<WalletDataWrapper>(json);
                    if (data != null)
                    {
                        currentBalance = data.balance;
                        transactionHistory = data.transactions ?? new List<TransactionData>();
                        Debug.Log($"[WalletManager] Wallet berhasil dimuat. Saldo: {currentBalance} Coins.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WalletManager] Gagal parse data wallet: {ex.Message}. Membuat wallet baru.");
                }
            }

            // Inisialisasi awal dengan saldo testing
            currentBalance = GameConstants.INITIAL_TEST_COINS;
            transactionHistory = new List<TransactionData>();
            RecordTransaction(TransactionType.InitialGrant, currentBalance, "Bonus Koin Awal Pemain Baru (Testing)");
            SaveWallet();
        }

        private void SaveWallet()
        {
            WalletDataWrapper data = new WalletDataWrapper
            {
                balance = currentBalance,
                transactions = transactionHistory
            };

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PREFS_WALLET_KEY, json);
            PlayerPrefs.Save();
        }

        private void RecordTransaction(TransactionType type, int amount, string description, string raceId = "")
        {
            TransactionData tx = new TransactionData(type, amount, currentBalance, description, raceId);
            transactionHistory.Insert(0, tx); // Simpan yang terbaru di posisi paling atas

            // Batasi riwayat di memori sampai 50 transaksi terakhir
            if (transactionHistory.Count > 50)
            {
                transactionHistory.RemoveAt(transactionHistory.Count - 1);
            }

            SaveWallet();
            OnTransactionAdded?.Invoke(tx);
            OnBalanceChanged?.Invoke(currentBalance);
        }

        /// <summary>
        /// Validasi dan potong biaya masuk sebelum balapan dimulai.
        /// Mengembalikan true jika saldo mencukupi, false jika kurang.
        /// </summary>
        public bool TryDeductEntryFee(RaceTier tier, string raceId, out string errorMessage)
        {
            int fee = (int)tier;
            if (currentBalance < fee)
            {
                errorMessage = $"Saldo koin tidak cukup! Butuh {fee} koin, saldo Anda {currentBalance} koin.";
                return false;
            }

            currentBalance -= fee;
            errorMessage = string.Empty;

            RecordTransaction(
                TransactionType.RaceEntryFee,
                -fee,
                $"Entry Fee Balapan Tier {tier} ({fee} Coins)",
                raceId
            );

            Debug.Log($"[WalletManager] Entry fee dipotong: {fee} koin. Sisa saldo: {currentBalance}.");
            return true;
        }

        /// <summary>
        /// Memberikan pembayaran kemenangan (koin entry + pengali tier) jika pemain juara 1.
        /// Jika kalah, koin entry sudah hangus (tidak ada tambahan).
        /// </summary>
        public int CreditWinnings(RaceTier tier, string raceId, int finishPosition)
        {
            if (finishPosition == 1)
            {
                int payout = GameConstants.CalculatePayout(tier);
                currentBalance += payout;

                RecordTransaction(
                    TransactionType.RaceReward,
                    payout,
                    $"Hadiah Juara 1 Tier {tier} ({GameConstants.GetMultiplier(tier)}x Entry)",
                    raceId
                );

                Debug.Log($"[WalletManager] Hadiah dikreditkan: +{payout} koin. Saldo baru: {currentBalance}.");
                return payout;
            }
            else
            {
                Debug.Log($"[WalletManager] Pemain finish di posisi #{finishPosition}. Entry fee hangus.");
                return 0;
            }
        }

        /// <summary>
        /// Menambahkan koin virtual (misal dari pengambilan koin di lintasan balap).
        /// </summary>
        public void AddCoins(int amount, string description = "Koin Diambil di Lintasan")
        {
            if (amount <= 0) return;
            currentBalance += amount;
            RecordTransaction(TransactionType.TrackCoinPickup, amount, description);
            Debug.Log($"[WalletManager] Koin bertambah: +{amount}. Saldo: {currentBalance}.");
        }

        /// <summary>
        /// Mengurangi koin untuk pembelian di Toko Kustomisasi (Skins, Trails, Upgrades).
        /// </summary>
        public bool TrySpendCoins(int amount, string description, out string errorMessage)
        {
            if (currentBalance < amount)
            {
                errorMessage = $"Saldo koin tidak mencukupi! Butuh {amount} koin, saldo Anda {currentBalance} koin.";
                return false;
            }

            currentBalance -= amount;
            errorMessage = string.Empty;
            RecordTransaction(TransactionType.ShopPurchase, -amount, description);
            Debug.Log($"[WalletManager] Koin dibelanjakan: -{amount}. Sisa saldo: {currentBalance}.");
            return true;
        }

        /// <summary>
        /// Fitur testing: Tambah koin gratis untuk mencoba berbagai tier balapan.
        /// </summary>
        public void AddTestingFaucet(int amount = GameConstants.FAUCET_TEST_AMOUNT)
        {
            currentBalance += amount;
            RecordTransaction(TransactionType.TestFaucet, amount, $"Top-up Uji Coba (+{amount} Virtual Coins)");
            Debug.Log($"[WalletManager] Test faucet: +{amount} koin. Saldo: {currentBalance}.");
        }

        /// <summary>
        /// Reset wallet ke kondisi awal.
        /// </summary>
        public void ResetWalletForTesting()
        {
            PlayerPrefs.DeleteKey(PREFS_WALLET_KEY);
            currentBalance = GameConstants.INITIAL_TEST_COINS;
            transactionHistory.Clear();
            RecordTransaction(TransactionType.InitialGrant, currentBalance, "Reset Akun Testing");
            SaveWallet();
        }
    }
}
