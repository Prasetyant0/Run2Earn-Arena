using System.Collections.Generic;
using UnityEngine;
using GameBalap.Core;

namespace GameBalap.Gameplay.Race
{
    public class TrackGenerator : MonoBehaviour
    {
        [Header("Track Configuration")]
        [SerializeField] private int totalSegments = GameConstants.DEFAULT_TRACK_SEGMENTS;
        [SerializeField] private float segmentLength = GameConstants.SEGMENT_LENGTH;
        [SerializeField] private Material trackMaterial;
        [SerializeField] private Material laneLineMaterial;

        [Header("Spawned References")]
        [SerializeField] private FinishLine spawnedFinishLine;
        [SerializeField] private List<Checkpoint> spawnedCheckpoints = new List<Checkpoint>();

        private readonly List<GameObject> activeSegments = new List<GameObject>();
        private GameObject finishStadiumArea;

        public float TotalTrackDistance => totalSegments * segmentLength;
        public FinishLine CurrentFinishLine => spawnedFinishLine;
        public IReadOnlyList<Checkpoint> Checkpoints => spawnedCheckpoints;

        public void GenerateTrack()
        {
            ClearTrack();

            int checkpointCounter = 0;

            // Generate Track Segments
            for (int i = 0; i < totalSegments; i++)
            {
                bool isStart = (i == 0);
                bool isFinish = (i == totalSegments - 1);
                float zPos = i * segmentLength;

                GameObject segmentObj = new GameObject($"TrackSegment_{i}");
                segmentObj.transform.SetParent(transform, false);
                segmentObj.transform.position = new Vector3(0, 0, zPos);

                // Buat bidang jalan aspal & pembatas neon
                CreateSegmentGround(segmentObj, segmentLength, isStart);

                // Tambahkan gantry distance marker
                if (i > 0 && i % 4 == 0 && !isFinish)
                {
                    CreateDistanceArch(segmentObj, $"{i * (int)segmentLength}M", segmentLength * 0.5f);
                }

                // Tambahkan script TrackSegment
                TrackSegment segment = segmentObj.AddComponent<TrackSegment>();
                segment.InitializeSegment(i, isFinish, isStart);

                // Tambahkan Checkpoint setiap 3 segmen
                if (i > 0 && i % 3 == 0 && !isFinish)
                {
                    Checkpoint cp = CreateCheckpoint(segmentObj, checkpointCounter++, segmentLength * 0.5f);
                    spawnedCheckpoints.Add(cp);
                }

                // Pada segmen terakhir, tempatkan Garis Finish
                if (isFinish)
                {
                    float finishZ = segmentLength * 0.35f;
                    spawnedFinishLine = CreateFinishLine(segmentObj, finishZ);
                }

                activeSegments.Add(segmentObj);
            }

            // Buat Area Selebrasi Finish / Grandstand yang Luas & Tertutup
            // Sehingga pemain dan bot memiliki landasan aman untuk deselerasi dan TIDAK BISA JATUH
            CreateFinishCelebrationStadium(totalSegments * segmentLength);

            Debug.Log($"[TrackGenerator] Track balap 3D berhasil di-generate. Panjang lintasan: {TotalTrackDistance}m ({totalSegments} segmen + Stadium Finish).");
        }

        private void CreateSegmentGround(GameObject parent, float length, bool isStart)
        {
            float totalWidth = GameConstants.LANE_WIDTH * 3f + 1.8f;

            // 1. Aspal Jalan Utama
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "RoadGround";
            road.transform.SetParent(parent.transform, false);
            road.transform.localPosition = new Vector3(0, -0.25f, length * 0.5f);
            road.transform.localScale = new Vector3(totalWidth, 0.5f, length);

            var rend = road.GetComponent<MeshRenderer>();
            Material mat = trackMaterial != null ? trackMaterial : new Material(Shader.Find("Standard"));
            if (trackMaterial == null)
            {
                mat.color = new Color(0.14f, 0.15f, 0.18f); // Aspal gelap sporty
            }
            rend.material = mat;

            // 2. Garis Pembatas Jalur Putih/Kuning
            CreateLaneDivider(parent, -GameConstants.LANE_WIDTH * 0.5f, length);
            CreateLaneDivider(parent, GameConstants.LANE_WIDTH * 0.5f, length);

            // 3. Neon Railings Samping (Kiri: Cyan, Kanan: Magenta)
            CreateNeonRail(parent, -totalWidth * 0.5f, length, new Color(0f, 0.95f, 1f));
            CreateNeonRail(parent, totalWidth * 0.5f, length, new Color(1f, 0.1f, 0.6f));

            // Jika segmen awal, buat Gerbang Start
            if (isStart)
            {
                CreateStartGate(parent);
            }
        }

