using System;

namespace GameBalap.Auth
{
    [System.Serializable]
    public class UserProfile
    {
        public string userId;
        public string displayName;
        public int level;
        public int totalRaces;
        public int totalWins;

        public float WinRate => totalRaces > 0 ? (float)totalWins / totalRaces * 100f : 0f;

        public UserProfile()
        {
            userId = "USR-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            displayName = "Racer_" + UnityEngine.Random.Range(100, 999);
            level = 1;
            totalRaces = 0;
            totalWins = 0;
        }
    }
}
