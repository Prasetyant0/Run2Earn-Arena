using System;
using System.Collections.Generic;
using UnityEngine;
using GameBalap.Auth;
using GameBalap.Economy;

namespace GameBalap.Social
{
    [System.Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string playerName;
        public int totalWins;
        public int coinBalance;
        public bool isCurrentPlayer;
    }

    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        [SerializeField] private List<LeaderboardEntry> cachedEntries = new List<LeaderboardEntry>();

        public IReadOnlyList<LeaderboardEntry> Entries => cachedEntries;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            GenerateMockLeaderboard();
        }

        public void GenerateMockLeaderboard()
        {
            cachedEntries.Clear();

            string currentPlayerName = AccountManager.Instance != null ? AccountManager.Instance.Profile.displayName : "Pemain_Kamu";
            int playerWins = AccountManager.Instance != null ? AccountManager.Instance.Profile.totalWins : 0;
            int playerCoins = WalletManager.Instance != null ? WalletManager.Instance.CurrentBalance : 1500;

            // Data simulasi pemain top
            cachedEntries.Add(new LeaderboardEntry { rank = 1, playerName = "ApexViper", totalWins = 142, coinBalance = 48500, isCurrentPlayer = false });
            cachedEntries.Add(new LeaderboardEntry { rank = 2, playerName = "NitroStorm", totalWins = 118, coinBalance = 36200, isCurrentPlayer = false });
            cachedEntries.Add(new LeaderboardEntry { rank = 3, playerName = "CyberBlade", totalWins = 95, coinBalance = 24800, isCurrentPlayer = false });
            cachedEntries.Add(new LeaderboardEntry { rank = 4, playerName = "GhostRider", totalWins = 64, coinBalance = 15300, isCurrentPlayer = false });
            cachedEntries.Add(new LeaderboardEntry { rank = 5, playerName = currentPlayerName, totalWins = playerWins, coinBalance = playerCoins, isCurrentPlayer = true });
            cachedEntries.Add(new LeaderboardEntry { rank = 6, playerName = "ShadowFlash", totalWins = 28, coinBalance = 4200, isCurrentPlayer = false });
            cachedEntries.Add(new LeaderboardEntry { rank = 7, playerName = "RookieRacer", totalWins = 12, coinBalance = 1800, isCurrentPlayer = false });
        }
    }
}