        private void CreateStartGate(GameObject parent)
        {
            GameObject startGate = new GameObject("StartGateArch");
            startGate.transform.SetParent(parent.transform, false);
            startGate.transform.localPosition = new Vector3(0, 0, 4f);

            float w = GameConstants.LANE_WIDTH * 3.6f;

            // Tiang Kiri & Kanan
            CreateGatePillar(startGate, new Vector3(-w * 0.5f, 2.5f, 0), new Vector3(0.6f, 5.2f, 0.6f), new Color(0.2f, 0.7f, 1f));
            CreateGatePillar(startGate, new Vector3(w * 0.5f, 2.5f, 0), new Vector3(0.6f, 5.2f, 0.6f), new Color(0.2f, 0.7f, 1f));

            // Palang Atas START
            GameObject crossBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossBar.name = "StartCrossbar";
            crossBar.transform.SetParent(startGate.transform, false);
            crossBar.transform.localPosition = new Vector3(0, 4.6f, 0);
            crossBar.transform.localScale = new Vector3(w + 0.8f, 1.0f, 0.7f);
            Destroy(crossBar.GetComponent<Collider>());

            Material barMat = new Material(Shader.Find("Standard"));
            barMat.color = new Color(0.1f, 0.5f, 0.95f);
            crossBar.GetComponent<MeshRenderer>().material = barMat;

            // Lampu Sinyal Start (Merah, Kuning, Hijau)
            CreateSignalLight(crossBar, new Vector3(-1.2f, 0, 0.4f), Color.red);
            CreateSignalLight(crossBar, new Vector3(0, 0, 0.4f), Color.yellow);
            CreateSignalLight(crossBar, new Vector3(1.2f, 0, 0.4f), Color.green);
        }

        private void CreateSignalLight(GameObject parent, Vector3 localPos, Color col)
        {
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.transform.SetParent(parent.transform, false);
            bulb.transform.localPosition = localPos;
            bulb.transform.localScale = new Vector3(0.35f, 0.35f, 0.2f);
            Destroy(bulb.GetComponent<Collider>());

            Material m = new Material(Shader.Find("Standard"));
            m.color = col;
            bulb.GetComponent<MeshRenderer>().material = m;
        }

        private void CreateDistanceArch(GameObject parent, string labelText, float localZ)
        {
            GameObject arch = new GameObject($"GantryArch_{labelText}");
            arch.transform.SetParent(parent.transform, false);
            arch.transform.localPosition = new Vector3(0, 0, localZ);

            float w = GameConstants.LANE_WIDTH * 3.4f;
            CreateGatePillar(arch, new Vector3(-w * 0.5f, 2.2f, 0), new Vector3(0.4f, 4.5f, 0.4f), new Color(0.3f, 0.35f, 0.4f));
            CreateGatePillar(arch, new Vector3(w * 0.5f, 2.2f, 0), new Vector3(0.4f, 4.5f, 0.4f), new Color(0.3f, 0.35f, 0.4f));

            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.transform.SetParent(arch.transform, false);
            banner.transform.localPosition = new Vector3(0, 4.0f, 0);
            banner.transform.localScale = new Vector3(w, 0.6f, 0.3f);
            Destroy(banner.GetComponent<Collider>());

            Material m = new Material(Shader.Find("Standard"));
            m.color = new Color(0.9f, 0.7f, 0.1f); // Kuning neon
            banner.GetComponent<MeshRenderer>().material = m;
        }

        private void CreateLaneDivider(GameObject parent, float xPos, float length)
        {
            // Buat garis strip putus-putus
            int stripeCount = (int)(length / 4f);
            for (int s = 0; s < stripeCount; s++)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "LaneStripe";
                line.transform.SetParent(parent.transform, false);
                line.transform.localPosition = new Vector3(xPos, 0.02f, s * 4f + 2f);
                line.transform.localScale = new Vector3(0.12f, 0.04f, 2.2f);

                var col = line.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var rend = line.GetComponent<MeshRenderer>();
                Material mat = laneLineMaterial != null ? laneLineMaterial : new Material(Shader.Find("Standard"));
                if (laneLineMaterial == null) mat.color = new Color(0.95f, 0.95f, 0.95f);
                rend.material = mat;
            }
        }

