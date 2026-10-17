using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameBalap.Core;
using GameBalap.Economy;
using GameBalap.Auth;
using GameBalap.Gameplay.Player;
using GameBalap.Gameplay.AI;
using GameBalap.NetworkValidation;

namespace GameBalap.Gameplay.Race
{
    [System.Serializable]
    public class RaceResultData
    {
        public string raceId;
        public RaceTier tier;
        public int entryFee;
        public int finalRank;
        public bool isWinner;
        public float playerTime;
        public float opponentTime;
        public int payoutAmount;
        public int updatedBalance;
        public string validationToken;
        public string opponentName;
    }

    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private TrackGenerator trackGenerator;
        [SerializeField] private PlayerController player;
        [SerializeField] private AIRunnerController opponentAI;

        [Header("State")]
        [SerializeField] private RaceState currentState = RaceState.Idle;
        [SerializeField] private RaceTier currentTier = RaceTier.Bronze;
        [SerializeField] private string currentRaceId = "";
        [SerializeField] private int playerRank = 1;

        // Internal timing & telemetry
        private float raceStartTime = 0f;
        private float playerFinishTime = 0f;
        private float opponentFinishTime = 0f;
        private bool isPlayerFinished = false;
        private bool isOpponentFinished = false;
        private readonly List<CheckpointTelemetry> playerCheckpoints = new List<CheckpointTelemetry>();

        // Events
        public event Action<RaceState> OnRaceStateChanged;
        public event Action<int> OnCountdownTick; // 3, 2, 1, 0 (GO)
        public event Action<int, float, float> OnLiveTelemetryUpdated; // playerRank, progress01, currentSpeed
        public event Action<RaceResultData> OnRaceFinished;
        public event Action<string> OnMatchmakingStarted;

