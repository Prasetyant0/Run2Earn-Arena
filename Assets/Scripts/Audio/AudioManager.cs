using System;
using UnityEngine;

namespace GameBalap.Audio
{
    /// <summary>
    /// Audio Manager dengan sistem sintesis suara prosedural (zero-dependency, tidak membutuhkan file audio eksternal).
    /// Menghasilkan BGM bertempo cepat (135 BPM) dan SFX (lompat, slide, koin, power-up, tabrakan, perisai pecah) 
    /// secara matematis menggunakan AudioClip.Create.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource sfxSource;
        private AudioSource bgmSource;

        // Cached synthesized clips
        private AudioClip clipJump;
        private AudioClip clipSlide;
        private AudioClip clipCoin;
        private AudioClip clipPowerUp;
        private AudioClip clipHit;
        private AudioClip clipShieldBreak;
        private AudioClip clipVictory;
        private AudioClip clipRaceBGM;

        private const int SAMPLE_RATE = 44100;
        private bool isMuted = false;

        public bool IsMuted => isMuted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Setup AudioSources
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D UI / SFX

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = 0.45f;
            bgmSource.spatialBlend = 0f;

            // Buat seluruh sound FX secara prosedural
            GenerateAllClips();
        }

        private void GenerateAllClips()
        {
            clipJump = CreateJumpSFX();
            clipSlide = CreateSlideSFX();
            clipCoin = CreateCoinSFX();
            clipPowerUp = CreatePowerUpSFX();
            clipHit = CreateHitSFX();
            clipShieldBreak = CreateShieldBreakSFX();
            clipVictory = CreateVictorySFX();
            clipRaceBGM = CreateLoopingRaceBGM();
        }

        #region Public SFX & BGM Methods

        public void PlayJump()
        {
            PlaySFX(clipJump, 0.7f);
        }

        public void PlaySlide()
        {
            PlaySFX(clipSlide, 0.6f);
        }

        public void PlayCoin()
        {
            PlaySFX(clipCoin, 0.75f);
        }

        public void PlayPowerUp()
        {
            PlaySFX(clipPowerUp, 0.85f);
        }

        public void PlayHit()
        {
            PlaySFX(clipHit, 0.9f);
        }

        public void PlayShieldBreak()
        {
            PlaySFX(clipShieldBreak, 0.85f);
        }

        public void PlayVictory()
        {
            PlaySFX(clipVictory, 0.9f);
        }

        public void StartRaceBGM()
        {
            if (isMuted || bgmSource == null || clipRaceBGM == null) return;
            bgmSource.clip = clipRaceBGM;
            bgmSource.Play();
        }

        public void StopRaceBGM()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Stop();
            }
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
            if (bgmSource != null)
            {
                bgmSource.mute = isMuted;
            }
            if (sfxSource != null)
            {
                sfxSource.mute = isMuted;
            }
        }

        private void PlaySFX(AudioClip clip, float volume)
        {
            if (isMuted || sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }

        #endregion

        #region Procedural Audio Generators

        /// <summary>
        /// SFX Lompat: Sapuan frekuensi naik (pitch sweep up) dari 150 Hz ke 480 Hz.
        /// </summary>
        private AudioClip CreateJumpSFX()
        {
            float duration = 0.22f;
            int numSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float freq = Mathf.Lerp(160f, 520f, t / duration);
                float env = 1f - (t / duration); // Linear fadeout
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("SFX_Jump", numSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX Meluncur (Slide): Suara desisan gesekan aspal yang halus.
        /// </summary>
        private AudioClip CreateSlideSFX()
        {
            float duration = 0.35f;
            int numSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[numSamples];
            System.Random rnd = new System.Random(42);

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float env = Mathf.Sin((t / duration) * Mathf.PI); // Half sine curve
                // Tone rendah bercampur desis
                float rumble = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.4f;
                samples[i] = (noise * 0.4f + rumble) * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create("SFX_Slide", numSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX Koin: Dua nada beruntun (B5: 987Hz lalu E6: 1318Hz) dengan decay lonceng emas klasik arcade.
        /// </summary>
        private AudioClip CreateCoinSFX()
        {
            float duration = 0.32f;
            int numSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float freq = t < 0.08f ? 987.77f : 1318.51f;
                float decay = Mathf.Exp(-t * 11f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * decay * 0.8f;
            }

            AudioClip clip = AudioClip.Create("SFX_Coin", numSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX Power-Up: Arpeggio akor naik (C - E - G - C) berbinar kosmik.
        /// </summary>
        private AudioClip CreatePowerUpSFX()
        {
            float duration = 0.45f;
            int numSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[numSamples];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5, E5, G5, C6

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                int noteIndex = Mathf.Clamp((int)((t / duration) * notes.Length), 0, notes.Length - 1);
                float freq = notes[noteIndex];
                float env = 1f - ((t / duration) * 0.4f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.65f;
            }

            AudioClip clip = AudioClip.Create("SFX_PowerUp", numSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX Tabrakan (Hit): Hentakan berat (thud) berfrekuensi rendah ditambah ledakan noise pendek.
        /// </summary>
        private AudioClip CreateHitSFX()
        {
            float duration = 0.35f;
            int numSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[numSamples];
            System.Random rnd = new System.Random(99);

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 35f, t / duration) * t);
                float decay = Mathf.Exp(-t * 14f);
                samples[i] = (thud * 0.7f + noise * 0.5f) * decay;
            }

            AudioClip clip = AudioClip.Create("SFX_Hit", numSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX Perisai Pecah: Denting kristal energi pecah beresonansi.
        /// </summary>
        private AudioClip CreateShieldBreakSFX()
        {
            float duration = 0.4f;
            int numSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float tone1 = Mathf.Sin(2f * Mathf.PI * 1480f * t);
                float tone2 = Mathf.Sin(2f * Mathf.PI * 2200f * t);
                float decay = Mathf.Exp(-t * 9f);
                samples[i] = (tone1 * 0.5f + tone2 * 0.5f) * decay * 0.8f;
            }

            AudioClip clip = AudioClip.Create("SFX_ShieldBreak", numSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX Kemenangan: Fanfare 3 nada ceria saat finish juara.
        /// </summary>
        private AudioClip CreateVictorySFX()
        {
            float duration = 0.8f;
            int numSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[numSamples];
            float[] fanfare = { 587.33f, 739.99f, 880f }; // D5, F#5, A5

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                int idx = Mathf.Clamp((int)((t / duration) * fanfare.Length), 0, fanfare.Length - 1);
                float decay = Mathf.Exp(-(t % (duration / 3f)) * 4f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * fanfare[idx] * t) * decay * 0.75f;
            }

            AudioClip clip = AudioClip.Create("SFX_Victory", numSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// BGM Balapan 135 BPM: Bassline synth elektronik berenergi tinggi yang berulang (seamless loop).
        /// </summary>
        private AudioClip CreateLoopingRaceBGM()
        {
            // 4 baris birama pada 135 BPM = 16 beat. 
            // 60 / 135 * 16 = ~7.11 detik
            float bpm = 135f;
            float beatDuration = 60f / bpm;
            int totalBeats = 16;
            float totalDuration = beatDuration * totalBeats;
            int numSamples = (int)(SAMPLE_RATE * totalDuration);
            float[] samples = new float[numSamples];

            // Bass note frequencies (D minor progression: D - F - C - G)
            float[] bassPattern = {
                146.83f, 146.83f, 146.83f, 174.61f, // D3, D3, D3, F3
                130.81f, 130.81f, 130.81f, 146.83f, // C3, C3, C3, D3
                98.00f,  98.00f,  110.00f, 130.81f, // G2, G2, A2, C3
                146.83f, 146.83f, 220.00f, 146.83f  // D3, D3, A3, D3
            };

            System.Random rnd = new System.Random(77);

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                int currentBeat = (int)(t / beatDuration) % totalBeats;
                float beatTime = t % beatDuration;

                // 1. Synth Bassline berdenyut
                float bassFreq = bassPattern[currentBeat];
                float bassEnv = Mathf.Exp(-beatTime * 6f);
                float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * bassEnv * 0.5f;

                // 2. Hi-Hat Shaker pada setiap off-beat
                float hat = 0f;
                if (beatTime > beatDuration * 0.5f)
                {
                    float hatTime = beatTime - (beatDuration * 0.5f);
                    float hatNoise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                    hat = hatNoise * Mathf.Exp(-hatTime * 28f) * 0.22f;
                }

                // 3. Kick Drum pada setiap ketukan (beat)
                float kick = 0f;
                if (beatTime < 0.12f)
                {
                    float kickFreq = Mathf.Lerp(110f, 40f, beatTime / 0.12f);
                    kick = Mathf.Sin(2f * Mathf.PI * kickFreq * beatTime) * Mathf.Exp(-beatTime * 18f) * 0.55f;
                }

                samples[i] = Mathf.Clamp(bass + hat + kick, -1f, 1f) * 0.45f;
            }

            AudioClip clip = AudioClip.Create("BGM_SynthRace_Loop", numSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
