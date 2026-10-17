using System;
using System.Collections.Generic;
using UnityEngine;
using GameBalap.Core;

namespace GameBalap.NetworkValidation
{
    public class RaceValidationResult
    {
        public bool IsValid;
        public string ValidationToken;
        public string ErrorReason;
        public float ElapsedTime;
        public int VerifiedRank;
    }

    [System.Serializable]
    public class CheckpointTelemetry
    {
        public int checkpointIndex;
        public float timestamp;
        public float speedAtCheckpoint;
    }

    /// <summary>
    /// Simulasi logic server otoritatif untuk memvalidasi integritas balapan dan mencegah manipulasi (Anti-Cheat).
    /// Memverifikasi bahwa catatan waktu, checkpoint, dan jarak tempuh masuk akal secara fisik sebelum koin payout dikreditkan.
    /// </summary>
    public class RaceValidationService : MonoBehaviour
    {
        public static RaceValidationService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Validasi telemetri balapan pemain sebelum mengizinkan klaim koin kemenangan.
        /// </summary>
        public RaceValidationResult ValidateRace(
            string raceId,
            RaceTier tier,
            float startTime,
            float finishTime,
            float totalTrackDistance,
            int collisionCount,
            List<CheckpointTelemetry> checkpoints,
            int rawRank)
        {
            float elapsedTime = finishTime - startTime;

            // 1. Validasi Batas Fisika Kecepatan (Anti-Speedhack)
            // Waktu tersingkat yang dimungkinkan adalah jarak / MAX_RUN_SPEED
            float minPossibleTime = totalTrackDistance / GameConstants.MAX_RUN_SPEED;
            if (elapsedTime < minPossibleTime * 0.95f) // Buffer toleransi 5%
            {
                Debug.LogError($"[AntiCheat] Waktu finish ({elapsedTime:F2}s) lebih cepat dari batas kecepatan fisik minimum ({minPossibleTime:F2}s)!");
                return new RaceValidationResult
                {
                    IsValid = false,
                    ErrorReason = "Integritas balapan tidak valid: Terdeteksi anomali kecepatan (Speedhack suspect).",
                    ElapsedTime = elapsedTime,
                    VerifiedRank = 999
                };
            }

            // 2. Validasi Urutan Checkpoint (Anti-Teleport / Shortcut)
            if (checkpoints != null && checkpoints.Count > 0)
            {
                float lastTimestamp = startTime;
                for (int i = 0; i < checkpoints.Count; i++)
                {
                    var cp = checkpoints[i];
                    if (cp.timestamp <= lastTimestamp)
                    {
                        Debug.LogError($"[AntiCheat] Urutan waktu checkpoint anomali pada CP #{cp.checkpointIndex}!");
                        return new RaceValidationResult
                        {
                            IsValid = false,
                            ErrorReason = "Integritas balapan tidak valid: Urutan lintasan tidak kronologis (Teleport suspect).",
                            ElapsedTime = elapsedTime,
                            VerifiedRank = 999
                        };
                    }
                    lastTimestamp = cp.timestamp;
                }
            }

            // 3. Validasi Konsistensi Tabrakan
            // Jika banyak tabrakan tapi waktu tetap super cepat, catat peringatan
            if (collisionCount > 3 && elapsedTime < minPossibleTime * 1.15f)
            {
                Debug.LogWarning("[AntiCheat] Tabrakan tinggi namun waktu mendekati sempurna.");
            }

            // Balapan valid dan terverifikasi
            string token = "VAL-" + Guid.NewGuid().ToString().Substring(0, 10).ToUpper();
            Debug.Log($"[AntiCheat] Validasi server sukses. Token: {token}, Waktu: {elapsedTime:F2}s, Juara: #{rawRank}");

            return new RaceValidationResult
            {
                IsValid = true,
                ValidationToken = token,
                ElapsedTime = elapsedTime,
                VerifiedRank = rawRank,
                ErrorReason = string.Empty
            };
        }
    }
}
