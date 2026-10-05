using System.Collections;
using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// Represents a single slot in the 9x9 Grid.
    /// Manages circular socket, 3D sphere state, squash-and-stretch placement bounce, 
    /// idle breathing pulse, and bubble blast pop animations.
    /// </summary>
    public class GridCell : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool IsOccupied { get; private set; }
        public Color CurrentColor { get; private set; }

        private SpriteRenderer m_BgRenderer;
        private SpriteRenderer m_BlockRenderer;
        private SpriteRenderer m_PreviewRenderer;
        private SpriteRenderer m_LineGlowRenderer;

        private GameObject m_BlockObj;
        private GameObject m_PreviewObj;
        private GameObject m_LineGlowObj;
        private float m_CellSize;

        private Coroutine m_IdleCoroutine;
        private Coroutine m_LineGlowCoroutine;

        public void Initialize(int x, int y, float cellSize)
        {
            GridX = x;
            GridY = y;
            m_CellSize = cellSize;
            IsOccupied = false;

            // Background Circular Socket Slot
            m_BgRenderer = gameObject.AddComponent<SpriteRenderer>();
            m_BgRenderer.sprite = SpriteFactory.GetEmptyCellSprite();
            m_BgRenderer.sortingOrder = 1;
            transform.localScale = Vector3.one * (cellSize * 0.98f);

            // Preview Ghost Sphere
            m_PreviewObj = new GameObject($"Preview_{x}_{y}");
            m_PreviewObj.transform.SetParent(transform, false);
            m_PreviewRenderer = m_PreviewObj.AddComponent<SpriteRenderer>();
            m_PreviewRenderer.sortingOrder = 3;
            m_PreviewObj.SetActive(false);

            // Line Clear Anticipation Highlight
            m_LineGlowObj = new GameObject($"LineGlow_{x}_{y}");
            m_LineGlowObj.transform.SetParent(transform, false);
            m_LineGlowRenderer = m_LineGlowObj.AddComponent<SpriteRenderer>();
            m_LineGlowRenderer.sprite = SpriteFactory.GetLineHighlightSprite();
            m_LineGlowRenderer.sortingOrder = 5;
            m_LineGlowObj.SetActive(false);

            // Filled 3D Sphere Ball
            m_BlockObj = new GameObject($"Sphere_{x}_{y}");
            m_BlockObj.transform.SetParent(transform, false);
            m_BlockRenderer = m_BlockObj.AddComponent<SpriteRenderer>();
            m_BlockRenderer.sortingOrder = 4;
            m_BlockObj.SetActive(false);

            // Embedded Sparkling Gem inside Sphere
            m_GemObj = new GameObject($"Gem_{x}_{y}");
            m_GemObj.transform.SetParent(m_BlockObj.transform, false);
            m_GemRenderer = m_GemObj.AddComponent<SpriteRenderer>();
            m_GemRenderer.sortingOrder = 6; // Above sphere body (4)
            m_GemObj.transform.localScale = Vector3.one * 0.58f;
            m_GemObj.SetActive(false);
        }

        public GemType EmbeddedGem { get; private set; } = GemType.None;
        private GameObject m_GemObj;
        private SpriteRenderer m_GemRenderer;

        public void SetLineClearHighlight(bool active)
        {
            if (m_LineGlowObj == null) return;

            if (active)
            {
                m_LineGlowObj.SetActive(true);
                if (m_LineGlowCoroutine == null && gameObject.activeInHierarchy)
                {
                    m_LineGlowCoroutine = StartCoroutine(AnimateLineGlowPulse());
                }
            }
            else
            {
                if (m_LineGlowCoroutine != null)
                {
                    StopCoroutine(m_LineGlowCoroutine);
                    m_LineGlowCoroutine = null;
                }
                m_LineGlowObj.SetActive(false);
                m_LineGlowObj.transform.localScale = Vector3.one;

                if (IsOccupied && m_BlockObj != null)
                {
                    m_BlockObj.transform.localScale = Vector3.one;
                }
            }
        }

        private IEnumerator AnimateLineGlowPulse()
        {
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.unscaledDeltaTime * 6.5f;
                float pulse = 1.0f + Mathf.Sin(elapsed) * 0.08f;
                if (m_LineGlowObj != null) m_LineGlowObj.transform.localScale = Vector3.one * pulse;
                if (IsOccupied && m_BlockObj != null) m_BlockObj.transform.localScale = Vector3.one * (1.0f + Mathf.Sin(elapsed) * 0.06f);
                yield return null;
            }
        }

        public void SetPreview(bool show, Color color = default)
        {
            if (IsOccupied) return;

            if (show)
            {
                m_PreviewRenderer.sprite = SpriteFactory.GetPreviewCellSprite(color);
                m_PreviewObj.SetActive(true);
            }
            else
            {
                m_PreviewObj.SetActive(false);
            }
        }

        public void Fill(Color color, bool animate = true)
        {
            Fill(color, GemType.None, animate);
        }

        public void Fill(Color color, GemType gem, bool animate = true)
        {
            IsOccupied = true;
            CurrentColor = color;
            EmbeddedGem = gem;
            SetPreview(false);

            m_BlockRenderer.sprite = SpriteFactory.GetBlockSprite(color);
            m_BlockObj.SetActive(true);

            if (gem != GemType.None && m_GemRenderer != null)
            {
                m_GemRenderer.sprite = GemIconFactory.GetGemSprite(gem);
                m_GemObj.SetActive(true);
            }
            else if (m_GemObj != null)
            {
                m_GemObj.SetActive(false);
            }

            StopAllCoroutines();

            if (animate)
            {
                StartCoroutine(AnimatePlaceSquash());
            }
            else
            {
                m_BlockObj.transform.localScale = Vector3.one;
                m_IdleCoroutine = StartCoroutine(AnimateIdleBreathing());
            }
        }

        public void RefreshBlockSprite()
        {
            if (IsOccupied && m_BlockRenderer != null)
            {
                m_BlockRenderer.sprite = SpriteFactory.GetBlockSprite(CurrentColor);
                if (EmbeddedGem != GemType.None && m_GemRenderer != null)
                {
                    m_GemRenderer.sprite = GemIconFactory.GetGemSprite(EmbeddedGem);
                }
            }
        }

        public void Clear(bool animate = true, float delay = 0f, bool harvestGem = true)
        {
            if (!IsOccupied) return;
            IsOccupied = false;

            // Harvest embedded gemstone towards adventure level objectives ONLY on real gameplay line clears!
            if (harvestGem && animate && EmbeddedGem != GemType.None)
            {
                if (LevelManager.Instance != null && LevelManager.Instance.IsLevelActive)
                {
                    LevelManager.Instance.OnGemCollected(EmbeddedGem, transform.position);
                }
            }

            EmbeddedGem = GemType.None;
            if (m_GemObj != null) m_GemObj.SetActive(false);

            StopAllCoroutines();

            if (animate)
            {
                StartCoroutine(AnimateBubbleBlast(delay));
            }
            else
            {
                m_BlockObj.SetActive(false);
            }
        }

        /// <summary>
        /// Juicy squash & stretch drop animation for spheres.
        /// </summary>
        private IEnumerator AnimatePlaceSquash()
        {
            Transform t = m_BlockObj.transform;
            t.localScale = Vector3.zero;

            float elapsed = 0f;
            float popDuration = 0.12f;

            // 1. Pop In
            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / popDuration;
                float s = Mathf.Sin(progress * Mathf.PI * 0.5f);
                t.localScale = new Vector3(s * 1.15f, s * 0.85f, 1f); // horizontal squash on impact
                yield return null;
            }

            // 2. Rebound Stretch
            elapsed = 0f;
            float reboundDuration = 0.10f;
            while (elapsed < reboundDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / reboundDuration;
                float sx = Mathf.Lerp(1.15f, 0.94f, progress);
                float sy = Mathf.Lerp(0.85f, 1.08f, progress);
                t.localScale = new Vector3(sx, sy, 1f);
                yield return null;
            }

            // 3. Settle to Round 1.0
            elapsed = 0f;
            float settleDuration = 0.08f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / settleDuration;
                float sx = Mathf.Lerp(0.94f, 1.0f, progress);
                float sy = Mathf.Lerp(1.08f, 1.0f, progress);
                t.localScale = new Vector3(sx, sy, 1f);
                yield return null;
            }

            t.localScale = Vector3.one;
            m_IdleCoroutine = StartCoroutine(AnimateIdleBreathing());
        }

        /// <summary>
        /// Exploding bubble / energy pop blast animation.
        /// </summary>
        private IEnumerator AnimateBubbleBlast(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            Transform t = m_BlockObj.transform;
            Color origColor = m_BlockRenderer.color;

            // Flash sphere white/bright
            m_BlockRenderer.color = Color.white;

            float elapsed = 0f;
            float duration = 0.24f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / duration;

                // Expand outward like an exploding bubble, then pop away
                float scale;
                if (p < 0.28f)
                {
                    scale = Mathf.Lerp(1.0f, 1.35f, p / 0.28f);
                }
                else
                {
                    scale = Mathf.Lerp(1.35f, 0f, (p - 0.28f) / 0.72f);
                }

                t.localScale = Vector3.one * scale;

                Color c = Color.Lerp(Color.white, origColor, p);
                c.a = Mathf.Clamp01(1f - p * 1.2f);
                m_BlockRenderer.color = c;

                yield return null;
            }

            m_BlockRenderer.color = origColor;
            t.localScale = Vector3.one;
            m_BlockObj.SetActive(false);
        }

        /// <summary>
        /// Gentle organic breathing/pulse animation so spheres look alive on the board.
        /// </summary>
        private IEnumerator AnimateIdleBreathing()
        {
            Transform t = m_BlockObj.transform;
            float phaseOffset = (GridX + GridY) * 0.45f;

            while (IsOccupied)
            {
                float breathe = Mathf.Sin(Time.time * 2.8f + phaseOffset) * 0.035f;
                t.localScale = new Vector3(1f + breathe, 1f - breathe * 0.5f, 1f);
                yield return null;
            }

            t.localScale = Vector3.one;
        }
    }
}
