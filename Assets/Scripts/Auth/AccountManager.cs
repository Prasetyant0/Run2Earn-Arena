using System;
using UnityEngine;

namespace GameBalap.Auth
{
    public class AccountManager : MonoBehaviour
    {
        public static AccountManager Instance { get; private set; }

        private const string PREFS_ACCOUNT_KEY = "GameBalap_Account_Profile";

        [SerializeField] private UserProfile currentProfile;

        public event Action<UserProfile> OnProfileUpdated;
        public UserProfile Profile => currentProfile;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProfile();
        }

        private void LoadProfile()
        {
            if (PlayerPrefs.HasKey(PREFS_ACCOUNT_KEY))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PREFS_ACCOUNT_KEY);
                    currentProfile = JsonUtility.FromJson<UserProfile>(json);
                    if (currentProfile != null)
                    {
                        Debug.Log($"[AccountManager] Profil berhasil dimuat: {currentProfile.displayName} ({currentProfile.userId})");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AccountManager] Gagal parse profil: {ex.Message}");
                }
            }

            // Inisialisasi profil baru jika belum ada
            currentProfile = new UserProfile();
            SaveProfile();
        }

        public void SaveProfile()
        {
            if (currentProfile == null) return;
            string json = JsonUtility.ToJson(currentProfile);
            PlayerPrefs.SetString(PREFS_ACCOUNT_KEY, json);
            PlayerPrefs.Save();
            OnProfileUpdated?.Invoke(currentProfile);
        }

        public void RecordRaceFinished(bool won)
        {
            if (currentProfile == null) return;

            currentProfile.totalRaces++;
            if (won)
            {
                currentProfile.totalWins++;
                // Level up setiap 3 kemenangan
                if (currentProfile.totalWins % 3 == 0)
                {
                    currentProfile.level++;
                }
            }

            SaveProfile();
        }

        public void UpdateDisplayName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;
            currentProfile.displayName = newName.Trim();
            SaveProfile();
        }
    }
}
