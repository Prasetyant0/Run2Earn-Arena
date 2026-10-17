using UnityEngine;
using GameBalap.Core;

namespace GameBalap.Gameplay.Player
{
    public class SmoothCameraFollow : MonoBehaviour
    {
        [Header("Target & Offsets")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 3.6f, -5.8f);
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private float lookAtHeight = 1.6f;

        private void Start()
        {
            FindTargetIfNull();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindTargetIfNull();
                if (target == null) return;
            }

            // Ikuti posisi target dengan smoothing
            Vector3 desiredPosition = new Vector3(
                Mathf.Lerp(transform.position.x, target.position.x, Time.deltaTime * smoothSpeed * 0.85f),
                Mathf.Lerp(transform.position.y, target.position.y + offset.y, Time.deltaTime * smoothSpeed),
                target.position.z + offset.z
            );

            transform.position = desiredPosition;

            // Selalu pandang sedikit di atas karakter untuk estetika optimal
            Vector3 lookTarget = target.position + Vector3.up * lookAtHeight;
            transform.LookAt(lookTarget);
        }

        private void FindTargetIfNull()
        {
            var player = UnityHelper.FindInstance<PlayerController>();
            if (player != null) target = player.transform;
        }
    }
}
