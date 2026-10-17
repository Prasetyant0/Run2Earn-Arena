using System.Collections.Generic;
using UnityEngine;
using GameBalap.Core;
using GameBalap.Gameplay.PowerUps;

namespace GameBalap.Gameplay.Race
{
    public class TrackSegment : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform groundPlane;
        [SerializeField] private Transform obstacleContainer;

        private readonly List<Obstacle> spawnedObstacles = new List<Obstacle>();
        private readonly List<MovingObstacle> spawnedMovingObstacles = new List<MovingObstacle>();
        private readonly List<TrackCoin> spawnedCoins = new List<TrackCoin>();
        private readonly List<PowerUpItem> spawnedPowerUps = new List<PowerUpItem>();

        public void InitializeSegment(int segmentIndex, bool isFinishSegment, bool isStartSegment)
        {
            if (obstacleContainer == null)
            {
                var obj = new GameObject("Obstacles");
                obj.transform.SetParent(transform, false);
                obstacleContainer = obj.transform;
            }

            // Segmen pertama (start) dan segmen terakhir (finish) aman dari obstacle
            if (isStartSegment || isFinishSegment)
            {
                return;
            }

            GenerateProceduralContent(segmentIndex);
        }

        private void GenerateProceduralContent(int segmentIndex)
        {
            // 1. Spawning Rintangan Statis
            int obstacleCount = Random.Range(1, 3);
            HashSet<int> occupiedLanes = new HashSet<int>();

            for (int i = 0; i < obstacleCount; i++)
            {
                float zOffset = 8f + (i * 12f);
                Lane lane = (Lane)Random.Range(-1, 2);
                occupiedLanes.Add((int)lane);

                // Hindari memblokir semua 3 jalur pada jarak z yang sama
                ObstacleType type = (ObstacleType)Random.Range(0, 3);
                SpawnObstacle(type, lane, zOffset);
            }

            // 2. Spawning Rintangan Bergerak Dinamis (Mulai dari segmen ke-2 dan seterusnya)
            if (segmentIndex >= 2 && Random.value < 0.45f)
            {
                SpawnMovingObstacle(16f, Random.Range(1.6f, 2.4f), Random.Range(0f, Mathf.PI));
            }

            // 3. Spawning Koin Lintasan (Track Coins) dalam formasi baris 3-5 koin
            SpawnCoinPattern(segmentIndex);

            // 4. Spawning Power-Up Item acak (SpeedBoost, Shield, CoinMagnet)
            if (Random.value < 0.5f)
            {
                Lane freeLane = (Lane)Random.Range(-1, 2);
                PowerUpType powerType = (PowerUpType)Random.Range(0, 3);
                SpawnPowerUp(powerType, freeLane, 14f);
            }
        }

        private void SpawnCoinPattern(int segmentIndex)
        {
            Lane coinLane = (Lane)Random.Range(-1, 2);
            int patternType = Random.Range(0, 2); // 0 = straight line, 1 = mini arc

            int coinCount = Random.Range(3, 6);
            float startZ = 6f;

            for (int i = 0; i < coinCount; i++)
            {
                float zPos = startZ + (i * 2.5f);
                float yPos = 0.8f;

                if (patternType == 1)
                {
                    // Lengkungan melompat parabola
                    float normalized = (float)i / (coinCount - 1);
                    yPos = 0.8f + Mathf.Sin(normalized * Mathf.PI) * 1.4f;
                }

                SpawnCoin(coinLane, zPos, yPos);
            }
        }

        public TrackCoin SpawnCoin(Lane lane, float localZ, float height = 0.8f)
        {
            GameObject coinObj = new GameObject($"TrackCoin_{lane}_{localZ:F0}");
            coinObj.transform.SetParent(obstacleContainer, false);

            TrackCoin coin = coinObj.AddComponent<TrackCoin>();
            coin.Initialize(lane, localZ, height);
            spawnedCoins.Add(coin);
            return coin;
        }

        public PowerUpItem SpawnPowerUp(PowerUpType type, Lane lane, float localZ)
        {
            GameObject powerObj = new GameObject($"PowerUp_{type}_{lane}");
            powerObj.transform.SetParent(obstacleContainer, false);

            PowerUpItem item = powerObj.AddComponent<PowerUpItem>();
            item.Initialize(type, lane, localZ);
            spawnedPowerUps.Add(item);
            return item;
        }

        public MovingObstacle SpawnMovingObstacle(float localZ, float speed, float phase)
        {
            GameObject movingObj = new GameObject($"MovingObstacle_{localZ:F0}");
            movingObj.transform.SetParent(obstacleContainer, false);

            MovingObstacle moving = movingObj.AddComponent<MovingObstacle>();
            moving.Initialize(localZ, speed, phase);
            spawnedMovingObstacles.Add(moving);
            return moving;
        }

