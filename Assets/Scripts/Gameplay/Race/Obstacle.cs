using UnityEngine;
using GameBalap.Core;
using GameBalap.Gameplay.Player;
using GameBalap.Gameplay.AI;

namespace GameBalap.Gameplay.Race
{
    public class Obstacle : MonoBehaviour
    {
        [Header("Obstacle Configuration")]
        [SerializeField] private ObstacleType type = ObstacleType.BarrierFull;
        [SerializeField] private Lane targetLane = Lane.Middle;

        private bool hasTriggeredPlayer = false;
        private bool hasTriggeredAI = false;

        public ObstacleType Type => type;
        public Lane TargetLane => targetLane;

        public void Initialize(ObstacleType obstacleType, Lane lane)
        {
            this.type = obstacleType;
            this.targetLane = lane;
            hasTriggeredPlayer = false;
            hasTriggeredAI = false;
            transform.localPosition = new Vector3((int)lane * GameConstants.LANE_WIDTH, 0, transform.localPosition.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (player != null && !hasTriggeredPlayer)
            {
                HandlePlayerCollision(player);
                return;
            }

            var ai = other.GetComponent<AIRunnerController>() ?? other.GetComponentInParent<AIRunnerController>();
            if (ai != null && !hasTriggeredAI)
            {
                HandleAICollision(ai);
            }
        }

        private void HandlePlayerCollision(PlayerController player)
        {
            if (player.CurrentState == CharacterMovementState.Finished) return;

            bool avoided = false;

            switch (type)
            {
                case ObstacleType.HurdleLow:
                    // Rintangan bawah: harus dilompati (sedang jumping atau posisi karakter cukup tinggi)
                    if (player.CurrentState == CharacterMovementState.Jumping || player.transform.position.y > 0.6f)
                    {
                        avoided = true;
                    }
                    break;

                case ObstacleType.HighBeam:
                    // Rintangan atas: harus dislide (tunduk)
                    if (player.CurrentState == CharacterMovementState.Sliding)
                    {
                        avoided = true;
                    }
                    break;

                case ObstacleType.BarrierFull:
                    // Rintangan penuh: harus dihindari dengan berpindah ke jalur lain
                    avoided = false;
                    break;
            }

            if (!avoided)
            {
                hasTriggeredPlayer = true;
                player.OnHitObstacle(this);
            }
        }

        private void HandleAICollision(AIRunnerController ai)
        {
            if (ai.CurrentState == CharacterMovementState.Finished) return;

            bool avoided = false;

            switch (type)
            {
                case ObstacleType.HurdleLow:
                    if (ai.CurrentState == CharacterMovementState.Jumping || ai.transform.position.y > 0.6f) avoided = true;
                    break;
                case ObstacleType.HighBeam:
                    if (ai.CurrentState == CharacterMovementState.Sliding) avoided = true;
                    break;
                case ObstacleType.BarrierFull:
                    avoided = false;
                    break;
            }

            if (!avoided)
            {
                hasTriggeredAI = true;
                ai.OnHitObstacle();
            }
        }
    }
}
