using System;
using UnityEngine;
using GameBalap.Gameplay.Player;
using GameBalap.Gameplay.AI;

namespace GameBalap.Gameplay.Race
{
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private int checkpointIndex;
        public int CheckpointIndex => checkpointIndex;

        public event Action<string, int, float, float> OnParticipantCrossed; // (id, index, time, speedZ)

        public void Initialize(int index)
        {
            checkpointIndex = index;
        }

        private void OnTriggerEnter(Collider other)
        {
            float time = Time.time;
            var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (player != null || other.CompareTag("Player"))
            {
                float speed = player != null ? player.CurrentSpeed : 0f;
                OnParticipantCrossed?.Invoke("PLAYER", checkpointIndex, time, speed);
                return;
            }

            var ai = other.GetComponent<AIRunnerController>() ?? other.GetComponentInParent<AIRunnerController>();
            if (ai != null || other.CompareTag("Opponent"))
            {
                OnParticipantCrossed?.Invoke("AI_OPPONENT", checkpointIndex, time, 0f);
            }
        }
    }
}