        public Obstacle SpawnObstacle(ObstacleType type, Lane lane, float localZ)
        {
            GameObject obsObj = new GameObject($"Obstacle_{type}_{lane}");
            obsObj.transform.SetParent(obstacleContainer, false);
            obsObj.transform.localPosition = new Vector3((int)lane * GameConstants.LANE_WIDTH, 0, localZ);

            Obstacle obstacle = obsObj.AddComponent<Obstacle>();

            // Buat representasi 3D kaya detail visual (bukan sekedar kotak polos)
            CreateRichVisualForObstacle(obsObj, type);

            // Tambahkan Trigger Collider dengan ukuran tepat
            BoxCollider col = obsObj.AddComponent<BoxCollider>();
            col.isTrigger = true;

            switch (type)
            {
                case ObstacleType.HurdleLow:
                    col.size = new Vector3(1.9f, 0.9f, 0.5f);
                    col.center = new Vector3(0, 0.45f, 0);
                    break;

                case ObstacleType.HighBeam:
                    col.size = new Vector3(1.9f, 1.4f, 0.5f);
                    col.center = new Vector3(0, 1.7f, 0);
                    break;

                case ObstacleType.BarrierFull:
                    col.size = new Vector3(1.9f, 2.5f, 0.7f);
                    col.center = new Vector3(0, 1.25f, 0);
                    break;
            }

            obstacle.Initialize(type, lane);
            spawnedObstacles.Add(obstacle);
            return obstacle;
        }

        private void CreateRichVisualForObstacle(GameObject parent, ObstacleType type)
        {
            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(parent.transform, false);

            switch (type)
            {
                case ObstacleType.HurdleLow:
                    BuildHurdleLowVisual(visualRoot);
                    break;

                case ObstacleType.HighBeam:
                    BuildHighBeamVisual(visualRoot);
                    break;

                case ObstacleType.BarrierFull:
                    BuildBarrierFullVisual(visualRoot);
                    break;
            }
        }

        /// <summary>
        /// Rintangan Lompat Sporty: Tiang penyangga samping, palang neon hazard kuning-hitam, dan indikator panah atas.
        /// </summary>
        private void BuildHurdleLowVisual(GameObject parent)
        {
            Material metalMat = new Material(Shader.Find("Standard")) { color = new Color(0.25f, 0.28f, 0.32f) };
            Material neonHazardMat = new Material(Shader.Find("Standard")) { color = new Color(1.0f, 0.75f, 0.05f) };
            neonHazardMat.EnableKeyword("_EMISSION");
            neonHazardMat.SetColor("_EmissionColor", new Color(1.0f, 0.75f, 0.05f) * 2.4f);

            Material darkStripeMat = new Material(Shader.Find("Standard")) { color = new Color(0.1f, 0.1f, 0.12f) };

            // 1. Kaki Tiang Kiri & Kanan di Tanah
            CreateSubPart(parent, "FootLeft", new Vector3(-0.85f, 0.05f, 0), new Vector3(0.2f, 0.1f, 0.6f), metalMat);
            CreateSubPart(parent, "FootRight", new Vector3(0.85f, 0.05f, 0), new Vector3(0.2f, 0.1f, 0.6f), metalMat);

            // 2. Tiang Vertikal Kiri & Kanan
            CreateSubPart(parent, "PoleLeft", new Vector3(-0.85f, 0.45f, 0), new Vector3(0.12f, 0.8f, 0.12f), metalMat);
            CreateSubPart(parent, "PoleRight", new Vector3(0.85f, 0.45f, 0), new Vector3(0.12f, 0.8f, 0.12f), metalMat);

            // 3. Palang Utama Rintangan Emissive
            CreateSubPart(parent, "HurdleBar", new Vector3(0, 0.65f, 0), new Vector3(1.85f, 0.32f, 0.18f), neonHazardMat);

            // Strip Belang Hitam di Palang
            CreateSubPart(parent, "Stripe1", new Vector3(-0.45f, 0.65f, 0), new Vector3(0.25f, 0.34f, 0.20f), darkStripeMat);
            CreateSubPart(parent, "Stripe2", new Vector3(0.45f, 0.65f, 0), new Vector3(0.25f, 0.34f, 0.20f), darkStripeMat);

            // Indikator Panah Lompat
            CreateSubPart(parent, "JumpSign", new Vector3(0, 0.95f, 0), new Vector3(0.35f, 0.25f, 0.06f), neonHazardMat);
        }

