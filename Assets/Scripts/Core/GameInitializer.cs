using UnityEngine;
using GameBalap.Economy;
using GameBalap.Auth;
using GameBalap.NetworkValidation;
using GameBalap.Gameplay.Player;
using GameBalap.Gameplay.AI;
using GameBalap.Gameplay.Race;
using GameBalap.Social;
using GameBalap.UI;

namespace GameBalap.Core
{
    /// <summary>
    /// Bootstrap otomatis yang menginisialisasi seluruh sistem, scene, kamera, pencahayaan,
    /// track, pemain, AI bot, dan UI jika scene dijalankan dalam keadaan kosong di Unity Editor.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameInitializer : MonoBehaviour
    {
        [Header("Auto Setup Options")]
        [SerializeField] private bool autoSetupLightAndCamera = true;

        private void Awake()
        {
            Debug.Log("<color=#00FFAA><b>[GameInitializer] Menginisialisasi Game Balap Lari 3D Prototipe...</b></color>");

            // 1. Inisialisasi Service Otoritatif & Ekonomi
            EnsureComponent<WalletManager>("WalletManager");
            EnsureComponent<AccountManager>("AccountManager");
            EnsureComponent<RaceValidationService>("RaceValidationService");
            EnsureComponent<LeaderboardManager>("LeaderboardManager");

            // 2. Inisialisasi Pencahayaan jika belum ada
            if (autoSetupLightAndCamera)
            {
                SetupLightingAndCamera();
            }

            // 3. Inisialisasi Gameplay & Track
            var trackGen = EnsureComponent<TrackGenerator>("TrackGenerator");
            var player = EnsureComponent<PlayerController>("PlayerRunner");
            player.gameObject.tag = "Player";
            var ai = EnsureComponent<AIRunnerController>("AIRivalRunner");
            ai.gameObject.tag = "Opponent";
            var raceManager = EnsureComponent<RaceManager>("RaceManager");

            // 4. Inisialisasi UI
            EnsureComponent<PrototypeUIController>("PrototypeUIController");
        }

        private T EnsureComponent<T>(string gameObjectName) where T : Component
        {
            T existing = UnityHelper.FindInstance<T>();
            if (existing != null) return existing;

            GameObject obj = new GameObject(gameObjectName);
            return obj.AddComponent<T>();
        }

        private void SetupLightingAndCamera()
        {
            // Cek Directional Light
            Light dirLight = UnityHelper.FindInstance<Light>();
            if (dirLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.intensity = 1.2f;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // Cek Main Camera & Pasang SmoothCameraFollow
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            if (!cam.TryGetComponent<SmoothCameraFollow>(out _))
            {
                cam.gameObject.AddComponent<SmoothCameraFollow>();
            }
        }
    }
}
