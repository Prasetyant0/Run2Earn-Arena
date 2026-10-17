using System;
using UnityEngine;
using GameBalap.Core;
using GameBalap.Economy;
using GameBalap.Gameplay.Player;
using GameBalap.Customization;
using GameBalap.Audio;

namespace GameBalap.Gameplay.PowerUps
{
    /// <summary>
    /// Pengelola status dan durasi Power-Up aktif pada pemain (Speed Boost, Shield, Coin Magnet).
    /// Mengontrol visual perisai energi, deteksi tarikan magnet koin, dan penyerapan tabrakan.
    /// </summary>
    public class PlayerPowerUpManager : MonoBehaviour
    {
        [Header("Power-Up Durations")]
        [SerializeField] private float speedBoostMultiplier = 1.35f;
        [SerializeField] private float baseMagnetRadius = 10f;

        // Timers & States
        private float speedBoostTimer = 0f;
        private float speedBoostTotalDuration = 0f;
        private bool hasShield = false;
        private float magnetTimer = 0f;
        private float magnetTotalDuration = 0f;

        // Telemetry
        public int CoinsCollectedThisRace { get; private set; } = 0;

        // Visual Shield Instance
        private GameObject shieldVisualInstance;

        // Properties
        public bool HasShield => hasShield;
        public bool IsSpeedBoosting => speedBoostTimer > 0f;
        public bool IsMagnetActive => magnetTimer > 0f;
        public float SpeedBoostMultiplier => IsSpeedBoosting ? speedBoostMultiplier : 1.0f;
        public float SpeedBoostRemaining => speedBoostTimer;
        public float SpeedBoostRatio => speedBoostTotalDuration > 0 ? Mathf.Clamp01(speedBoostTimer / speedBoostTotalDuration) : 0f;
        public float MagnetRemaining => magnetTimer;
        public float MagnetRatio => magnetTotalDuration > 0 ? Mathf.Clamp01(magnetTimer / magnetTotalDuration) : 0f;

        // Events
        public event Action<PowerUpType, float> OnPowerUpActivated;
        public event Action<PowerUpType> OnPowerUpExpired;
        public event Action OnShieldBroken;
        public event Action<int, Vector3> OnCoinCollected; // Amount & World Position

        private void Update()
        {
            // Update Speed Boost Timer
            if (speedBoostTimer > 0f)
            {
                speedBoostTimer -= Time.deltaTime;
                if (speedBoostTimer <= 0f)
                {
                    speedBoostTimer = 0f;
                    OnPowerUpExpired?.Invoke(PowerUpType.SpeedBoost);
                }
            }

            // Update Magnet Timer & Attract Coins
            if (magnetTimer > 0f)
            {
                magnetTimer -= Time.deltaTime;
                AttractNearbyCoins();

                if (magnetTimer <= 0f)
                {
                    magnetTimer = 0f;
                    OnPowerUpExpired?.Invoke(PowerUpType.CoinMagnet);
                }
            }

            // Animate Shield Visual if active
            if (shieldVisualInstance != null)
            {
                shieldVisualInstance.transform.Rotate(30f * Time.deltaTime, 60f * Time.deltaTime, 0f, Space.World);
            }
        }

        public void CollectPowerUp(PowerUpType type, float duration)
        {
            Debug.Log($"<color=#00FFAA>[PowerUpManager] Mengambil PowerUp: {type} ({duration}s)</color>");

            switch (type)
            {
                case PowerUpType.SpeedBoost:
                    speedBoostTimer = duration;
                    speedBoostTotalDuration = duration;
                    break;

                case PowerUpType.Shield:
                    hasShield = true;
                    CreateShieldVisual();
                    break;

                case PowerUpType.CoinMagnet:
                    magnetTimer = duration;
                    magnetTotalDuration = duration;
                    break;
            }

            AudioManager.Instance?.PlayPowerUp();
            OnPowerUpActivated?.Invoke(type, duration);
        }

