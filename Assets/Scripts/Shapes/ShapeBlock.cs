using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// An individual block unit that constitutes a piece of a DraggableShape.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ShapeBlock : MonoBehaviour
    {
        public Vector2Int LocalCoordinate { get; private set; }
        public GemType EmbeddedGem { get; private set; } = GemType.None;
        private SpriteRenderer m_SpriteRenderer;
        private GameObject m_GemObj;
        private SpriteRenderer m_GemRenderer;

        private void Awake()
        {
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
        }

        private Color m_CurrentColor;

        public void Initialize(Vector2Int localCoord, Color color, float blockSize, GemType gem = GemType.None)
        {
            LocalCoordinate = localCoord;
            m_CurrentColor = color;
            EmbeddedGem = gem;
            if (m_SpriteRenderer == null)
            {
                m_SpriteRenderer = GetComponent<SpriteRenderer>();
            }

            m_SpriteRenderer.sprite = SpriteFactory.GetBlockSprite(color);
            m_SpriteRenderer.sortingOrder = 10; // Render above grid
            transform.localScale = Vector3.one * blockSize;

            EnsureGemObject();
            SetGem(gem);
        }

        private void EnsureGemObject()
        {
            if (m_GemObj == null)
            {
                m_GemObj = new GameObject("BlockGem");
                m_GemObj.transform.SetParent(transform, false);
                m_GemRenderer = m_GemObj.AddComponent<SpriteRenderer>();
                m_GemRenderer.sortingOrder = 12; // Above block sphere
                m_GemObj.transform.localScale = Vector3.one * 0.58f;
                m_GemObj.SetActive(false);
            }
        }

        public void SetGem(GemType gem)
        {
            EmbeddedGem = gem;
            EnsureGemObject();
            if (gem != GemType.None)
            {
                m_GemRenderer.sprite = GemIconFactory.GetGemSprite(gem);
                m_GemObj.SetActive(true);
            }
            else
            {
                m_GemObj.SetActive(false);
            }
        }

        public void RefreshSprite()
        {
            if (m_SpriteRenderer != null)
            {
                m_SpriteRenderer.sprite = SpriteFactory.GetBlockSprite(m_CurrentColor);
            }
            if (EmbeddedGem != GemType.None && m_GemRenderer != null)
            {
                m_GemRenderer.sprite = GemIconFactory.GetGemSprite(EmbeddedGem);
            }
        }

        public void SetSortingOrder(int order)
        {
            if (m_SpriteRenderer != null)
            {
                m_SpriteRenderer.sortingOrder = order;
            }
            if (m_GemRenderer != null)
            {
                m_GemRenderer.sortingOrder = order + 2;
            }
        }
    }
}
