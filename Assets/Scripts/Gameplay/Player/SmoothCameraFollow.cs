using UnityEngine;
using GameBalap.Core;

namespace GameBalap.Gameplay.Player
{
    public class SmoothCameraFollow : MonoBehaviour
    {
        public static SmoothCameraFollow Instance { get; private set; }

        [Header("Target & Offsets")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 3.6f, -5.8f);
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private float lookAtHeight = 1.6f;

        [Header("Screen Shake")]
        private float shakeTimer = 0f;
        private float shakeDuration = 0.3f;
        private float shakeIntensity = 0.3f;

        private void Awake()
        {
            Instance = this;
        }

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

            // Efek Camera Shake saat tabrakan
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                float currentIntensity = Mathf.Lerp(0f, shakeIntensity, shakeTimer / shakeDuration);
                Vector3 shakeOffset = Random.insideUnitSphere * currentIntensity;
                shakeOffset.z = 0; // Jaga jarak kedalaman stabil
                desiredPosition += shakeOffset;
            }

            transform.position = desiredPosition;

            // Selalu pandang sedikit di atas karakter untuk estetika optimal
            Vector3 lookTarget = target.position + Vector3.up * lookAtHeight;
            transform.LookAt(lookTarget);
        }

        public void TriggerShake(float intensity = 0.32f, float duration = 0.28f)
        {
            this.shakeIntensity = intensity;
            this.shakeDuration = duration;
            this.shakeTimer = duration;
        }

        private void FindTargetIfNull()
        {
            var player = UnityHelper.FindInstance<PlayerController>();
            if (player != null)
            {
                target = player.transform;
                player.OnStumbled += () => TriggerShake(0.38f, 0.32f);
            }
        }
    }
}
