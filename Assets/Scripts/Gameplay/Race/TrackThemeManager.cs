using UnityEngine;
using GameBalap.Core;

namespace GameBalap.Gameplay.Race
{
    /// <summary>
    /// Mengelola tema lingkungan visual (Cyberpunk, Desert, Volcanic), 
    /// mengatur pencahayaan, kabut (fog), serta ornamen dekorasi di sepanjang sisi lintasan.
    /// </summary>
    public class TrackThemeManager : MonoBehaviour
    {
        public static TrackThemeManager Instance { get; private set; }

        [SerializeField] private EnvironmentTheme currentTheme = EnvironmentTheme.CyberpunkNeon;
        private TrackThemeConfig activeConfig;

        public EnvironmentTheme CurrentTheme => currentTheme;
        public TrackThemeConfig ActiveConfig => activeConfig ?? (activeConfig = TrackThemeConfig.GetPreset(currentTheme));

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ApplyTheme(currentTheme);
        }

        public void ApplyTheme(EnvironmentTheme theme)
        {
            currentTheme = theme;
            activeConfig = TrackThemeConfig.GetPreset(theme);

            // 1. Atur Pencahayaan Matahari & Suasana
            Light dirLight = UnityHelper.FindInstance<Light>();
            if (dirLight != null)
            {
                dirLight.color = activeConfig.sunLightColor;
                dirLight.intensity = activeConfig.sunIntensity;
            }

            // 2. Atur Kabut Atmosfer (Unity Fog)
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = activeConfig.skyFogColor;
            RenderSettings.fogDensity = activeConfig.fogDensity;

            // 3. Atur Background Kamera
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = activeConfig.skyFogColor;
            }

            Debug.Log($"<color=#00FFAA><b>[TrackThemeManager] Tema Lingkungan Diterapkan: {activeConfig.themeName}</b></color>");
        }

        /// <summary>
        /// Membuat dekorasi lingkungan di sisi kiri dan kanan segmen lintasan sesuai tema aktif.
        /// </summary>
        public void SpawnThematicScenery(Transform parent, float segmentLength)
        {
            float sideDistance = 12f;
            float zOffset = segmentLength * 0.5f;

            switch (currentTheme)
            {
                case EnvironmentTheme.CyberpunkNeon:
                    SpawnCyberBuilding(parent, new Vector3(-sideDistance - 4f, 12f, zOffset), new Vector3(8f, 26f, 10f), activeConfig.leftNeonColor);
                    SpawnCyberBuilding(parent, new Vector3(sideDistance + 4f, 15f, zOffset), new Vector3(9f, 32f, 11f), activeConfig.rightNeonColor);
                    break;

                case EnvironmentTheme.DesertDune:
                    SpawnDesertMonument(parent, new Vector3(-sideDistance, 4f, zOffset), new Vector3(4f, 9f, 4f));
                    SpawnDesertMonument(parent, new Vector3(sideDistance, 5f, zOffset), new Vector3(5f, 11f, 5f));
                    break;

                case EnvironmentTheme.VolcanicMagma:
                    SpawnVolcanicSpire(parent, new Vector3(-sideDistance, 6f, zOffset), new Vector3(5f, 13f, 5f));
                    SpawnVolcanicSpire(parent, new Vector3(sideDistance, 7f, zOffset), new Vector3(6f, 15f, 6f));
                    break;
            }
        }

        private void SpawnCyberBuilding(Transform parent, Vector3 localPos, Vector3 scale, Color neonColor)
        {
            // Menara Gedung Cyber
            GameObject bldg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bldg.name = "CyberBuilding";
            bldg.transform.SetParent(parent, false);
            bldg.transform.localPosition = localPos;
            bldg.transform.localScale = scale;
            Destroy(bldg.GetComponent<Collider>());

            Material bldgMat = activeConfig.CreateMat("BldgMat", new Color(0.08f, 0.09f, 0.12f));
            bldg.GetComponent<MeshRenderer>().material = bldgMat;

            // Strip Jendela Neon / Hologram Reklame
            GameObject neonStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neonStrip.name = "HologramStrip";
            neonStrip.transform.SetParent(bldg.transform, false);
            neonStrip.transform.localPosition = new Vector3(localPos.x > 0 ? -0.51f : 0.51f, 0, 0);
            neonStrip.transform.localScale = new Vector3(0.08f, 0.75f, 0.75f);
            Destroy(neonStrip.GetComponent<Collider>());

            Material neonMat = activeConfig.CreateEmissiveMat("HoloMat", neonColor, 3.0f);
            neonStrip.GetComponent<MeshRenderer>().material = neonMat;
        }

        private void SpawnDesertMonument(Transform parent, Vector3 localPos, Vector3 scale)
        {
            GameObject obelisk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obelisk.name = "DesertObelisk";
            obelisk.transform.SetParent(parent, false);
            obelisk.transform.localPosition = localPos;
            obelisk.transform.localScale = scale;
            Destroy(obelisk.GetComponent<Collider>());

            Material mat = activeConfig.CreateMat("DesertStone", new Color(0.68f, 0.48f, 0.32f));
            obelisk.GetComponent<MeshRenderer>().material = mat;

            // Glyphs Emas Emisif
            GameObject glyph = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glyph.name = "SunGlyph";
            glyph.transform.SetParent(obelisk.transform, false);
            glyph.transform.localPosition = new Vector3(0, 0.2f, 0);
            glyph.transform.localScale = new Vector3(1.05f, 0.15f, 1.05f);
            Destroy(glyph.GetComponent<Collider>());

            Material glyphMat = activeConfig.CreateEmissiveMat("GlyphMat", activeConfig.leftNeonColor, 2.5f);
            glyph.GetComponent<MeshRenderer>().material = glyphMat;
        }

        private void SpawnVolcanicSpire(Transform parent, Vector3 localPos, Vector3 scale)
        {
            GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spire.name = "BasaltSpire";
            spire.transform.SetParent(parent, false);
            spire.transform.localPosition = localPos;
            spire.transform.localScale = scale;
            spire.transform.localRotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 45f), Random.Range(-5f, 5f));
            Destroy(spire.GetComponent<Collider>());

            Material rockMat = activeConfig.CreateMat("BasaltMat", new Color(0.12f, 0.08f, 0.08f));
            spire.GetComponent<MeshRenderer>().material = rockMat;

            // Celah Magma Menyala di Batu
            GameObject lavaVein = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lavaVein.name = "MagmaVein";
            lavaVein.transform.SetParent(spire.transform, false);
            lavaVein.transform.localPosition = new Vector3(0, -0.1f, 0);
            lavaVein.transform.localScale = new Vector3(1.05f, 0.2f, 1.05f);
            Destroy(lavaVein.GetComponent<Collider>());

            Material lavaMat = activeConfig.CreateEmissiveMat("MagmaVeinMat", activeConfig.leftNeonColor, 3.2f);
            lavaVein.GetComponent<MeshRenderer>().material = lavaMat;
        }
    }
}