        public void CollectCoin(int amount, Vector3 worldPosition)
        {
            CoinsCollectedThisRace += amount;
            WalletManager.Instance?.AddCoins(amount);
            AudioManager.Instance?.PlayCoin();
            OnCoinCollected?.Invoke(amount, worldPosition);
        }

        /// <summary>
        /// Dipanggil oleh PlayerController saat menabrak rintangan.
        /// Jika memiliki Shield atau sedang SpeedBoost, tabrakan diserap dan pemain tidak terjatuh.
        /// </summary>
        public bool TryAbsorbCollision()
        {
            // Jika sedang dalam efek Speed Boost, kebal tabrakan sepenuhnya
            if (IsSpeedBoosting)
            {
                Debug.Log("[PowerUpManager] Rintangan dihancurkan karena efek SpeedBoost!");
                return true;
            }

            // Jika memiliki Shield, perisai pecah dan menyerap tabrakan
            if (hasShield)
            {
                hasShield = false;
                DestroyShieldVisual();
                PlayShieldBreakEffect();
                AudioManager.Instance?.PlayShieldBreak();
                OnShieldBroken?.Invoke();
                Debug.Log("[PowerUpManager] Perisai menyerap tabrakan dan pecah!");
                return true;
            }

            return false;
        }

        private void AttractNearbyCoins()
        {
            // Ambil bonus radius magnet dari peningkatan toko jika ada
            float magnetMult = ShopManager.Instance != null ? ShopManager.Instance.GetStatMultiplier(StatUpgradeType.MagnetRadius) : 1.0f;
            float radius = baseMagnetRadius * magnetMult;
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (var hit in hits)
            {
                var coin = hit.GetComponent<TrackCoin>() ?? hit.GetComponentInParent<TrackCoin>();
                if (coin != null)
                {
                    coin.AttractTo(transform);
                }
            }
        }

        private void CreateShieldVisual()
        {
            if (shieldVisualInstance != null) return;

            shieldVisualInstance = new GameObject("ShieldEnergyDome");
            shieldVisualInstance.transform.SetParent(transform, false);
            shieldVisualInstance.transform.localPosition = new Vector3(0, 1.0f, 0);

            // Bola Energi Transparan
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "EnergySphere";
            sphere.transform.SetParent(shieldVisualInstance.transform, false);
            sphere.transform.localScale = new Vector3(1.7f, 2.1f, 1.7f);

            var col = sphere.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            Color shieldColor = new Color(0.1f, 1.0f, 0.6f, 0.45f);
            Material mat = new Material(Shader.Find("Standard"))
            {
                name = "ActiveShieldMat",
                color = shieldColor
            };
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.1f, 1.0f, 0.6f) * 2.5f);
            sphere.GetComponent<MeshRenderer>().material = mat;

            // Cincin Orbit Berputar
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "OrbitRing";
            ring.transform.SetParent(shieldVisualInstance.transform, false);
            ring.transform.localScale = new Vector3(2.1f, 0.04f, 2.1f);
            ring.transform.localRotation = Quaternion.Euler(45f, 0, 0);

            var ringCol = ring.GetComponent<Collider>();
            if (ringCol != null) DestroyImmediate(ringCol);

            ring.GetComponent<MeshRenderer>().material = mat;
        }

        private void DestroyShieldVisual()
        {
            if (shieldVisualInstance != null)
            {
                Destroy(shieldVisualInstance);
                shieldVisualInstance = null;
            }
        }

        private void PlayShieldBreakEffect()
        {
            GameObject fx = new GameObject("ShieldBreakFX");
            fx.transform.position = transform.position + Vector3.up * 1.0f;

            var ps = fx.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 6f;
            main.startSize = 0.25f;
            main.loop = false;
            main.startColor = new Color(0.2f, 1.0f, 0.7f);

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 30) });

            ps.Play();
            Destroy(fx, 1.0f);
        }

        public void ResetAll()
        {
            speedBoostTimer = 0f;
            magnetTimer = 0f;
            hasShield = false;
            CoinsCollectedThisRace = 0;
            DestroyShieldVisual();
        }
    }
}
