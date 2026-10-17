using System;
using UnityEngine;
using GameBalap.Gameplay.Player;
using GameBalap.Gameplay.AI;

namespace GameBalap.Gameplay.Race
{
    public class FinishLine : MonoBehaviour
    {
        public event Action<string, float> OnFinished; // participantId, finishTime

        private bool hasPlayerFinished = false;
        private bool hasAIFinished = false;

        public void ResetTrigger()
        {
            hasPlayerFinished = false;
            hasAIFinished = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            float finishTime = Time.time;

            var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if ((player != null || other.CompareTag("Player")) && !hasPlayerFinished)
            {
                hasPlayerFinished = true;
                Debug.Log($"<color=#00FFAA><b>[FinishLine] Pemain melintasi garis finish pada waktu: {finishTime:F2}s!</b></color>");
                OnFinished?.Invoke("PLAYER", finishTime);
                return;
            }

            var ai = other.GetComponent<AIRunnerController>() ?? other.GetComponentInParent<AIRunnerController>();
            if ((ai != null || other.CompareTag("Opponent")) && !hasAIFinished)
            {
                hasAIFinished = true;
                Debug.Log($"<color=#FFAA00><b>[FinishLine] Lawan bot melintasi garis finish pada waktu: {finishTime:F2}s!</b></color>");
                OnFinished?.Invoke("AI_OPPONENT", finishTime);
            }
        }
    }
}