        private void CreateNeonRail(GameObject parent, float xPos, float length, Color neonColor)
        {
            // Rail Beton Bawah
            GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curb.name = "SideCurb";
            curb.transform.SetParent(parent.transform, false);
            curb.transform.localPosition = new Vector3(xPos, 0.2f, length * 0.5f);
            curb.transform.localScale = new Vector3(0.45f, 0.5f, length);
            Material curbMat = new Material(Shader.Find("Standard")) { color = new Color(0.2f, 0.22f, 0.26f) };
            curb.GetComponent<MeshRenderer>().material = curbMat;

            // Strip Neon Menyala di Atas Curb
            GameObject neon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neon.name = "NeonStrip";
            neon.transform.SetParent(curb.transform, false);
            neon.transform.localPosition = new Vector3(0, 0.52f, 0);
            neon.transform.localScale = new Vector3(0.8f, 0.25f, 1.0f);
            Destroy(neon.GetComponent<Collider>());

            Material neonMat = new Material(Shader.Find("Standard")) { color = neonColor };
            neon.GetComponent<MeshRenderer>().material = neonMat;
        }

        private Checkpoint CreateCheckpoint(GameObject parent, int index, float localZ)
        {
            GameObject cpObj = new GameObject($"Checkpoint_{index}");
            cpObj.transform.SetParent(parent.transform, false);
            cpObj.transform.localPosition = new Vector3(0, 1.5f, localZ);

            BoxCollider col = cpObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(GameConstants.LANE_WIDTH * 3.5f, 5f, 2f);

            Checkpoint cp = cpObj.AddComponent<Checkpoint>();
            cp.Initialize(index);
            return cp;
        }

        private FinishLine CreateFinishLine(GameObject parent, float localZ)
        {
            GameObject finishObj = new GameObject("FinishLineTrigger");
            finishObj.transform.SetParent(parent.transform, false);
            finishObj.transform.localPosition = new Vector3(0, 1.5f, localZ);

            BoxCollider col = finishObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(GameConstants.LANE_WIDTH * 4f, 6f, 3f);

            // Gerbang visual Finish Line megah
            CreateGrandFinishArch(finishObj);

            return finishObj.AddComponent<FinishLine>();
        }

        private void CreateGrandFinishArch(GameObject parent)
        {
            float w = GameConstants.LANE_WIDTH * 3.8f;

            // Menara Kiri
            CreateGatePillar(parent, new Vector3(-w * 0.5f, 3.2f, 0), new Vector3(0.8f, 6.5f, 0.8f), new Color(0.95f, 0.75f, 0.1f));
            // Menara Kanan
            CreateGatePillar(parent, new Vector3(w * 0.5f, 3.2f, 0), new Vector3(0.8f, 6.5f, 0.8f), new Color(0.95f, 0.75f, 0.1f));

            // Banner Utama "FINISH"
            GameObject arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arch.name = "FinishCrossbar";
            arch.transform.SetParent(parent.transform, false);
            arch.transform.localPosition = new Vector3(0, 5.2f, 0);
            arch.transform.localScale = new Vector3(w + 1.2f, 1.4f, 0.8f);
            Destroy(arch.GetComponent<Collider>());

            Material goldMat = new Material(Shader.Find("Standard")) { color = new Color(1.0f, 0.85f, 0.1f) };
            arch.GetComponent<MeshRenderer>().material = goldMat;

            // Garis Catur Hitam-Putih di Bawah Garis Finish
            GameObject checkeredFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            checkeredFloor.name = "CheckeredFinishStrip";
            checkeredFloor.transform.SetParent(parent.transform, false);
            checkeredFloor.transform.localPosition = new Vector3(0, -1.45f, 0);
            checkeredFloor.transform.localScale = new Vector3(GameConstants.LANE_WIDTH * 3f + 1f, 0.05f, 2.5f);
            Destroy(checkeredFloor.GetComponent<Collider>());

            Material checkMat = new Material(Shader.Find("Standard")) { color = Color.white };
            checkeredFloor.GetComponent<MeshRenderer>().material = checkMat;
        }

        private void CreateGatePillar(GameObject parent, Vector3 localPos, Vector3 scale, Color col)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.transform.SetParent(parent.transform, false);
            pillar.transform.localPosition = localPos;
            pillar.transform.localScale = scale;
            Destroy(pillar.GetComponent<Collider>());

