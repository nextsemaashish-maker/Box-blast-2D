using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// High-performance procedural Particle and Visual FX manager.
    /// Uses pre-allocated object pooling (zero GC allocations during gameplay)
    /// to deliver sparkling star bursts, shockwave rings, and combo celebration effects.
    /// </summary>
    public class ParticleFXManager : MonoBehaviour
    {
        public static ParticleFXManager Instance { get; private set; }

        private class SparkleItem
        {
            public GameObject obj;
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector3 position;
            public Vector3 velocity;
            public float life;
            public float maxLife;
            public float rotSpeed;
            public float baseScale;
            public Color baseColor;
            public bool active;
        }

        private class ShockwaveItem
        {
            public GameObject obj;
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector3 position;
            public float life;
            public float duration;
            public float startScale;
            public float endScale;
            public Color baseColor;
            public bool active;
        }

        private const int SparklePoolSize = 120;
        private const int ShockwavePoolSize = 12;

        private readonly List<SparkleItem> m_SparklePool = new List<SparkleItem>(SparklePoolSize);
        private readonly List<ShockwaveItem> m_ShockwavePool = new List<ShockwaveItem>(ShockwavePoolSize);

        private Sprite m_SparkleSprite;
        private Sprite m_RingSprite;

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

            m_SparkleSprite = SpriteFactory.GetSparkleShardSprite();
            m_RingSprite = SpriteFactory.GetShockwaveRingSprite();

            InitializePools();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void InitializePools()
        {
            // 1. Sparkle Shards Pool
            for (int i = 0; i < SparklePoolSize; i++)
            {
                GameObject obj = new GameObject($"Sparkle_{i}");
                obj.transform.SetParent(transform, false);

                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = m_SparkleSprite;
                sr.sortingOrder = 25; // Above blocks & cells
                obj.SetActive(false);

                m_SparklePool.Add(new SparkleItem
                {
                    obj = obj,
                    transform = obj.transform,
                    renderer = sr,
                    active = false
                });
            }

            // 2. Shockwave Rings Pool
            for (int i = 0; i < ShockwavePoolSize; i++)
            {
                GameObject obj = new GameObject($"Shockwave_{i}");
                obj.transform.SetParent(transform, false);

                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = m_RingSprite;
                sr.sortingOrder = 24;
                obj.SetActive(false);

                m_ShockwavePool.Add(new ShockwaveItem
                {
                    obj = obj,
                    transform = obj.transform,
                    renderer = sr,
                    active = false
                });
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Update Sparkles
            for (int i = 0; i < m_SparklePool.Count; i++)
            {
                SparkleItem item = m_SparklePool[i];
                if (!item.active) continue;

                item.life += dt;
                if (item.life >= item.maxLife)
                {
                    item.active = false;
                    item.obj.SetActive(false);
                    continue;
                }

                float progress = item.life / item.maxLife;

                // Air friction drag
                item.velocity *= Mathf.Pow(0.86f, dt * 60f);
                item.position += item.velocity * dt;
                item.transform.position = item.position;

                // Continuous rotation
                item.transform.Rotate(0f, 0f, item.rotSpeed * dt);

                // Scale: quick pop then elastic shrink
                float scaleNorm = 1.0f - progress * progress;
                float currentScale = item.baseScale * Mathf.Max(0f, scaleNorm);
                item.transform.localScale = new Vector3(currentScale, currentScale, 1f);

                // Alpha fadeout
                Color c = item.baseColor;
                c.a = item.baseColor.a * (1f - progress);
                item.renderer.color = c;
            }

            // 2. Update Shockwaves
            for (int i = 0; i < m_ShockwavePool.Count; i++)
            {
                ShockwaveItem item = m_ShockwavePool[i];
                if (!item.active) continue;

                item.life += dt;
                if (item.life >= item.duration)
                {
                    item.active = false;
                    item.obj.SetActive(false);
                    continue;
                }

                float progress = item.life / item.duration;
                // Cubic ease-out expansion
                float ease = 1f - Mathf.Pow(1f - progress, 3f);
                float currentScale = Mathf.Lerp(item.startScale, item.endScale, ease);
                item.transform.localScale = new Vector3(currentScale, currentScale, 1f);

                // Alpha fadeout
                Color c = item.baseColor;
                c.a = item.baseColor.a * (1f - progress * progress);
                item.renderer.color = c;
            }
        }

        // ==========================================
        // 💥 PARTICLE SPAWN METHODS
        // ==========================================

        /// <summary>
        /// Spawns a radial burst of colorful glowing sparkle shards at a cleared cell.
        /// </summary>
        public void PlayCellBlast(Vector3 worldPos, Color blockColor, float delay = 0f)
        {
            if (delay > 0f)
            {
                StartCoroutine(DelayedCellBlast(worldPos, blockColor, delay));
            }
            else
            {
                SpawnCellBlastNow(worldPos, blockColor);
            }
        }

        private IEnumerator DelayedCellBlast(Vector3 worldPos, Color blockColor, float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnCellBlastNow(worldPos, blockColor);
        }

        private void SpawnCellBlastNow(Vector3 worldPos, Color blockColor)
        {
            int count = Random.Range(8, 12);
            for (int i = 0; i < count; i++)
            {
                SparkleItem item = GetAvailableSparkle();
                if (item == null) break;

                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(3.2f, 5.8f);
                Vector3 vel = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed;

                // Slightly tint towards bright energetic colors
                Color sparkColor = Color.Lerp(blockColor, Color.white, Random.Range(0.25f, 0.65f));
                sparkColor.a = 1.0f;

                item.position = worldPos + (Vector3)(Random.insideUnitCircle * 0.12f);
                item.velocity = vel;
                item.life = 0f;
                item.maxLife = Random.Range(0.32f, 0.48f);
                item.rotSpeed = Random.Range(-450f, 450f);
                item.baseScale = Random.Range(0.35f, 0.62f);
                item.baseColor = sparkColor;
                item.active = true;

                item.transform.position = item.position;
                item.transform.localScale = Vector3.zero;
                item.renderer.color = sparkColor;
                item.obj.SetActive(true);
            }
        }

        /// <summary>
        /// Spawns energetic directional sparks spraying outward perpendicularly from a laser sweep line.
        /// </summary>
        public void PlayDirectionalBlast(Vector3 worldPos, Color blockColor, Vector2 axisDirection)
        {
            Vector2 perp = new Vector2(-axisDirection.y, axisDirection.x);
            int count = Random.Range(10, 16);

            for (int i = 0; i < count; i++)
            {
                SparkleItem item = GetAvailableSparkle();
                if (item == null) break;

                float side = (i % 2 == 0) ? 1f : -1f;
                Vector2 dir = (perp * side + Random.insideUnitCircle * 0.45f).normalized;
                float speed = Random.Range(4.5f, 8.5f);
                Vector3 vel = new Vector3(dir.x, dir.y, 0f) * speed;

                Color sparkColor = Color.Lerp(blockColor, Color.white, Random.Range(0.35f, 0.85f));
                sparkColor.a = 1.0f;

                item.position = worldPos + (Vector3)(Random.insideUnitCircle * 0.08f);
                item.velocity = vel;
                item.life = 0f;
                item.maxLife = Random.Range(0.28f, 0.44f);
                item.rotSpeed = Random.Range(-600f, 600f);
                item.baseScale = Random.Range(0.38f, 0.68f);
                item.baseColor = sparkColor;
                item.active = true;

                item.transform.position = item.position;
                item.transform.localScale = Vector3.zero;
                item.renderer.color = sparkColor;
                item.obj.SetActive(true);
            }
        }

        /// <summary>
        /// Spawns a radial shockwave ring and bright star burst at an intersection point.
        /// </summary>
        public void PlayIntersectionBurst(Vector3 worldPos, Color flareColor)
        {
            ShockwaveItem ring = GetAvailableShockwave();
            if (ring != null)
            {
                ring.position = worldPos;
                ring.life = 0f;
                ring.duration = 0.32f;
                ring.startScale = 0.25f;
                ring.endScale = 2.4f;
                ring.baseColor = Color.Lerp(flareColor, Color.white, 0.5f);
                ring.active = true;
                ring.transform.position = worldPos;
                ring.transform.localScale = Vector3.one * ring.startScale;
                ring.renderer.color = ring.baseColor;
                ring.obj.SetActive(true);
            }

            SpawnCellBlastNow(worldPos, flareColor);
        }

        /// <summary>
        /// Spawns an expanding golden shockwave ring and star shower for 3x3 zone clears.
        /// </summary>
        public void PlayZoneClearShockwave(Vector3 centerPos)
        {
            // 1. Shockwave Ring
            ShockwaveItem ring = GetAvailableShockwave();
            if (ring != null)
            {
                ring.position = centerPos;
                ring.life = 0f;
                ring.duration = 0.38f;
                ring.startScale = 0.4f;
                ring.endScale = 3.6f;
                ring.baseColor = new Color(1.0f, 0.88f, 0.35f, 0.95f); // Brilliant celestial gold
                ring.active = true;

                ring.transform.position = centerPos;
                ring.transform.localScale = Vector3.one * ring.startScale;
                ring.renderer.color = ring.baseColor;
                ring.obj.SetActive(true);
            }

            // 2. Extra Golden Star Shards
            Color gold = new Color(1.0f, 0.85f, 0.25f);
            Color cyan = new Color(0.25f, 0.90f, 1.0f);
            for (int i = 0; i < 20; i++)
            {
                SparkleItem item = GetAvailableSparkle();
                if (item == null) break;

                float angle = (i / 20f) * Mathf.PI * 2f + Random.Range(-0.1f, 0.1f);
                float speed = Random.Range(4.5f, 7.5f);
                Vector3 vel = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed;

                Color c = (i % 2 == 0) ? gold : cyan;

                item.position = centerPos;
                item.velocity = vel;
                item.life = 0f;
                item.maxLife = Random.Range(0.40f, 0.60f);
                item.rotSpeed = Random.Range(-500f, 500f);
                item.baseScale = Random.Range(0.45f, 0.75f);
                item.baseColor = c;
                item.active = true;

                item.transform.position = item.position;
                item.transform.localScale = Vector3.zero;
                item.renderer.color = c;
                item.obj.SetActive(true);
            }
        }

        /// <summary>
        /// Spawns multi-colored celebratory confetti sparks on multi-line combos.
        /// </summary>
        public void PlayComboCelebration(Vector3 centerPos, int comboCount)
        {
            int count = Mathf.Min(30, 14 + comboCount * 4);
            Color[] festive = new Color[]
            {
                new Color(1f, 0.85f, 0.2f),  // Gold
                new Color(0.2f, 0.9f, 1f),   // Cyan
                new Color(1f, 0.3f, 0.6f),   // Neon Pink
                new Color(0.2f, 1f, 0.5f),   // Emerald
                new Color(0.7f, 0.4f, 1f)    // Violet
            };

            for (int i = 0; i < count; i++)
            {
                SparkleItem item = GetAvailableSparkle();
                if (item == null) break;

                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(4.0f, 8.0f);
                Vector3 vel = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed;

                Color c = festive[Random.Range(0, festive.Length)];

                item.position = centerPos;
                item.velocity = vel;
                item.life = 0f;
                item.maxLife = Random.Range(0.45f, 0.65f);
                item.rotSpeed = Random.Range(-600f, 600f);
                item.baseScale = Random.Range(0.40f, 0.70f);
                item.baseColor = c;
                item.active = true;

                item.transform.position = item.position;
                item.transform.localScale = Vector3.zero;
                item.renderer.color = c;
                item.obj.SetActive(true);
            }
        }

        /// <summary>
        /// Heavy fiery bomb blast burst.
        /// </summary>
        public void PlayBombBurst(Vector3 centerPos)
        {
            // Expanding fire ring
            ShockwaveItem ring = GetAvailableShockwave();
            if (ring != null)
            {
                ring.position = centerPos;
                ring.life = 0f;
                ring.duration = 0.42f;
                ring.startScale = 0.5f;
                ring.endScale = 4.2f;
                ring.baseColor = new Color(1.0f, 0.45f, 0.12f, 1f);
                ring.active = true;
                ring.transform.position = centerPos;
                ring.transform.localScale = Vector3.one * ring.startScale;
                ring.renderer.color = ring.baseColor;
                ring.obj.SetActive(true);
            }

            // Fiery sparks
            Color[] fireColors = { new Color(1f, 0.9f, 0.3f), new Color(1f, 0.45f, 0.1f), new Color(1f, 0.2f, 0.2f) };
            for (int i = 0; i < 28; i++)
            {
                SparkleItem item = GetAvailableSparkle();
                if (item == null) break;

                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(5.0f, 9.5f);
                Vector3 vel = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed;

                item.position = centerPos;
                item.velocity = vel;
                item.life = 0f;
                item.maxLife = Random.Range(0.35f, 0.55f);
                item.rotSpeed = Random.Range(-700f, 700f);
                item.baseScale = Random.Range(0.45f, 0.85f);
                item.baseColor = fireColors[Random.Range(0, fireColors.Length)];
                item.active = true;

                item.transform.position = item.position;
                item.transform.localScale = Vector3.zero;
                item.renderer.color = item.baseColor;
                item.obj.SetActive(true);
            }
        }

        private SparkleItem GetAvailableSparkle()
        {
            for (int i = 0; i < m_SparklePool.Count; i++)
            {
                if (!m_SparklePool[i].active) return m_SparklePool[i];
            }
            return null;
        }

        private ShockwaveItem GetAvailableShockwave()
        {
            for (int i = 0; i < m_ShockwavePool.Count; i++)
            {
                if (!m_ShockwavePool[i].active) return m_ShockwavePool[i];
            }
            return null;
        }
    }
}
