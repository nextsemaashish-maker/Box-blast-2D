using System.Collections.Generic;
using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// Renders a rich atmospheric radial vignette background and a field of 
    /// gently floating, twinkling stardust glow orbs behind the board.
    /// Runs with zero garbage collection allocations for silky 120 FPS performance.
    /// </summary>
    public class AmbientBackground : MonoBehaviour
    {
        public static AmbientBackground Instance { get; private set; }

        private struct StarMote
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector2 pos;
            public float speed;
            public float swaySpeed;
            public float swayAmp;
            public float baseAlpha;
            public float twinkleSpeed;
            public float phase;
            public Color baseColor;
        }

        private GameObject m_BackdropObj;
        private SpriteRenderer m_BackdropSr;
        private Camera m_Camera;
        private StarMote[] m_Stars;
        private const int StarCount = 32;

        private float m_CamHalfWidth;
        private float m_CamHalfHeight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            m_Camera = Camera.main;
            BuildBackdrop();
            BuildStarField();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildBackdrop()
        {
            m_BackdropObj = new GameObject("CosmicRadialBackdrop");
            m_BackdropObj.transform.SetParent(transform, false);
            m_BackdropObj.transform.position = new Vector3(0f, 0f, 5f);

            m_BackdropSr = m_BackdropObj.AddComponent<SpriteRenderer>();
            m_BackdropSr.sprite = SpriteFactory.GetCosmicStarryBackgroundSprite();
            m_BackdropSr.sortingOrder = -25;
            m_BackdropSr.color = Color.white;
        }

        private void BuildStarField()
        {
            m_Stars = new StarMote[StarCount];
            Sprite starSprite = SpriteFactory.GetSoftGlowOrbSprite();

            Color[] palette = new Color[]
            {
                new Color(0.45f, 0.75f, 1.0f),  // Soft sapphire cyan
                new Color(0.85f, 0.92f, 1.0f),  // Pure starlight white
                new Color(0.70f, 0.45f, 1.0f),  // Soft amethyst violet
                new Color(1.0f,  0.88f, 0.50f)  // Warm celestial gold
            };

            UpdateCameraBounds();

            for (int i = 0; i < StarCount; i++)
            {
                GameObject starObj = new GameObject($"Stardust_{i}");
                starObj.transform.SetParent(transform, false);

                SpriteRenderer sr = starObj.AddComponent<SpriteRenderer>();
                sr.sprite = starSprite;
                sr.sortingOrder = -20;

                float scale = Random.Range(0.08f, 0.22f);
                starObj.transform.localScale = new Vector3(scale, scale, 1f);

                Color col = palette[Random.Range(0, palette.Length)];
                float alpha = Random.Range(0.20f, 0.55f);
                col.a = alpha;
                sr.color = col;

                float startX = Random.Range(-m_CamHalfWidth * 1.15f, m_CamHalfWidth * 1.15f);
                float startY = Random.Range(-m_CamHalfHeight * 1.15f, m_CamHalfHeight * 1.15f);
                starObj.transform.position = new Vector3(startX, startY, 4f);

                m_Stars[i] = new StarMote
                {
                    transform = starObj.transform,
                    renderer = sr,
                    pos = new Vector2(startX, startY),
                    speed = Random.Range(0.06f, 0.20f),
                    swaySpeed = Random.Range(0.4f, 1.2f),
                    swayAmp = Random.Range(0.08f, 0.25f),
                    baseAlpha = alpha,
                    twinkleSpeed = Random.Range(1.0f, 3.0f),
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    baseColor = col
                };
            }
        }

        private void Update()
        {
            if (m_Camera == null) m_Camera = Camera.main;
            UpdateCameraBounds();

            // Resize background plane to always cover camera viewport + shake bleed margin
            if (m_BackdropObj != null)
            {
                float bgWidth = m_CamHalfWidth * 2.6f;
                float bgHeight = m_CamHalfHeight * 2.6f;
                m_BackdropObj.transform.localScale = new Vector3(bgWidth, bgHeight, 1f);
            }

            // Animate floating stardust motes with gentle upward drift and twinkling
            float dt = Time.deltaTime;
            float time = Time.time;

            if (m_Stars != null)
            {
                for (int i = 0; i < m_Stars.Length; i++)
                {
                    ref StarMote star = ref m_Stars[i];

                    // Drift upward
                    star.pos.y += star.speed * dt;

                    // Wrap around if drifted past top
                    if (star.pos.y > m_CamHalfHeight * 1.2f)
                    {
                        star.pos.y = -m_CamHalfHeight * 1.2f;
                        star.pos.x = Random.Range(-m_CamHalfWidth * 1.1f, m_CamHalfWidth * 1.1f);
                    }

                    // Gentle horizontal sinusoidal sway
                    float currentX = star.pos.x + Mathf.Sin(time * star.swaySpeed + star.phase) * star.swayAmp;
                    star.transform.position = new Vector3(currentX, star.pos.y, 4f);

                    // Soft twinkling alpha pulsation
                    float twinkle = 0.65f + 0.35f * Mathf.Sin(time * star.twinkleSpeed + star.phase);
                    Color c = star.baseColor;
                    c.a = star.baseAlpha * twinkle;
                    star.renderer.color = c;
                }
            }
        }

        private void UpdateCameraBounds()
        {
            if (m_Camera == null) return;
            m_CamHalfHeight = m_Camera.orthographicSize;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            m_CamHalfWidth = m_CamHalfHeight * aspect;
        }
    }
}