            Material m = new Material(Shader.Find("Standard")) { color = col };
            pillar.GetComponent<MeshRenderer>().material = m;
        }

        /// <summary>
        /// Area stadium finish yang luas (45 meter) setelah garis finish 
        /// dengan dinding penahan kokoh agar pemain tidak jatuh ke ruang hampa.
        /// </summary>
        private void CreateFinishCelebrationStadium(float startZ)
        {
            float stadiumLength = 48f;
            float totalWidth = GameConstants.LANE_WIDTH * 3f + 4f;

            finishStadiumArea = new GameObject("FinishCelebrationStadium");
            finishStadiumArea.transform.SetParent(transform, false);
            finishStadiumArea.transform.position = new Vector3(0, 0, startZ);

            // 1. Lantai Stadium Luas
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "StadiumFloor";
            floor.transform.SetParent(finishStadiumArea.transform, false);
            floor.transform.localPosition = new Vector3(0, -0.25f, stadiumLength * 0.5f);
            floor.transform.localScale = new Vector3(totalWidth, 0.5f, stadiumLength);

            Material floorMat = new Material(Shader.Find("Standard")) { color = new Color(0.2f, 0.22f, 0.28f) };
            floor.GetComponent<MeshRenderer>().material = floorMat;

            // 2. Karpet Merah Kemenangan di Tengah
            GameObject redCarpet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            redCarpet.name = "VictoryRedCarpet";
            redCarpet.transform.SetParent(finishStadiumArea.transform, false);
            redCarpet.transform.localPosition = new Vector3(0, 0.02f, stadiumLength * 0.5f);
            redCarpet.transform.localScale = new Vector3(GameConstants.LANE_WIDTH * 1.5f, 0.04f, stadiumLength);
            Destroy(redCarpet.GetComponent<Collider>());

            Material redMat = new Material(Shader.Find("Standard")) { color = new Color(0.85f, 0.15f, 0.2f) };
            redCarpet.GetComponent<MeshRenderer>().material = redMat;

            // 3. Tembok Kiri & Kanan Stadium
            CreateStadiumWall(finishStadiumArea, new Vector3(-totalWidth * 0.5f, 2f, stadiumLength * 0.5f), new Vector3(0.6f, 4f, stadiumLength), Color.cyan);
            CreateStadiumWall(finishStadiumArea, new Vector3(totalWidth * 0.5f, 2f, stadiumLength * 0.5f), new Vector3(0.6f, 4f, stadiumLength), Color.magenta);

            // 4. TEMBOK PENAHAN AKHIR (Pemain TIDAK AKAN PERNAH JATUH!)
            GameObject endWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            endWall.name = "EndSolidWall";
            endWall.transform.SetParent(finishStadiumArea.transform, false);
            endWall.transform.localPosition = new Vector3(0, 2.5f, stadiumLength);
            endWall.transform.localScale = new Vector3(totalWidth + 1f, 5.5f, 1.2f);

            Material endWallMat = new Material(Shader.Find("Standard")) { color = new Color(0.95f, 0.8f, 0.1f) };
            endWall.GetComponent<MeshRenderer>().material = endWallMat;

            // Banner Selebrasi Kemenangan di Dinding Akhir
            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "VictoryBanner";
            banner.transform.SetParent(endWall.transform, false);
            banner.transform.localPosition = new Vector3(0, 0.2f, -0.65f);
            banner.transform.localScale = new Vector3(0.85f, 0.5f, 0.1f);
            Destroy(banner.GetComponent<Collider>());

            Material bannerMat = new Material(Shader.Find("Standard")) { color = new Color(0.1f, 0.6f, 1.0f) };
            banner.GetComponent<MeshRenderer>().material = bannerMat;
        }

        private void CreateStadiumWall(GameObject parent, Vector3 pos, Vector3 scale, Color accentCol)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "StadiumSideWall";
            wall.transform.SetParent(parent.transform, false);
            wall.transform.localPosition = pos;
            wall.transform.localScale = scale;

            Material mat = new Material(Shader.Find("Standard")) { color = accentCol * 0.7f };
            wall.GetComponent<MeshRenderer>().material = mat;
        }

        public void ClearTrack()
        {
            foreach (var seg in activeSegments)
            {
                if (seg != null) Destroy(seg);
            }
            activeSegments.Clear();
            spawnedCheckpoints.Clear();
            spawnedFinishLine = null;

            if (finishStadiumArea != null)
            {
                Destroy(finishStadiumArea);
                finishStadiumArea = null;
            }
        }
    }
}
