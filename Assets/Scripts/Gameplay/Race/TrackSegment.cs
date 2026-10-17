using System.Collections.Generic;
using UnityEngine;
using GameBalap.Core;

namespace GameBalap.Gameplay.Race
{
    public class TrackSegment : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform groundPlane;
        [SerializeField] private Transform obstacleContainer;

        private readonly List<Obstacle> spawnedObstacles = new List<Obstacle>();

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

            GenerateProceduralObstacles(segmentIndex);
        }

        private void GenerateProceduralObstacles(int segmentIndex)
        {
            // Tentukan pola obstacle per segmen
            int obstacleCount = Random.Range(1, 3);
            for (int i = 0; i < obstacleCount; i++)
            {
                float zOffset = 8f + (i * 12f);
                Lane lane = (Lane)Random.Range(-1, 2);
                ObstacleType type = (ObstacleType)Random.Range(0, 3);

                SpawnObstacle(type, lane, zOffset);
            }
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
            Material darkStripeMat = new Material(Shader.Find("Standard")) { color = new Color(0.1f, 0.1f, 0.12f) };

            // 1. Kaki Tiang Kiri & Kanan di Tanah
            CreateSubPart(parent, "FootLeft", new Vector3(-0.85f, 0.05f, 0), new Vector3(0.2f, 0.1f, 0.6f), metalMat);
            CreateSubPart(parent, "FootRight", new Vector3(0.85f, 0.05f, 0), new Vector3(0.2f, 0.1f, 0.6f), metalMat);

            // 2. Tiang Vertikal Kiri & Kanan
            CreateSubPart(parent, "PoleLeft", new Vector3(-0.85f, 0.45f, 0), new Vector3(0.12f, 0.8f, 0.12f), metalMat);
            CreateSubPart(parent, "PoleRight", new Vector3(0.85f, 0.45f, 0), new Vector3(0.12f, 0.8f, 0.12f), metalMat);

            // 3. Palang Utama Rintangan
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
            Material yellowLightMat = new Material(Shader.Find("Standard")) { color = new Color(1.0f, 0.9f, 0.1f) };

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
            Material darkHazardMat = new Material(Shader.Find("Standard")) { color = new Color(0.12f, 0.12f, 0.14f) };
            Material amberBeaconMat = new Material(Shader.Find("Standard")) { color = new Color(1.0f, 0.55f, 0.05f) };

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
        }
    }
}
