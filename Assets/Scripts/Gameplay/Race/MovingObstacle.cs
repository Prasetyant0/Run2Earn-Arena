using UnityEngine;
using GameBalap.Core;
using GameBalap.Gameplay.Player;

namespace GameBalap.Gameplay.Race
{
    /// <summary>
    /// Rintangan dinamis yang bergerak bolak-balik melintasi jalur (horizontal ping-pong / patrol).
    /// Memaksa pemain membaca ritme pergerakan dan memilih timing perpindahan jalur yang tepat.
    /// </summary>
    public class MovingObstacle : MonoBehaviour
    {
        [Header("Movement Config")]
        [SerializeField] private float moveSpeed = 1.8f;
        [SerializeField] private float amplitudeX = 2.4f; // Jarak ayunan mencakup jalur Kiri sampai Kanan
        [SerializeField] private float phaseOffset = 0f;

        private float baseZ;
        private Obstacle obstacleComponent;

        public void Initialize(float localZ, float speed = 1.8f, float phase = 0f)
        {
            this.baseZ = localZ;
            this.moveSpeed = speed;
            this.phaseOffset = phase;

            transform.localPosition = new Vector3(0, 0, localZ);

            obstacleComponent = gameObject.AddComponent<Obstacle>();
            obstacleComponent.Initialize(ObstacleType.BarrierFull, Lane.Middle);

            BuildVisual();

            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.8f, 2.0f, 0.8f);
            col.center = new Vector3(0, 1.0f, 0);
        }

        private void Update()
        {
            // Pergerakan horizontal sinusoidal bolak-balik melintasi jalur
            float currentX = Mathf.Sin(Time.time * moveSpeed + phaseOffset) * amplitudeX;
            transform.localPosition = new Vector3(currentX, 0, baseZ);
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.OnHitObstacle(obstacleComponent);
            }
        }

        private void BuildVisual()
        {
            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(transform, false);

            // Material Laser / Cyber Hazard
            Color hazardColor = new Color(1.0f, 0.15f, 0.35f);
            Material dangerMat = new Material(Shader.Find("Standard"))
            {
                name = "MovingObstacleHazardMat",
                color = hazardColor
            };
            dangerMat.EnableKeyword("_EMISSION");
            dangerMat.SetColor("_EmissionColor", hazardColor * 2.8f);

            Material darkChassisMat = new Material(Shader.Find("Standard"))
            {
                name = "ChassisMat",
                color = new Color(0.18f, 0.18f, 0.22f)
            };

            // 1. Hover Chassis Utama
            CreateSubPart(visualRoot, "Chassis", new Vector3(0, 0.5f, 0), new Vector3(1.8f, 0.4f, 0.8f), darkChassisMat);

            // 2. Pilar Plasma Kiri & Kanan
            CreateSubPart(visualRoot, "PillarLeft", new Vector3(-0.75f, 1.1f, 0), new Vector3(0.2f, 1.2f, 0.2f), darkChassisMat);
            CreateSubPart(visualRoot, "PillarRight", new Vector3(0.75f, 1.1f, 0), new Vector3(0.2f, 1.2f, 0.2f), darkChassisMat);

            // 3. Medan Listrik / Laser Emissive Tengah
            CreateSubPart(visualRoot, "PlasmaField", new Vector3(0, 1.1f, 0), new Vector3(1.3f, 0.9f, 0.15f), dangerMat);

            // 4. Lampu Peringatan Strobo Merah di Atas
            CreateSubPart(visualRoot, "WarningSiren", new Vector3(0, 1.8f, 0), new Vector3(0.35f, 0.35f, 0.35f), dangerMat);
        }

        private void CreateSubPart(GameObject parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;

            var col = part.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            part.GetComponent<MeshRenderer>().material = mat;
        }
    }
}
