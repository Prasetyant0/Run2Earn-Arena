using UnityEngine;
using GameBalap.Core;

namespace GameBalap.Gameplay.Player
{
    /// <summary>
    /// Mengelola efek partikel prosedural pada pelari:
    /// - Debu kaki saat lari di tanah (Run Dust)
    /// - Percikan gesekan saat berseluncur (Slide Sparks)
    /// - Garis angin kecepatan tinggi saat lari maksimal (Speed Lines)
    /// - Ledakan percikan saat menabrak rintangan (Hit Sparks)
    /// </summary>
    public class RunnerParticleEffects : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Color dustColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
        [SerializeField] private Color sparkColor = new Color(1.0f, 0.75f, 0.1f, 1.0f);

        private ParticleSystem runDustSystem;
        private ParticleSystem slideSparksSystem;
        private ParticleSystem speedLinesSystem;
        private ParticleSystem hitSparksSystem;

        private void Awake()
        {
            SetupRunDustSystem();
            SetupSlideSparksSystem();
            SetupSpeedLinesSystem();
            SetupHitSparksSystem();
        }

        private Material CreateParticleMat(bool additive)
        {
            Shader shader = null;
            if (additive)
            {
                shader = Shader.Find("Particles/Standard Unlit")
                      ?? Shader.Find("Legacy Shaders/Particles/Additive")
                      ?? Shader.Find("Mobile/Particles/Additive")
                      ?? Shader.Find("Sprites/Default");
            }
            else
            {
                shader = Shader.Find("Particles/Standard Unlit")
                      ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                      ?? Shader.Find("Mobile/Particles/Alpha Blended")
                      ?? Shader.Find("Sprites/Default");
            }

            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            return mat;
        }

        private void SetupRunDustSystem()
        {
            GameObject obj = new GameObject("RunDustParticles");
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(0, 0.05f, -0.2f);

            runDustSystem = obj.AddComponent<ParticleSystem>();
            var main = runDustSystem.main;
            main.startLifetime = 0.4f;
            main.startSpeed = 1.2f;
            main.startSize = 0.28f;
            main.startColor = dustColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = false;

            var emission = runDustSystem.emission;
            emission.rateOverTime = 18f;

            var shape = runDustSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;

            var vel = runDustSystem.velocityOverLifetime;
            vel.enabled = true;
            vel.z = new ParticleSystem.MinMaxCurve(-2.5f);
            vel.y = new ParticleSystem.MinMaxCurve(0.6f);

            var rend = obj.GetComponent<ParticleSystemRenderer>();
            rend.material = CreateParticleMat(false);
        }

        private void SetupSlideSparksSystem()
        {
            GameObject obj = new GameObject("SlideSparksParticles");
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(0, 0.05f, 0f);

            slideSparksSystem = obj.AddComponent<ParticleSystem>();
            var main = slideSparksSystem.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 4.5f;
            main.startSize = 0.12f;
            main.startColor = sparkColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = false;

            var emission = slideSparksSystem.emission;
            emission.rateOverTime = 40f;

            var shape = slideSparksSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.2f;
            shape.rotation = new Vector3(0, 180f, 0); // Semburkan ke belakang

            var rend = obj.GetComponent<ParticleSystemRenderer>();
            rend.material = CreateParticleMat(true);
        }

        private void SetupSpeedLinesSystem()
        {
            GameObject obj = new GameObject("SpeedLinesParticles");
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(0, 1.2f, 2.0f);

            speedLinesSystem = obj.AddComponent<ParticleSystem>();
            var main = speedLinesSystem.main;
            main.startLifetime = 0.22f;
            main.startSpeed = -22f; // Bergerak meluncur cepat ke arah belakang
            main.startSize = 0.08f;
            main.startColor = new Color(0.8f, 0.95f, 1.0f, 0.6f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.loop = true;
            main.playOnAwake = false;

            var emission = speedLinesSystem.emission;
            emission.rateOverTime = 30f;

            var shape = speedLinesSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.4f;

            var rend = obj.GetComponent<ParticleSystemRenderer>();
            rend.material = CreateParticleMat(true);
        }

        private void SetupHitSparksSystem()
        {
            GameObject obj = new GameObject("HitSparksParticles");
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(0, 1.0f, 0.2f);

            hitSparksSystem = obj.AddComponent<ParticleSystem>();
            var main = hitSparksSystem.main;
            main.startLifetime = 0.45f;
            main.startSpeed = 6f;
            main.startSize = 0.2f;
            main.startColor = new Color(1f, 0.3f, 0.1f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            main.playOnAwake = false;

            var emission = hitSparksSystem.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            var shape = hitSparksSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var rend = obj.GetComponent<ParticleSystemRenderer>();
            rend.material = CreateParticleMat(true);
        }

        public void UpdateEffects(CharacterMovementState state, float currentSpeed, bool isGrounded)
        {
            // 1. Debu Lari (Hanya saat berlari di tanah)
            if (runDustSystem != null)
            {
                if (state == CharacterMovementState.Running && isGrounded && currentSpeed > 2f)
                {
                    if (!runDustSystem.isPlaying) runDustSystem.Play();
                }
                else
                {
                    if (runDustSystem.isPlaying) runDustSystem.Stop();
                }
            }

            // 2. Percikan Gesekan Slide
            if (slideSparksSystem != null)
            {
                if (state == CharacterMovementState.Sliding && isGrounded)
                {
                    if (!slideSparksSystem.isPlaying) slideSparksSystem.Play();
                }
                else
                {
                    if (slideSparksSystem.isPlaying) slideSparksSystem.Stop();
                }
            }

            // 3. Speed Lines (Saat mendekati kecepatan maksimal > 16 m/s)
            if (speedLinesSystem != null)
            {
                if (currentSpeed >= GameConstants.MAX_RUN_SPEED * 0.85f && state != CharacterMovementState.Finished)
                {
                    if (!speedLinesSystem.isPlaying) speedLinesSystem.Play();
                }
                else
                {
                    if (speedLinesSystem.isPlaying) speedLinesSystem.Stop();
                }
            }
        }

        public void SetCustomTrailColor(Color color)
        {
            this.dustColor = color;
            if (runDustSystem != null)
            {
                var main = runDustSystem.main;
                main.startColor = color;
            }
        }

        public void PlayHitEffect()
        {
            if (hitSparksSystem != null)
            {
                hitSparksSystem.Play();
            }
        }
    }
}
