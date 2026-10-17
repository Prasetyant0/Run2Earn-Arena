using UnityEngine;
using GameBalap.Core;
using GameBalap.Economy;
using GameBalap.Gameplay.Player;

namespace GameBalap.Gameplay.PowerUps
{
    /// <summary>
    /// Koin virtual yang tersebar di sepanjang lintasan balap.
    /// Memberikan tambahan +10 saldo koin virtual kepada pemain saat diambil.
    /// Memiliki interaksi magnetik jika Coin Magnet aktif pada pemain.
    /// </summary>
    public class TrackCoin : MonoBehaviour
    {
        [Header("Coin Config")]
        [SerializeField] private int coinValue = 10;
        [SerializeField] private float rotationSpeed = 160f;
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobFrequency = 4f;

        private float baseY;
        private bool isCollected = false;
        private Transform magnetTarget = null;
        private float magnetPullSpeed = 18f;

        public int Value => coinValue;

        public void Initialize(Lane lane, float localZ, float height = 0.8f)
        {
            transform.localPosition = new Vector3((int)lane * GameConstants.LANE_WIDTH, height, localZ);
            baseY = transform.localPosition.y;

            BuildVisual();

            SphereCollider col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.65f;
        }

        private void Update()
        {
            if (isCollected) return;

            // Jika sedang ditarik magnet ke arah pemain
            if (magnetTarget != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, magnetTarget.position + Vector3.up * 0.9f, magnetPullSpeed * Time.deltaTime);
                magnetPullSpeed += 30f * Time.deltaTime; // Akselerasi tarikan magnet

                if (Vector3.Distance(transform.position, magnetTarget.position + Vector3.up * 0.9f) < 0.8f)
                {
                    Collect(magnetTarget.gameObject);
                }
                return;
            }

            // Putaran koin emas
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);

            // Efek melayang naik-turun (bobbing)
            float newY = baseY + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
        }

        public void AttractTo(Transform target)
        {
            if (isCollected) return;
            magnetTarget = target;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (player != null || other.CompareTag("Player"))
            {
                Collect(other.gameObject);
            }
        }

        private void Collect(GameObject collector)
        {
            if (isCollected) return;
            isCollected = true;

            var powerUpManager = collector.GetComponent<PlayerPowerUpManager>() ?? collector.GetComponentInParent<PlayerPowerUpManager>();
            if (powerUpManager != null)
            {
                powerUpManager.CollectCoin(coinValue, transform.position);
            }
            else
            {
                WalletManager.Instance?.AddCoins(coinValue);
            }

            PlaySparkleEffect();
            Destroy(gameObject);
        }

        private void BuildVisual()
        {
            GameObject coinModel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coinModel.name = "CoinMesh";
            coinModel.transform.SetParent(transform, false);
            coinModel.transform.localScale = new Vector3(0.6f, 0.08f, 0.6f);
            coinModel.transform.localRotation = Quaternion.Euler(90f, 0, 0); // Tegak menghadap lintasan

            var col = coinModel.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            // Material Emas Berkilau dengan Emissive HDR
            Color goldColor = new Color(1.0f, 0.82f, 0.05f);
            Material mat = new Material(Shader.Find("Standard"))
            {
                name = "TrackCoinGoldMat",
                color = goldColor
            };
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", goldColor * 2.2f);
            coinModel.GetComponent<MeshRenderer>().material = mat;

            // Cincin dalam untuk detail koin 3D
            GameObject innerCore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerCore.name = "CoinCore";
            innerCore.transform.SetParent(coinModel.transform, false);
            innerCore.transform.localScale = new Vector3(0.65f, 1.05f, 0.65f);
            var innerCol = innerCore.GetComponent<Collider>();
            if (innerCol != null) DestroyImmediate(innerCol);

            Material innerMat = new Material(Shader.Find("Standard"))
            {
                name = "TrackCoinCoreMat",
                color = new Color(0.95f, 0.65f, 0.02f)
            };
            innerMat.EnableKeyword("_EMISSION");
            innerMat.SetColor("_EmissionColor", new Color(0.95f, 0.65f, 0.02f) * 1.5f);
            innerCore.GetComponent<MeshRenderer>().material = innerMat;
        }

        private void PlaySparkleEffect()
        {
            GameObject sparkle = new GameObject("CoinSparkle");
            sparkle.transform.position = transform.position;

            var ps = sparkle.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 3.5f;
            main.startSize = 0.15f;
            main.loop = false;
            main.startColor = new Color(1.0f, 0.85f, 0.15f);

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 16) });

            ps.Play();
            Destroy(sparkle, 0.6f);
        }
    }
}
