using UnityEngine;
using GameBalap.Core;

namespace GameBalap.Gameplay.Race
{
    [System.Serializable]
    public class TrackThemeConfig
    {
        public EnvironmentTheme theme;
        public string themeName;

        [Header("Colors")]
        public Color roadColor;
        public Color curbColor;
        public Color laneMarkColor;
        public Color leftNeonColor;
        public Color rightNeonColor;
        public Color obstacleAccent;

        [Header("Lighting & Atmosphere")]
        public Color skyFogColor;
        public Color sunLightColor;
        public float sunIntensity;
        public float fogDensity;

        public static TrackThemeConfig GetPreset(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.DesertDune:
                    return new TrackThemeConfig
                    {
                        theme = EnvironmentTheme.DesertDune,
                        themeName = "Ancient Dune Valley",
                        roadColor = new Color(0.58f, 0.42f, 0.28f),        // Sandstone gurun hangat
                        curbColor = new Color(0.38f, 0.26f, 0.16f),
                        laneMarkColor = new Color(1.0f, 0.95f, 0.8f),
                        leftNeonColor = new Color(1.0f, 0.72f, 0.05f),      // Solar Gold neon
                        rightNeonColor = new Color(0.08f, 0.95f, 0.75f),    // Turquoise Oasis neon
                        obstacleAccent = new Color(0.95f, 0.55f, 0.1f),
                        skyFogColor = new Color(0.85f, 0.65f, 0.45f),       // Haze kabut matahari gurun
                        sunLightColor = new Color(1.0f, 0.9f, 0.75f),
                        sunIntensity = 1.35f,
                        fogDensity = 0.007f
                    };

                case EnvironmentTheme.VolcanicMagma:
                    return new TrackThemeConfig
                    {
                        theme = EnvironmentTheme.VolcanicMagma,
                        themeName = "Magma Caldera",
                        roadColor = new Color(0.12f, 0.10f, 0.11f),        // Batuan obsidian abu-abu gelap
                        curbColor = new Color(0.18f, 0.08f, 0.06f),
                        laneMarkColor = new Color(1.0f, 0.6f, 0.1f),
                        leftNeonColor = new Color(1.0f, 0.22f, 0.0f),       // Magma Fire Red
                        rightNeonColor = new Color(1.0f, 0.85f, 0.0f),      // Superheated Lava Yellow
                        obstacleAccent = new Color(1.0f, 0.35f, 0.0f),
                        skyFogColor = new Color(0.16f, 0.08f, 0.08f),       // Asap kabut vulkanik kelam
                        sunLightColor = new Color(1.0f, 0.55f, 0.35f),
                        sunIntensity = 1.15f,
                        fogDensity = 0.009f
                    };

                case EnvironmentTheme.CyberpunkNeon:
                default:
                    return new TrackThemeConfig
                    {
                        theme = EnvironmentTheme.CyberpunkNeon,
                        themeName = "Neo-Tokyo Cyber City",
                        roadColor = new Color(0.11f, 0.12f, 0.15f),        // Aspal modern gelap
                        curbColor = new Color(0.18f, 0.20f, 0.25f),
                        laneMarkColor = new Color(0.95f, 0.95f, 1.0f),
                        leftNeonColor = new Color(0.0f, 0.95f, 1.0f),       // Electric Cyan
                        rightNeonColor = new Color(1.0f, 0.05f, 0.65f),     // Cyberpunk Magenta
                        obstacleAccent = new Color(0.0f, 0.85f, 1.0f),
                        skyFogColor = new Color(0.06f, 0.07f, 0.12f),       // Kabut malam neon sci-fi
                        sunLightColor = new Color(0.7f, 0.85f, 1.0f),
                        sunIntensity = 1.1f,
                        fogDensity = 0.006f
                    };
            }
        }

        public Material CreateEmissiveMat(string name, Color baseColor, float emissionMultiplier = 2.5f)
        {
            Material mat = new Material(Shader.Find("Standard"))
            {
                name = name,
                color = baseColor
            };
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseColor * emissionMultiplier);
            return mat;
        }

        public Material CreateMat(string name, Color baseColor)
        {
            return new Material(Shader.Find("Standard"))
            {
                name = name,
                color = baseColor
            };
        }
    }
}
