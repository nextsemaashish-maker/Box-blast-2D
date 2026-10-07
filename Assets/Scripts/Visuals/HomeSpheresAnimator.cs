using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BoxBlast
{
    /// <summary>
    /// Premium 3D orbital carousel animator for the Home Screen:
    /// - 6 jewel spheres revolving in a smooth, continuous 3D elliptical orbit ("roundly ghume")
    /// - Realistic 3D depth perspective (foreground spheres grow larger, background spheres shrink)
    /// - Dynamic Z-layer sorting so foreground spheres pass cleanly in front
    /// - Motion tilt & organic floating wave breathing
    /// - Soft pulsing colorful glow halos behind each sphere
    /// - Sequential specular lens flare star twinkles
    /// - Interactive drag / swipe support: player can spin the carousel with inertia!
    /// - Interactive tap reaction: spring jelly jiggle, stardust burst & pickup sound
    /// - Automatic skin synchronization with SkinManager
    /// </summary>
    public class HomeSpheresAnimator : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private class SphereItem
        {
            public int index;
            public GameObject rootObj;
            public RectTransform rootRt;
            public Color baseColor;
            public float baseAngleDeg;

            // Glow Aura
            public GameObject glowObj;
            public RectTransform glowRt;
            public Image glowImg;

            // Main Sphere Body
            public GameObject sphereObj;
            public RectTransform sphereRt;
            public Image sphereImg;
            public Button button;

            // Specular Flare Star
            public GameObject glintObj;
            public RectTransform glintRt;
            public Image glintImg;

            // Animation States
            public float tapTimer = -1f;
            public float glintTimer = -1f;
            public float currentDepth = 0f;
        }

        private readonly List<SphereItem> m_Spheres = new List<SphereItem>();
        private RectTransform m_ClusterRt;

        // Base Center Position (Symmetrically centered between Logo and Buttons)
        private float m_BaseCenterY = 0f;
        private bool m_HasBaseCenterY = false;

        // Orbit Parameters
        private float m_RadiusX = 136f;
        private float m_RadiusY = 88f;
        private float m_BaseOrbitSpeed = 30f; // Degrees per second (smooth, majestic continuous rotation)
        private float m_CurrentOrbitAngle = 0f;

        // Drag & Inertia Physics
        private bool m_IsDragging = false;
        private float m_DragVelocity = 0f;
        private float m_LastDragTime = 0f;
        private float m_LastDragDeltaX = 0f;

        // Periodic Specular Glint Twinkle
        private float m_GlintInterval = 1.35f;
        private float m_GlintTimer = 0f;
        private int m_GlintSphereIndex = 0;

        // Center Ambient Core Glow
        private Image m_CenterCoreGlow;

        // UI Sparkle Particle Pool for Tap Reactions
        private class UISparkle
        {
            public GameObject obj;
            public RectTransform rt;
            public Image img;
            public Vector2 velocity;
            public float life;
            public float maxLife;
            public float rotSpeed;
            public float baseScale;
            public bool active;
        }

        private readonly List<UISparkle> m_SparklePool = new List<UISparkle>(24);
        private Transform m_SparkleContainer;

        private bool m_IsInitialized = false;

        private void Awake()
        {
            if (!m_IsInitialized)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            if (m_IsInitialized)
            {
                RefreshSpheresSkin();
            }
            SkinManager.OnSkinChanged += HandleSkinChanged;
        }

        private void OnDisable()
        {
            SkinManager.OnSkinChanged -= HandleSkinChanged;
        }

        private void OnDestroy()
        {
            SkinManager.OnSkinChanged -= HandleSkinChanged;
        }

        private void HandleSkinChanged(BallSkinType newSkin)
        {
            RefreshSpheresSkin();
        }

        public void RefreshSpheresSkin()
        {
            for (int i = 0; i < m_Spheres.Count; i++)
            {
                if (m_Spheres[i].sphereImg != null)
                {
                    m_Spheres[i].sphereImg.sprite = SpriteFactory.GetBlockSprite(m_Spheres[i].baseColor);
                }
            }
        }

        /// <summary>
        /// Explicitly sets the base center Y position for the cluster.
        /// </summary>
        public void SetBaseCenterY(float y)
        {
            m_BaseCenterY = y;
            m_HasBaseCenterY = true;
            if (m_ClusterRt != null)
            {
                m_ClusterRt.anchoredPosition = new Vector2(0f, m_BaseCenterY);
            }
        }

        /// <summary>
        /// Builds and initializes the 6 animated spheres revolving in an orbital ring.
        /// </summary>
        public void Initialize()
        {
            if (m_IsInitialized) return;
            m_IsInitialized = true;

            m_ClusterRt = GetComponent<RectTransform>();
            if (m_ClusterRt == null)
            {
                m_ClusterRt = gameObject.AddComponent<RectTransform>();
            }

            if (!m_HasBaseCenterY && m_ClusterRt != null)
            {
                m_BaseCenterY = m_ClusterRt.anchoredPosition.y;
                m_HasBaseCenterY = true;
            }

            // Ensure cluster has raycast background for drag detection
            Image clusterRaycast = GetComponent<Image>();
            if (clusterRaycast == null)
            {
                clusterRaycast = gameObject.AddComponent<Image>();
            }
            clusterRaycast.color = Color.clear;
            clusterRaycast.raycastTarget = true;

            // Clean any existing children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            m_Spheres.Clear();

            // 1. Soft Ambient Center Core Glow (Celestial focal point inside the ring)
            GameObject centerCoreObj = new GameObject("CenterAmbientCore");
            centerCoreObj.transform.SetParent(transform, false);
            RectTransform coreRt = centerCoreObj.AddComponent<RectTransform>();
            coreRt.anchoredPosition = Vector2.zero;
            coreRt.sizeDelta = new Vector2(210f, 210f);
            m_CenterCoreGlow = centerCoreObj.AddComponent<Image>();
            m_CenterCoreGlow.sprite = SpriteFactory.GetSoftGlowOrbSprite();
            m_CenterCoreGlow.color = new Color(0.35f, 0.75f, 1.0f, 0.16f);
            m_CenterCoreGlow.raycastTarget = false;

            // 2. 6 Vibrant Jewel Sphere Colors (Full spectrum rainbow)
            Color[] colors = new Color[]
            {
                SpriteFactory.BlockColors[0], // Ruby Red
                SpriteFactory.BlockColors[1], // Sunset Orange
                SpriteFactory.BlockColors[2], // Golden Yellow
                SpriteFactory.BlockColors[3], // Emerald Green
                SpriteFactory.BlockColors[4], // Crystal Cyan
                SpriteFactory.BlockColors[6]  // Amethyst Purple
            };

            int count = colors.Length; // 6 spheres in the revolving ring

            for (int s = 0; s < count; s++)
            {
                SphereItem item = new SphereItem();
                item.index = s;
                item.baseColor = colors[s];
                item.baseAngleDeg = s * (360f / count);

                // Root Container for this sphere
                GameObject root = new GameObject($"OrbitalSphereRoot_{s}");
                root.transform.SetParent(transform, false);
                item.rootObj = root;
                item.rootRt = root.AddComponent<RectTransform>();
                item.rootRt.sizeDelta = new Vector2(88f, 88f);

                // Soft Glowing Aura Halo (Behind sphere)
                GameObject glow = new GameObject($"GlowAura_{s}");
                glow.transform.SetParent(root.transform, false);
                item.glowObj = glow;
                item.glowRt = glow.AddComponent<RectTransform>();
                item.glowRt.anchoredPosition = Vector2.zero;
                item.glowRt.sizeDelta = new Vector2(136f, 136f);
                item.glowImg = glow.AddComponent<Image>();
                item.glowImg.sprite = SpriteFactory.GetSoftGlowOrbSprite();
                Color auraCol = Color.Lerp(item.baseColor, Color.white, 0.20f);
                auraCol.a = 0.32f;
                item.glowImg.color = auraCol;
                item.glowImg.raycastTarget = false;

                // Main 3D Glossy Sphere Body
                GameObject sphere = new GameObject($"SphereBody_{s}");
                sphere.transform.SetParent(root.transform, false);
                item.sphereObj = sphere;
                item.sphereRt = sphere.AddComponent<RectTransform>();
                item.sphereRt.anchoredPosition = Vector2.zero;
                item.sphereRt.sizeDelta = new Vector2(82f, 82f);
                item.sphereImg = sphere.AddComponent<Image>();
                item.sphereImg.sprite = SpriteFactory.GetBlockSprite(item.baseColor);
                item.sphereImg.raycastTarget = true;

                // Tap / Click Event for bouncy jiggle & sparkle burst
                item.button = sphere.AddComponent<Button>();
                item.button.transition = Selectable.Transition.None;
                int capturedIndex = s;
                item.button.onClick.AddListener(() => OnSphereTapped(capturedIndex));

                // Specular Lens Flare Star (Twinkling Highlight Glint)
                GameObject glint = new GameObject($"SpecularGlint_{s}");
                glint.transform.SetParent(sphere.transform, false);
                item.glintObj = glint;
                item.glintRt = glint.AddComponent<RectTransform>();
                item.glintRt.anchoredPosition = new Vector2(-18f, 19f);
                item.glintRt.sizeDelta = new Vector2(52f, 52f);
                item.glintRt.localScale = Vector3.zero;
                item.glintImg = glint.AddComponent<Image>();
                item.glintImg.sprite = SpriteFactory.GetLensFlareStarSprite();
                item.glintImg.color = new Color(1f, 1f, 1f, 0.95f);
                item.glintImg.raycastTarget = false;

                m_Spheres.Add(item);
            }

            // 3. Sparkle Particle Container (Placed in front)
            GameObject spkContainerObj = new GameObject("SparkleParticleContainer");
            spkContainerObj.transform.SetParent(transform, false);
            m_SparkleContainer = spkContainerObj.transform;
            CreateSparklePool(20);
        }

        private void CreateSparklePool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject spk = new GameObject($"UISparkle_{i}");
                spk.transform.SetParent(m_SparkleContainer, false);
                RectTransform rt = spk.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(28f, 28f);
                Image img = spk.AddComponent<Image>();
                img.sprite = SpriteFactory.GetSparkleShardSprite();
                img.raycastTarget = false;
                spk.SetActive(false);

                UISparkle item = new UISparkle
                {
                    obj = spk,
                    rt = rt,
                    img = img,
                    active = false
                };
                m_SparklePool.Add(item);
            }
        }

        private void Update()
        {
            if (!m_IsInitialized || m_Spheres.Count == 0) return;

            float dt = Time.deltaTime;
            float t = Time.time;

            // 1. Orbital Angle Progression
            if (!m_IsDragging)
            {
                // Smooth deceleration of drag velocity or default continuous revolution
                if (Mathf.Abs(m_DragVelocity) > 0.1f)
                {
                    m_CurrentOrbitAngle += m_DragVelocity * dt;
                    m_DragVelocity = Mathf.Lerp(m_DragVelocity, 0f, dt * 2.8f);
                }
                else
                {
                    m_CurrentOrbitAngle += m_BaseOrbitSpeed * dt;
                }
            }

            // Keep angle normalized within 0..360
            m_CurrentOrbitAngle = (m_CurrentOrbitAngle % 360f + 360f) % 360f;

            // 2. Subtle Overall Hover
            if (m_ClusterRt != null)
            {
                float clusterHover = Mathf.Sin(t * 1.6f) * 3.5f;
                m_ClusterRt.anchoredPosition = new Vector2(0f, m_BaseCenterY + clusterHover);
            }

            // 3. Center Core Breathing Glow
            if (m_CenterCoreGlow != null)
            {
                float corePulse = Mathf.Sin(t * 2.2f);
                Color cc = m_CenterCoreGlow.color;
                cc.a = Mathf.Lerp(0.12f, 0.22f, (corePulse + 1f) * 0.5f);
                m_CenterCoreGlow.color = cc;
            }

            // 4. Periodic Specular Glint Twinkle Timer
            m_GlintTimer += dt;
            if (m_GlintTimer >= m_GlintInterval)
            {
                m_GlintTimer = 0f;
                TriggerNextGlint();
            }

            // 5. Update Each Sphere's 3D Orbit Position, Scale, and Tilt
            for (int i = 0; i < m_Spheres.Count; i++)
            {
                UpdateSphereOrbitalPosition(m_Spheres[i], t, dt);
            }

            // 6. 3D Depth Sibling Sorting (Foreground spheres render over background spheres!)
            SortSpheresByDepth();

            // 7. Update Sparkle Particles
            UpdateSparkles(dt);
        }

        private void UpdateSphereOrbitalPosition(SphereItem item, float t, float dt)
        {
            // Current angle along the circular/elliptical orbit
            float currentAngleDeg = (m_CurrentOrbitAngle + item.baseAngleDeg) % 360f;
            float rad = currentAngleDeg * Mathf.Deg2Rad;

            // Elliptical coordinates
            float orbitX = Mathf.Cos(rad) * m_RadiusX;
            float orbitY = Mathf.Sin(rad) * m_RadiusY;

            // Organic vertical wave breathing (subtle floating undulation)
            float waveY = Mathf.Sin(t * 2.5f + item.index * 1.05f) * 4.5f;
            float finalY = orbitY + waveY;

            // 3D Depth: sin(rad) is -1 at bottom (front/closest) and +1 at top (back/furthest)
            // depthNormalized: 0 = front (closest), 1 = back (furthest)
            float depthFactor = (Mathf.Sin(rad) + 1f) * 0.5f;
            item.currentDepth = finalY; // Used for sibling sorting

            // Perspective scale: Larger at front (1.12), smaller at back (0.88)
            float depthScale = Mathf.Lerp(1.12f, 0.88f, depthFactor);

            // Motion-based tilt sway: sphere leans gently in direction of orbital movement
            float tiltAngle = Mathf.Cos(rad) * -7.5f;

            // Tap Spring Jiggle Physics
            float tapScaleX = 1f;
            float tapScaleY = 1f;
            if (item.tapTimer >= 0f)
            {
                item.tapTimer += dt;
                float tapDuration = 0.65f;
                if (item.tapTimer < tapDuration)
                {
                    float decay = Mathf.Exp(-item.tapTimer * 5.8f);
                    float oscillation = Mathf.Sin(item.tapTimer * 26f) * decay * 0.38f;
                    tapScaleX = 1f + oscillation;
                    tapScaleY = 1f - oscillation * 0.82f;
                }
                else
                {
                    item.tapTimer = -1f;
                }
            }

            // Apply position & transform
            item.rootRt.anchoredPosition = new Vector2(orbitX, finalY);

            Vector3 finalScale = new Vector3(
                depthScale * tapScaleX,
                depthScale * tapScaleY,
                1f
            );
            item.sphereRt.localScale = finalScale;
            item.sphereRt.localRotation = Quaternion.Euler(0f, 0f, tiltAngle);

            // Update Glow Halo (Vibrant at front, soft at back)
            if (item.glowImg != null)
            {
                float glowAlpha = Mathf.Lerp(0.42f, 0.20f, 1f - depthFactor);
                Color gc = item.glowImg.color;
                gc.a = glowAlpha;
                item.glowImg.color = gc;
                item.glowRt.localScale = Vector3.one * (1.0f + Mathf.Sin(t * 2.2f + item.index) * 0.08f);
            }

            // Update Specular Glint Twinkle
            if (item.glintRt != null)
            {
                if (item.glintTimer >= 0f)
                {
                    item.glintTimer += dt;
                    float glintDuration = 0.52f;
                    float p = item.glintTimer / glintDuration;

                    if (p < 1f)
                    {
                        float bell = Mathf.Sin(p * Mathf.PI);
                        float s = Mathf.Pow(bell, 0.7f) * 1.25f;
                        item.glintRt.localScale = new Vector3(s, s, 1f);
                        item.glintRt.localRotation = Quaternion.Euler(0f, 0f, p * 90f);

                        Color glCol = item.glintImg.color;
                        glCol.a = Mathf.Clamp01(bell * 1.15f);
                        item.glintImg.color = glCol;
                    }
                    else
                    {
                        item.glintTimer = -1f;
                        item.glintRt.localScale = Vector3.zero;
                    }
                }
                else
                {
                    item.glintRt.localScale = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// Dynamically orders siblings so spheres at the front (lowest Y) render in front of background spheres.
        /// </summary>
        private void SortSpheresByDepth()
        {
            // Sibling index: Lower sibling = rendered first (behind). Higher sibling = rendered last (on top).
            // Back spheres have highest Y -> need lowest sibling index.
            // Front spheres have lowest Y -> need highest sibling index.
            List<SphereItem> sorted = new List<SphereItem>(m_Spheres);
            sorted.Sort((a, b) => b.currentDepth.CompareTo(a.currentDepth)); // Highest Y first

            // The center glow is at sibling 0 (always in background)
            if (m_CenterCoreGlow != null)
            {
                m_CenterCoreGlow.transform.SetSiblingIndex(0);
            }

            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].rootObj.transform.SetSiblingIndex(i + 1);
            }

            // Sparkle container always on top
            if (m_SparkleContainer != null)
            {
                m_SparkleContainer.SetAsLastSibling();
            }
        }

        private void TriggerNextGlint()
        {
            if (m_Spheres.Count == 0) return;

            // Trigger glint on sphere nearest to front or sequential
            SphereItem item = m_Spheres[m_GlintSphereIndex % m_Spheres.Count];
            m_GlintSphereIndex++;
            item.glintTimer = 0f;
        }

        /// <summary>
        /// Interactive tap: bouncy spring jelly jiggle, stardust burst, tone SFX, and spin boost!
        /// </summary>
        public void OnSphereTapped(int index)
        {
            if (index < 0 || index >= m_Spheres.Count) return;

            SphereItem item = m_Spheres[index];
            item.tapTimer = 0f;
            item.glintTimer = 0f;

            // Playful spin boost on tap!
            m_DragVelocity = m_BaseOrbitSpeed * 2.8f;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPickUp();
            }

            Vector2 burstOrigin = item.rootRt.anchoredPosition;
            SpawnSparkleBurst(burstOrigin, item.baseColor, 7);
        }

        // --- Drag / Swipe Carousel Spin Support ---
        public void OnBeginDrag(PointerEventData eventData)
        {
            m_IsDragging = true;
            m_DragVelocity = 0f;
            m_LastDragTime = Time.time;
            m_LastDragDeltaX = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            float dt = Time.time - m_LastDragTime;
            if (dt > 0.0001f)
            {
                m_LastDragDeltaX = eventData.delta.x / dt;
            }
            m_LastDragTime = Time.time;

            // Rotate carousel proportionally to drag
            m_CurrentOrbitAngle += eventData.delta.x * 0.40f;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            m_IsDragging = false;
            // Transfer drag momentum into inertia spin
            m_DragVelocity = Mathf.Clamp(m_LastDragDeltaX * 0.35f, -280f, 280f);
            if (Mathf.Abs(m_DragVelocity) < 15f)
            {
                m_DragVelocity = 0f;
            }
        }

        private void SpawnSparkleBurst(Vector2 origin, Color baseColor, int count)
        {
            Color lightCol = Color.Lerp(baseColor, Color.white, 0.45f);

            for (int i = 0; i < count; i++)
            {
                UISparkle spk = GetAvailableSparkle();
                if (spk == null) break;

                float angle = (i / (float)count) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.25f, 0.25f);
                float speed = UnityEngine.Random.Range(140f, 260f);
                spk.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                spk.life = 0f;
                spk.maxLife = UnityEngine.Random.Range(0.42f, 0.65f);
                spk.rotSpeed = UnityEngine.Random.Range(-380f, 380f);
                spk.baseScale = UnityEngine.Random.Range(0.7f, 1.25f);

                spk.rt.anchoredPosition = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 16f;
                spk.rt.localScale = Vector3.one * spk.baseScale;
                spk.img.color = lightCol;
                spk.obj.SetActive(true);
                spk.active = true;
            }
        }

        private UISparkle GetAvailableSparkle()
        {
            for (int i = 0; i < m_SparklePool.Count; i++)
            {
                if (!m_SparklePool[i].active) return m_SparklePool[i];
            }

            GameObject spk = new GameObject($"UISparkle_{m_SparklePool.Count}");
            spk.transform.SetParent(m_SparkleContainer, false);
            RectTransform rt = spk.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(28f, 28f);
            Image img = spk.AddComponent<Image>();
            img.sprite = SpriteFactory.GetSparkleShardSprite();
            img.raycastTarget = false;
            spk.SetActive(false);

            UISparkle item = new UISparkle
            {
                obj = spk,
                rt = rt,
                img = img,
                active = false
            };
            m_SparklePool.Add(item);
            return item;
        }

        private void UpdateSparkles(float dt)
        {
            for (int i = 0; i < m_SparklePool.Count; i++)
            {
                UISparkle s = m_SparklePool[i];
                if (!s.active) continue;

                s.life += dt;
                float progress = s.life / s.maxLife;

                if (progress >= 1f)
                {
                    s.active = false;
                    s.obj.SetActive(false);
                    continue;
                }

                s.velocity.y += 65f * dt;
                s.velocity *= Mathf.Clamp01(1f - dt * 3.2f);
                s.rt.anchoredPosition += s.velocity * dt;

                s.rt.Rotate(0f, 0f, s.rotSpeed * dt);

                float alpha = Mathf.Clamp01(1f - progress);
                Color c = s.img.color;
                c.a = alpha;
                s.img.color = c;

                float scale = s.baseScale * (1f - progress * 0.45f);
                s.rt.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
