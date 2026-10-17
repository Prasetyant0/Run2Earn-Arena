using UnityEngine;
using GameBalap.Core;
using GameBalap.Gameplay.Player;

namespace GameBalap.Gameplay.PowerUps
{
    public class PowerUpItem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PowerUpType powerUpType = PowerUpType.SpeedBoost;
        [SerializeField] private float duration = 5f;
        [SerializeField] private float rotationSpeed = 110f;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobFrequency = 3.5f;

        private float baseY;
        private bool isCollected = false;

        public PowerUpType Type => powerUpType;
        public float Duration => duration;

        public void Initialize(PowerUpType type, Lane lane, float localZ)
        {
            this.powerUpType = type;
            transform.localPosition = new Vector3((int)lane * GameConstants.LANE_WIDTH, 1.1f, localZ);
            baseY = transform.localPosition.y;

            BuildVisual(type);

            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.2f, 1.4f, 1.2f);
        }

        private void Update()
        {
            if (isCollected) return;

            // Efek berputar
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);

            // Efek mengambang naik-turun (bobbing)
            float newY = baseY + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            var powerUpManager = other.GetComponent<PlayerPowerUpManager>() ?? other.GetComponentInParent<PlayerPowerUpManager>();
            if (powerUpManager != null)
            {
                isCollected = true;
                powerUpManager.CollectPowerUp(powerUpType, duration);
                PlayCollectEffect();
                Destroy(gameObject);
            }
        }

        private void BuildVisual(PowerUpType type)
        {
            GameObject visual = new GameObject("VisualRoot");
            visual.transform.SetParent(transform, false);

            switch (type)
            {
                case PowerUpType.SpeedBoost:
                    BuildSpeedBoostVisual(visual);
                    break;
                case PowerUpType.Shield:
                    BuildShieldVisual(visual);
                    break;
                case PowerUpType.CoinMagnet:
                    BuildMagnetVisual(visual);
                    break;
            }
        }

        private void BuildSpeedBoostVisual(GameObject parent)
        {
            Color neonCyan = new Color(0.0f, 0.95f, 1.0f);
            Material mat = CreateEmissiveMat("BoostMat", neonCyan, 3.2f);

            // Panah Ganda Kilat (Chevrons)
            CreateSubBox(parent, "Chevron1", new Vector3(0, 0.2f, 0), new Vector3(0.55f, 0.35f, 0.2f), mat);
            CreateSubBox(parent, "Chevron2", new Vector3(0, -0.2f, 0), new Vector3(0.55f, 0.35f, 0.2f), mat);
        }

        private void BuildShieldVisual(GameObject parent)
        {
            Color shieldGreen = new Color(0.1f, 1.0f, 0.5f);
            Material mat = CreateEmissiveMat("ShieldMat", shieldGreen, 2.8f);

            // Cincin Perisai Heksagonal / Bola Perlindungan
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "ShieldOrb";
            sphere.transform.SetParent(parent.transform, false);
            sphere.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            StripCollider(sphere);
            sphere.GetComponent<MeshRenderer>().material = mat;

            // Cincin Luar
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "OrbitalRing";
            ring.transform.SetParent(parent.transform, false);
            ring.transform.localScale = new Vector3(0.9f, 0.04f, 0.9f);
            ring.transform.localRotation = Quaternion.Euler(30f, 0, 0);
            StripCollider(ring);
            ring.GetComponent<MeshRenderer>().material = mat;
        }

        private void BuildMagnetVisual(GameObject parent)
        {
            Material redMat = CreateEmissiveMat("MagnetRedMat", new Color(1.0f, 0.15f, 0.2f), 2.5f);
            Material blueMat = CreateEmissiveMat("MagnetBlueMat", new Color(0.2f, 0.4f, 1.0f), 2.5f);
            Material grayMat = new Material(Shader.Find("Standard")) { color = new Color(0.3f, 0.3f, 0.35f) };

            // Batang Tapal Kuda Magnet
            CreateSubBox(parent, "BaseCurve", new Vector3(0, -0.2f, 0), new Vector3(0.6f, 0.18f, 0.18f), grayMat);
            CreateSubBox(parent, "PoleNorth", new Vector3(-0.25f, 0.1f, 0), new Vector3(0.18f, 0.45f, 0.18f), redMat);
            CreateSubBox(parent, "PoleSouth", new Vector3(0.25f, 0.1f, 0), new Vector3(0.18f, 0.45f, 0.18f), blueMat);
        }

        private void CreateSubBox(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent.transform, false);
            box.transform.localPosition = pos;
            box.transform.localScale = scale;
            StripCollider(box);
            box.GetComponent<MeshRenderer>().material = mat;
        }

        private Material CreateEmissiveMat(string name, Color col, float emission)
        {
            Material mat = new Material(Shader.Find("Standard"))
            {
                name = name,
                color = col
            };
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", col * emission);
            return mat;
        }

        private void StripCollider(GameObject obj)
        {
            var col = obj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }

        private void PlayCollectEffect()
        {
            // Partikel burst pengambilan power-up
            GameObject burstObj = new GameObject("PowerUpBurst");
            burstObj.transform.position = transform.position;
            var ps = burstObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.4f;
            main.startSpeed = 5f;
            main.startSize = 0.25f;
            main.loop = false;
            main.startColor = powerUpType == PowerUpType.SpeedBoost ? Color.cyan : (powerUpType == PowerUpType.Shield ? Color.green : Color.magenta);

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 25) });

            ps.Play();
            Destroy(burstObj, 0.8f);
        }
    }
}