        public RaceState CurrentState => currentState;
        public RaceTier CurrentTier => currentTier;
        public int CurrentRank => playerRank;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureSceneReferences();
        }

        private void Start()
        {
            // Pastikan track di-generate saat start
            if (trackGenerator != null)
            {
                trackGenerator.GenerateTrack();
                SubscribeToTrackEvents();
            }
        }

        private void Update()
        {
            if (currentState == RaceState.Racing)
            {
                UpdateLiveRankAndTelemetry();
            }
        }

        private void EnsureSceneReferences()
        {
            if (trackGenerator == null) trackGenerator = UnityHelper.FindInstance<TrackGenerator>();
            if (player == null) player = UnityHelper.FindInstance<PlayerController>();
            if (opponentAI == null) opponentAI = UnityHelper.FindInstance<AIRunnerController>();

            // Jika objek belum ada di scene sama sekali (prosedural setup)
            if (trackGenerator == null)
            {
                GameObject tgObj = new GameObject("TrackGenerator");
                trackGenerator = tgObj.AddComponent<TrackGenerator>();
            }

            if (player == null)
            {
                GameObject playerObj = new GameObject("PlayerRunner");
                player = playerObj.AddComponent<PlayerController>();
                playerObj.tag = "Player";
            }

            if (opponentAI == null)
            {
                GameObject aiObj = new GameObject("AIRivalRunner");
                opponentAI = aiObj.AddComponent<AIRunnerController>();
                aiObj.tag = "Opponent";
            }
        }

        private void SubscribeToTrackEvents()
        {
            if (trackGenerator.CurrentFinishLine != null)
            {
                trackGenerator.CurrentFinishLine.OnFinished += HandleFinishLineCrossed;
            }

            foreach (var cp in trackGenerator.Checkpoints)
            {
                cp.OnParticipantCrossed += HandleCheckpointCrossed;
            }
        }

        private void HandleCheckpointCrossed(string participantId, int index, float time, float speedZ)
        {
            if (participantId == "PLAYER")
            {
                playerCheckpoints.Add(new CheckpointTelemetry
                {
                    checkpointIndex = index,
                    timestamp = time,
                    speedAtCheckpoint = speedZ
                });
            }
        }

        private void HandleFinishLineCrossed(string participantId, float finishTime)
        {
            if (participantId == "PLAYER" && !isPlayerFinished)
            {
                isPlayerFinished = true;
                playerFinishTime = finishTime;
                player.StopRacing();
                Debug.Log($"[RaceManager] Player finish di waktu: {playerFinishTime - raceStartTime:F2}s");
            }
            else if (participantId == "AI_OPPONENT" && !isOpponentFinished)
            {
                isOpponentFinished = true;
                opponentFinishTime = finishTime;
                opponentAI.StopRacing();
                Debug.Log($"[RaceManager] Lawan finish di waktu: {opponentFinishTime - raceStartTime:F2}s");
            }

            // Jika player sudah finish, selesaikan sesi balapan
            if (isPlayerFinished)
            {
                StartCoroutine(CompleteRaceRoutine());
            }
        }

        private void UpdateLiveRankAndTelemetry()
        {
            if (player == null || opponentAI == null || trackGenerator == null) return;

            // Hitung posisi ranking real-time berdasarkan jarak tempuh
            playerRank = player.DistanceTravelled >= opponentAI.DistanceTravelled ? 1 : 2;

            float progress = Mathf.Clamp01(player.DistanceTravelled / trackGenerator.TotalTrackDistance);
            OnLiveTelemetryUpdated?.Invoke(playerRank, progress, player.CurrentSpeed);
        }

        /// <summary>
        /// Memulai alur balapan dengan tier tertentu (memotong entry fee & matchmaking).
        /// </summary>
        public bool StartRaceWithTier(RaceTier tier, out string errorMessage)
        {
            currentTier = tier;
            currentRaceId = "RACE-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // 1. Validasi & potong koin entry fee
            if (!WalletManager.Instance.TryDeductEntryFee(tier, currentRaceId, out errorMessage))
            {
                return false;
            }

            // 2. Mulai alur matchmaking dan persiapan balapan
            StartCoroutine(MatchmakingRoutine());
            return true;
        }

        private IEnumerator MatchmakingRoutine()
        {
            currentState = RaceState.Matchmaking;
            OnRaceStateChanged?.Invoke(currentState);

            string[] dummyOpponents = { "ThunderVolt", "CyberSprint", "ShadowRunner", "ApexDash", "NitroSonic" };
            string chosenOpponent = dummyOpponents[UnityEngine.Random.Range(0, dummyOpponents.Length)];
            float botSkill = currentTier == RaceTier.Bronze ? 0.82f : (currentTier == RaceTier.Silver ? 0.90f : 0.96f);

            OnMatchmakingStarted?.Invoke(chosenOpponent);

            // Simulasi pencarian lawan 1.5 detik
            yield return new WaitForSeconds(1.5f);

            // Setup Track & Posisi Start
            trackGenerator.GenerateTrack();
            SubscribeToTrackEvents();
            trackGenerator.CurrentFinishLine.ResetTrigger();

            playerCheckpoints.Clear();
            isPlayerFinished = false;
            isOpponentFinished = false;
            playerFinishTime = 0f;
            opponentFinishTime = 0f;

            // Reset posisi pelari di garis start
            Vector3 playerStart = new Vector3(0, 0, 2f);
            Vector3 aiStart = new Vector3(GameConstants.LANE_WIDTH, 0, 2f);
            player.ResetPosition(playerStart);
            opponentAI.ResetPosition(aiStart, Lane.Right, chosenOpponent, botSkill);

            // Mulai Countdown
            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            currentState = RaceState.Countdown;
            OnRaceStateChanged?.Invoke(currentState);

            for (int i = 3; i > 0; i--)
            {
                OnCountdownTick?.Invoke(i);
                yield return new WaitForSeconds(1.0f);
            }

            OnCountdownTick?.Invoke(0); // "GO!"
            yield return new WaitForSeconds(0.4f);

            // Mulai Balapan
            currentState = RaceState.Racing;
            raceStartTime = Time.time;
            OnRaceStateChanged?.Invoke(currentState);

            player.StartRacing();
            opponentAI.StartRacing();
        }

        private IEnumerator CompleteRaceRoutine()
        {
            currentState = RaceState.Finished;
            OnRaceStateChanged?.Invoke(currentState);

            yield return new WaitForSeconds(0.8f);

            // Tentukan apakah pemain menang (Juara 1)
            int finalRank = isOpponentFinished && opponentFinishTime < playerFinishTime ? 2 : 1;
            bool isWinner = (finalRank == 1);

            // 1. Validasi server-side / anti-cheat logic
            var validation = RaceValidationService.Instance.ValidateRace(
                currentRaceId,
                currentTier,
                raceStartTime,
                playerFinishTime,
                trackGenerator.TotalTrackDistance,
                player.CollisionCount,
                playerCheckpoints,
                finalRank
            );

            int payout = 0;
            if (validation.IsValid && isWinner)
            {
                // Kreditkan koin kemenangan
                payout = WalletManager.Instance.CreditWinnings(currentTier, currentRaceId, 1);
            }
            else if (!validation.IsValid)
            {
                Debug.LogError($"[RaceManager] Balapan ditolak oleh validasi server: {validation.ErrorReason}");
            }

            // Catat statistik di profil pemain
            AccountManager.Instance.RecordRaceFinished(isWinner);

            RaceResultData result = new RaceResultData
            {
                raceId = currentRaceId,
                tier = currentTier,
                entryFee = (int)currentTier,
                finalRank = finalRank,
                isWinner = isWinner,
                playerTime = playerFinishTime - raceStartTime,
                opponentTime = isOpponentFinished ? opponentFinishTime - raceStartTime : (playerFinishTime - raceStartTime + 1.8f),
                payoutAmount = payout,
                updatedBalance = WalletManager.Instance.CurrentBalance,
                validationToken = validation.ValidationToken,
                opponentName = opponentAI.BotName
            };

            currentState = RaceState.ResultDisplayed;
            OnRaceStateChanged?.Invoke(currentState);
            OnRaceFinished?.Invoke(result);
        }

        public void ResetToLobby()
        {
            StopAllCoroutines();
            currentState = RaceState.Idle;
            OnRaceStateChanged?.Invoke(currentState);

            Vector3 playerStart = new Vector3(0, 0, 2f);
            Vector3 aiStart = new Vector3(GameConstants.LANE_WIDTH, 0, 2f);

            if (player != null) player.ResetPosition(playerStart);
            if (opponentAI != null) opponentAI.ResetPosition(aiStart, Lane.Right, "Bot_Rival", 0.85f);
        }
    }
}
