using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// Procedural Sound Manager that generates crisp, juicy sound effects 
    /// dynamically without needing external audio files. Includes booster and coin SFX.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager s_Instance;
        public static SoundManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindAnyObjectByType<SoundManager>();
                }
                return s_Instance;
            }
            private set => s_Instance = value;
        }

        private AudioSource m_AudioSource;
        private AudioSource m_MusicSource;
        private AudioClip m_BgmClip;
        private AudioClip m_PickUpClip;
        private AudioClip m_PlaceClip;
        private AudioClip m_BlastClip;
        private AudioClip m_ComboClip;
        private AudioClip m_GameOverClip;
        private AudioClip m_BombClip;
        private AudioClip m_CannonClip;
        private AudioClip m_ArrowClip;
        private AudioClip m_ShuffleClip;
        private AudioClip m_CoinClip;
        private AudioClip m_PurchaseClip;

        public bool IsMusicEnabled { get; private set; } = true;
        public bool IsSfxEnabled { get; private set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_Instance = null;
        }

        private void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
            }
            else if (s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }

            m_MusicSource = gameObject.AddComponent<AudioSource>();
            m_MusicSource.loop = true;
            m_MusicSource.volume = 0.35f;

            IsMusicEnabled = PlayerPrefs.GetInt("BoxBlast_Music", 1) == 1;
            IsSfxEnabled = PlayerPrefs.GetInt("BoxBlast_SFX", 1) == 1;

            GenerateProceduralAudio();

            if (IsMusicEnabled && m_BgmClip != null)
            {
                m_MusicSource.clip = m_BgmClip;
                m_MusicSource.Play();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ToggleMusic()
        {
            IsMusicEnabled = !IsMusicEnabled;
            PlayerPrefs.SetInt("BoxBlast_Music", IsMusicEnabled ? 1 : 0);
            PlayerPrefs.Save();

            if (m_MusicSource != null)
            {
                if (IsMusicEnabled)
                {
                    if (m_BgmClip != null && !m_MusicSource.isPlaying)
                    {
                        m_MusicSource.clip = m_BgmClip;
                        m_MusicSource.Play();
                    }
                }
                else
                {
                    m_MusicSource.Pause();
                }
            }
        }

        public void ToggleSFX()
        {
            IsSfxEnabled = !IsSfxEnabled;
            PlayerPrefs.SetInt("BoxBlast_SFX", IsSfxEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void GenerateProceduralAudio()
        {
            m_BgmClip = CreateAmbientMusicClip("AmbientLofiBGM", 12f);
            m_PickUpClip = CreateToneClip("PickUp", 0.08f, 520f, 780f, 0.4f);
            m_PlaceClip = CreateToneClip("Place", 0.10f, 320f, 200f, 0.5f);
            m_BlastClip = CreateBlastClip("Blast", 0.28f);
            m_ComboClip = CreateArpeggioClip("Combo", 0.35f);
            m_GameOverClip = CreateGameOverClip("GameOver", 0.6f);

            // Boosters & Shop SFX
            m_BombClip = CreateBombExplosionClip("Bomb", 0.45f);
            m_CannonClip = CreateCannonClip("Cannon", 0.38f);
            m_ArrowClip = CreateToneClip("Arrow", 0.12f, 850f, 420f, 0.55f);
            m_ShuffleClip = CreateArpeggioClip("Shuffle", 0.25f);
            m_CoinClip = CreateToneClip("Coin", 0.10f, 987.77f, 1318.51f, 0.5f); // B5 to E6 chime
            m_PurchaseClip = CreateArpeggioClip("Purchase", 0.40f);
        }

        public void PlayPickUp()
        {
            PlayClip(m_PickUpClip, 0.6f, Random.Range(0.98f, 1.05f));
        }

        public void PlayButtonClick()
        {
            PlayClip(m_PickUpClip, 0.5f, 1.1f);
        }

        public void PlayPlace()
        {
            PlayClip(m_PlaceClip, 0.7f, Random.Range(0.95f, 1.05f));
        }

        private static readonly float[] PentatonicScale = new float[] {
            1.000f, // C (combo 1)
            1.122f, // D (combo 2)
            1.260f, // E (combo 3)
            1.335f, // F (combo 4)
            1.498f, // G (combo 5)
            1.682f, // A (combo 6)
            1.888f, // B (combo 7)
            2.000f  // High C (combo 8+)
        };

        public void PlayBlast(int comboMultiplier = 1)
        {
            int index = Mathf.Clamp(comboMultiplier - 1, 0, PentatonicScale.Length - 1);
            float pitch = PentatonicScale[index];
            PlayClip(m_BlastClip, 0.88f, pitch);
        }

        public void PlayCombo(int comboMultiplier = 2)
        {
            int index = Mathf.Clamp(comboMultiplier - 1, 0, PentatonicScale.Length - 1);
            float pitch = PentatonicScale[index];
            PlayClip(m_ComboClip, 0.85f, pitch);
        }

        public void PlayGameOver()
        {
            PlayClip(m_GameOverClip, 0.75f, 1.0f);
        }

        public void PlayBomb()
        {
            PlayClip(m_BombClip, 0.95f, 1.0f);
        }

        public void PlayCannon()
        {
            PlayClip(m_CannonClip, 0.95f, 1.0f);
        }

        public void PlayArrow()
        {
            PlayClip(m_ArrowClip, 0.8f, 1.1f);
        }

        public void PlayShuffle()
        {
            PlayClip(m_ShuffleClip, 0.75f, 1.15f);
        }

        public void PlayCoin()
        {
            PlayClip(m_CoinClip, 0.7f, Random.Range(0.98f, 1.04f));
        }

        public void PlayPurchase()
        {
            PlayClip(m_PurchaseClip, 0.85f, 1.0f);
        }

        private void PlayClip(AudioClip clip, float volume, float pitch)
        {
            if (!IsSfxEnabled) return;
            if (m_AudioSource != null && clip != null)
            {
                m_AudioSource.pitch = pitch;
                m_AudioSource.PlayOneShot(clip, volume);
            }
        }

        private static AudioClip CreateAmbientMusicClip(string clipName, float duration)
        {
            int sampleRate = 22050;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // 4-Chord ambient progression: Cmaj7, Am7, Fmaj7, G6 (3.0s per bar, total 12.0s loop)
            float[] roots = { 130.81f, 110.00f, 87.31f, 98.00f }; // C3, A2, F2, G2
            int[][] chordIntervals = new int[][]
            {
                new int[] { 0, 4, 7, 11, 16 }, // Cmaj7
                new int[] { 0, 3, 7, 10, 15 }, // Am7
                new int[] { 0, 4, 7, 11, 16 }, // Fmaj7
                new int[] { 0, 4, 7, 9, 14 }   // G6
            };

            float[] melodyFreqs = {
                523.25f, 659.25f, 587.33f, 783.99f, // C5, E5, D5, G5
                440.00f, 523.25f, 659.25f, 587.33f, // A4, C5, E5, D5
                349.23f, 440.00f, 523.25f, 659.25f, // F4, A4, C5, E5
                392.00f, 493.88f, 587.33f, 523.25f  // G4, B4, D5, C5
            };

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int barIndex = Mathf.Clamp(Mathf.FloorToInt(t / 3.0f), 0, 3);
                float barT = (t % 3.0f) / 3.0f;

                // 1. Warm sub-bass
                float bassFreq = roots[barIndex] * 0.5f;
                float bassEnv = Mathf.Sin(barT * Mathf.PI);
                float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * bassEnv * 0.22f;

                // 2. Ambient pad chord with shimmer
                float pad = 0f;
                int[] chord = chordIntervals[barIndex];
                float root = roots[barIndex];
                for (int c = 0; c < chord.Length; c++)
                {
                    float noteFreq = root * Mathf.Pow(2f, chord[c] / 12f);
                    float wave1 = Mathf.Sin(2f * Mathf.PI * (noteFreq - 0.35f) * t);
                    float wave2 = Mathf.Sin(2f * Mathf.PI * (noteFreq + 0.35f) * t);
                    pad += (wave1 + wave2) * 0.035f;
                }
                pad *= Mathf.Sin(barT * Mathf.PI);

                // 3. Lo-Fi bell arpeggio (every 0.75s)
                int noteIdx = Mathf.Clamp(Mathf.FloorToInt(t / 0.75f), 0, melodyFreqs.Length - 1);
                float noteT = (t % 0.75f) / 0.75f;
                float bellEnv = Mathf.Exp(-noteT * 4.5f);
                float bellFreq = melodyFreqs[noteIdx];
                float bell = (Mathf.Sin(2f * Mathf.PI * bellFreq * t) * 0.7f +
                              Mathf.Sin(4f * Mathf.PI * bellFreq * t) * 0.3f) * bellEnv * 0.16f;

                float sample = bass + pad + bell;
                float loopFade = Mathf.Clamp01(t * 10f) * Mathf.Clamp01((duration - t) * 10f);
                samples[i] = sample * loopFade;
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateToneClip(string clipName, float duration, float startFreq, float endFreq, float volume)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
                float env = 1f - t;
                samples[i] = Mathf.Sin(2 * Mathf.PI * currentFreq * (i / (float)sampleRate)) * env * volume;
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateBlastClip(string clipName, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float freq = Mathf.Lerp(450f, 90f, t * t);
                float env = Mathf.Pow(1f - t, 1.5f);
                float noise = (Random.value * 2f - 1f) * 0.35f * env;
                float sine = Mathf.Sin(2 * Mathf.PI * freq * (i / (float)sampleRate)) * 0.65f * env;
                samples[i] = (sine + noise) * 0.7f;
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateBombExplosionClip(string clipName, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float bassFreq = Mathf.Lerp(160f, 40f, t);
                float env = Mathf.Pow(1f - t, 1.2f);
                float noise = (Random.value * 2f - 1f) * 0.55f * env;
                float sub = Mathf.Sin(2 * Mathf.PI * bassFreq * (i / (float)sampleRate)) * 0.65f * env;
                samples[i] = (sub + noise) * 0.85f;
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateCannonClip(string clipName, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float freq = Mathf.Lerp(280f, 55f, Mathf.Sqrt(t));
                float env = Mathf.Pow(1f - t, 1.8f);
                float noise = (Random.value * 2f - 1f) * 0.40f * env;
                float thud = Mathf.Sin(2 * Mathf.PI * freq * (i / (float)sampleRate)) * 0.70f * env;
                samples[i] = (thud + noise) * 0.85f;
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateArpeggioClip(string clipName, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];
            float[] notes = { 440f, 554.37f, 659.25f, 880f }; // A Major chord

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t * notes.Length), 0, notes.Length - 1);
                float noteT = (t * notes.Length) % 1f;
                float env = 1f - noteT;
                samples[i] = Mathf.Sin(2 * Mathf.PI * notes[noteIndex] * (i / (float)sampleRate)) * env * 0.45f;
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateGameOverClip(string clipName, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];
            float[] notes = { 400f, 370f, 330f, 260f }; // Descending minor tones

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t * notes.Length), 0, notes.Length - 1);
                float noteT = (t * notes.Length) % 1f;
                float env = 1f - noteT;
                samples[i] = Mathf.Sin(2 * Mathf.PI * notes[noteIndex] * (i / (float)sampleRate)) * env * 0.45f;
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