        /// <summary>
        /// Rintangan Balok Atas (Gantry Beam): Pilar tinggi kokoh, balok baja gantung merah, dan lampu peringatan.
        /// </summary>
        private void BuildHighBeamVisual(GameObject parent)
        {
            Material pillarMat = new Material(Shader.Find("Standard")) { color = new Color(0.22f, 0.24f, 0.28f) };
            Material warnRedMat = new Material(Shader.Find("Standard")) { color = new Color(0.95f, 0.18f, 0.18f) };
            warnRedMat.EnableKeyword("_EMISSION");
            warnRedMat.SetColor("_EmissionColor", new Color(0.95f, 0.18f, 0.18f) * 2.5f);

            Material yellowLightMat = new Material(Shader.Find("Standard")) { color = new Color(1.0f, 0.9f, 0.1f) };
            yellowLightMat.EnableKeyword("_EMISSION");
            yellowLightMat.SetColor("_EmissionColor", new Color(1.0f, 0.9f, 0.1f) * 3.0f);

            // 1. Tiang Penyangga Tinggi Kiri & Kanan
            CreateSubPart(parent, "GantryLeft", new Vector3(-0.9f, 1.25f, 0), new Vector3(0.18f, 2.5f, 0.25f), pillarMat);
            CreateSubPart(parent, "GantryRight", new Vector3(0.9f, 1.25f, 0), new Vector3(0.18f, 2.5f, 0.25f), pillarMat);

            // 2. Balok Berat Melintang di Atas
            CreateSubPart(parent, "OverheadBeam", new Vector3(0, 1.95f, 0), new Vector3(1.95f, 0.55f, 0.32f), warnRedMat);

            // 3. Rambu Peringatan "SLIDE" di Tengah Bawah Balok
            CreateSubPart(parent, "SlideWarningBadge", new Vector3(0, 1.55f, 0), new Vector3(0.5f, 0.25f, 0.08f), yellowLightMat);

            // 4. Lampu Peringatan Strobo di Ujung Balok
            CreateSubPart(parent, "BeaconLeft", new Vector3(-0.85f, 2.3f, 0), new Vector3(0.15f, 0.2f, 0.15f), yellowLightMat);
            CreateSubPart(parent, "BeaconRight", new Vector3(0.85f, 2.3f, 0), new Vector3(0.15f, 0.2f, 0.15f), yellowLightMat);
        }

        /// <summary>
        /// Rintangan Penuh (Concrete Jersey Barricade): Beton tebal bertingkat dengan strip hazard kuning-hitam dan lampu suar.
        /// </summary>
        private void BuildBarrierFullVisual(GameObject parent)
        {
            Material concreteMat = new Material(Shader.Find("Standard")) { color = new Color(0.35f, 0.36f, 0.38f) };
            Material hazardYellowMat = new Material(Shader.Find("Standard")) { color = new Color(0.95f, 0.8f, 0.05f) };
            hazardYellowMat.EnableKeyword("_EMISSION");
            hazardYellowMat.SetColor("_EmissionColor", new Color(0.95f, 0.8f, 0.05f) * 1.6f);

            Material darkHazardMat = new Material(Shader.Find("Standard")) { color = new Color(0.12f, 0.12f, 0.14f) };
            Material amberBeaconMat = new Material(Shader.Find("Standard")) { color = new Color(1.0f, 0.55f, 0.05f) };
            amberBeaconMat.EnableKeyword("_EMISSION");
            amberBeaconMat.SetColor("_EmissionColor", new Color(1.0f, 0.55f, 0.05f) * 3.2f);

            // 1. Fondasi Beton Bawah Lebar
            CreateSubPart(parent, "ConcreteBase", new Vector3(0, 0.35f, 0), new Vector3(1.85f, 0.7f, 0.65f), concreteMat);

            // 2. Tembok Barikade Atas
            CreateSubPart(parent, "BarrierTop", new Vector3(0, 1.25f, 0), new Vector3(1.75f, 1.15f, 0.42f), hazardYellowMat);

            // 3. Strip Garis Belang Hazard Diagonal/Vertikal
            CreateSubPart(parent, "Stripe1", new Vector3(-0.45f, 1.25f, 0), new Vector3(0.3f, 1.18f, 0.45f), darkHazardMat);
            CreateSubPart(parent, "Stripe2", new Vector3(0.45f, 1.25f, 0), new Vector3(0.3f, 1.18f, 0.45f), darkHazardMat);

            // 4. Lampu Suar Amber di Puncak Barikade
            CreateSubPart(parent, "TopBeacon", new Vector3(0, 1.95f, 0), new Vector3(0.28f, 0.28f, 0.28f), amberBeaconMat);
        }

        private void CreateSubPart(GameObject parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;

            // Pastikan tidak ada collider pada bagian visual agar collider trigger utama yang bekerja
            var col = part.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            part.GetComponent<MeshRenderer>().material = mat;
        }

        public void Clear()
        {
            foreach (var obs in spawnedObstacles)
            {
                if (obs != null) Destroy(obs.gameObject);
            }
            spawnedObstacles.Clear();

            foreach (var moving in spawnedMovingObstacles)
            {
                if (moving != null) Destroy(moving.gameObject);
            }
            spawnedMovingObstacles.Clear();

            foreach (var coin in spawnedCoins)
            {
                if (coin != null) Destroy(coin.gameObject);
            }
            spawnedCoins.Clear();

            foreach (var power in spawnedPowerUps)
            {
                if (power != null) Destroy(power.gameObject);
            }
            spawnedPowerUps.Clear();
        }
    }
}
