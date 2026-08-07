using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Handles procedural synthesis and playback of jump whoosh and continuous metallic grinding audio effects.
    /// Auto-attaches to player GameObject if missing.
    /// </summary>
    public class PlayerAudio : MonoBehaviour
    {
        public static PlayerAudio Instance { get; private set; }

        private AudioSource jumpAudioSource;
        private AudioSource grindAudioSource;
        private AudioSource trickAudioSource;

        private AudioClip jumpClip;
        private AudioClip metallicGrindClip;
        private AudioClip spinTrickClip;

        private PlayerMovement playerMovement;

        private void Awake()
        {
            Instance = this;
            playerMovement = GetComponent<PlayerMovement>();

            // Setup Audio Sources
            jumpAudioSource = gameObject.AddComponent<AudioSource>();
            jumpAudioSource.playOnAwake = false;
            jumpAudioSource.spatialBlend = 0f; // 2D Sound

            grindAudioSource = gameObject.AddComponent<AudioSource>();
            grindAudioSource.playOnAwake = false;
            grindAudioSource.loop = true;
            grindAudioSource.spatialBlend = 0f; // 2D Sound

            trickAudioSource = gameObject.AddComponent<AudioSource>();
            trickAudioSource.playOnAwake = false;
            trickAudioSource.spatialBlend = 0f; // 2D Sound

            // Synthesize procedural audio clips
            jumpClip = GenerateJumpClip();
            metallicGrindClip = GenerateMetallicGrindClip();
            spinTrickClip = GenerateSpinTrickClip();

            grindAudioSource.clip = metallicGrindClip;
        }

        private void OnEnable()
        {
            UISystem.ScoreManager.OnTrickScored += HandleTrickScored;
        }

        private void OnDisable()
        {
            UISystem.ScoreManager.OnTrickScored -= HandleTrickScored;
        }

        private void HandleTrickScored(string trickName, int points, int multiplier)
        {
            // Play spin trick sound when performing air/spin tricks (360, Backflip, Frontflip, etc.)
            PlaySpinTrickSound();
        }

        private void Update()
        {
            if (playerMovement == null) return;

            // Handle Metallic Grind Audio Loop
            if (playerMovement.isGrinding)
            {
                if (!grindAudioSource.isPlaying)
                {
                    grindAudioSource.volume = 0.4f;
                    grindAudioSource.Play();
                }
            }
            else
            {
                if (grindAudioSource.isPlaying)
                {
                    grindAudioSource.Stop();
                }
            }
        }

        /// <summary>
        /// Plays procedural jump sound.
        /// </summary>
        public void PlayJumpSound()
        {
            if (jumpAudioSource != null && jumpClip != null)
            {
                jumpAudioSource.PlayOneShot(jumpClip, 0.6f);
            }
        }

        /// <summary>
        /// Plays procedural spin/air trick sound.
        /// </summary>
        public void PlaySpinTrickSound()
        {
            if (trickAudioSource != null && spinTrickClip != null)
            {
                trickAudioSource.PlayOneShot(spinTrickClip, 0.75f);
            }
        }

        /// <summary>
        /// Generates a clean synthetic jump whoosh/pop sound clip.
        /// </summary>
        private static AudioClip GenerateJumpClip()
        {
            int sampleRate = 44100;
            float duration = 0.16f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Frequency sweeps from 180Hz up to 480Hz
                float freq = Mathf.Lerp(180f, 480f, Mathf.Pow(progress, 0.6f));
                float phase = 2f * Mathf.PI * freq * t;

                // Wave synthesis: Sine + soft harmonic
                float wave = Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 2f) * 0.25f;

                // Envelope: quick attack, smooth decay
                float envelope = (progress < 0.15f) ? (progress / 0.15f) : Mathf.Pow(1f - progress, 1.8f);

                samples[i] = wave * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("JumpSynth", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a warm metallic rail grind: brown noise rumble + low buzz vibration + subtle rhythmic wheel tapping.
        /// </summary>
        private static AudioClip GenerateMetallicGrindClip()
        {
            int sampleRate = 44100;
            float duration = 1.0f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            System.Random rand = new System.Random(42);

            float brownNoise = 0f; // Integrated white noise for deep rumble

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;

                // Brown noise: integrate white noise for deep, bassy rumble
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                brownNoise += white * 0.02f;
                brownNoise *= 0.998f; // Slight decay to prevent drift
                float rumble = Mathf.Clamp(brownNoise, -1f, 1f);

                // Low-frequency metal frame buzz (95Hz fundamental)
                float buzz = Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.15f;

                // Subtle rhythmic wheel clicking pattern (~18Hz modulation)
                float wheelTap = Mathf.Pow(Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 18f * t)), 8f) * 0.12f;

                // Combine: mostly rumble, hint of buzz and tapping
                float combined = (rumble * 0.55f) + buzz + wheelTap;

                samples[i] = combined * 0.30f;
            }

            AudioClip clip = AudioClip.Create("MetallicGrindSynth", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a fast swirling air whoosh/spin tone for spin tricks (360, Backflip, Frontflip, etc.).
        /// </summary>
        private static AudioClip GenerateSpinTrickClip()
        {
            int sampleRate = 44100;
            float duration = 0.24f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            System.Random rand = new System.Random(77);
            float lastNoise = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Swirling frequency sweep (pitch rises quickly then drops slightly)
                float freq = Mathf.Sin(progress * Mathf.PI) * 550f + 220f;
                float phase = 2f * Mathf.PI * freq * t;

                // Filtered air whoosh noise
                float rawNoise = (float)(rand.NextDouble() * 2.0 - 1.0);
                lastNoise = Mathf.Lerp(lastNoise, rawNoise, 0.30f);

                // Combine sine wave + filtered air noise
                float tone = Mathf.Sin(phase) * 0.6f + Mathf.Sin(phase * 1.5f) * 0.4f;
                float whoosh = lastNoise * 0.5f;

                // Parabolic envelope
                float envelope = Mathf.Sin(progress * Mathf.PI);

                samples[i] = (tone * 0.5f + whoosh * 0.5f) * envelope * 0.6f;
            }

            AudioClip clip = AudioClip.Create("SpinTrickSynth", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
