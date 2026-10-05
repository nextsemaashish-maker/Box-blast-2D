using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// Handles user interaction, smooth dragging, adaptive finger-offset, 
    /// organic tilt physics, ghost previewing on the grid, and spring return for a shape.
    /// Optimized for 60Hz/90Hz/120Hz high-refresh mobile gameplay.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class DraggableShape : MonoBehaviour
    {
        public static DraggableShape ActiveDragShape { get; private set; }

        public ShapeData Data { get; private set; }
        public Vector3 SlotPosition { get; private set; }
        public int SlotIndex { get; set; } = 0;
        public bool IsDragging { get; private set; }

        [Header("Drag Settings")]
        [SerializeField] private float m_SlotScale = 0.52f;
        [SerializeField] private float m_BoardScale = 1.0f;
        [SerializeField] private float m_DragYOffset = 1.35f;

        private readonly List<ShapeBlock> m_Blocks = new List<ShapeBlock>();
        private BoxCollider2D m_Collider;
        private Camera m_MainCamera;
        private Coroutine m_ReturnCoroutine;

        // Smooth physics & dynamics
        private float m_CurrentLiftOffset = 0f;
        private float m_CurrentScale = 0.52f;
        private float m_CurrentZAngle = 0f;
        private Vector3 m_LastPos;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ActiveDragShape = null;
        }

        private void Awake()
        {
            m_MainCamera = Camera.main;
            m_Collider = GetComponent<BoxCollider2D>();
        }

        public void Initialize(ShapeData data, Vector3 slotPos, float cellSize, int slotIndex = 0, List<GemType> gems = null)
        {
            Data = data;
            SlotPosition = slotPos;
            SlotIndex = slotIndex;
            transform.position = slotPos;
            transform.localScale = Vector3.one * m_SlotScale;
            m_CurrentScale = m_SlotScale;
            m_LastPos = slotPos;

            if (m_MainCamera == null) m_MainCamera = Camera.main;
            if (m_Collider == null) m_Collider = GetComponent<BoxCollider2D>();

            // Instantiate block pieces
            Vector2 centerOffset = data.GetCenterOffset() * cellSize;

            for (int i = 0; i < data.Blocks.Length; i++)
            {
                Vector2Int coord = data.Blocks[i];
                GameObject blockObj = new GameObject($"Block_{coord.x}_{coord.y}");
                blockObj.transform.SetParent(transform, false);

                Vector3 localPos = new Vector3(coord.x * cellSize - centerOffset.x, coord.y * cellSize - centerOffset.y, 0f);
                blockObj.transform.localPosition = localPos;

                ShapeBlock block = blockObj.AddComponent<ShapeBlock>();
                GemType gem = (gems != null && i < gems.Count) ? gems[i] : GemType.None;
                block.Initialize(coord, data.BlockColor, cellSize, gem);
                m_Blocks.Add(block);
            }

            // Configure generous Collider size for mobile touch/click detection
            // Note: In local space, scale is m_SlotScale (0.52f).
            // A minimum world touch target of 2.3 units ensures effortless pickup on any mobile touch screen!
            float minWorldSize = 2.3f;
            float colWidth = Mathf.Max(data.Width * cellSize, minWorldSize) / m_SlotScale;
            float colHeight = Mathf.Max(data.Height * cellSize, minWorldSize) / m_SlotScale;
            m_Collider.size = new Vector2(colWidth, colHeight);
            m_Collider.offset = Vector2.zero;
            m_Collider.isTrigger = true;
        }

        public void UpdateSlotPosition(Vector3 newSlotPos)
        {
            SlotPosition = newSlotPos;
            if (!IsDragging)
            {
                transform.position = newSlotPos;
                m_LastPos = newSlotPos;
            }
        }

        public void RefreshSprites()
        {
            for (int i = 0; i < m_Blocks.Count; i++)
            {
                if (m_Blocks[i] != null)
                {
                    m_Blocks[i].RefreshSprite();
                }
            }
        }

        private void OnDestroy()
        {
            if (ActiveDragShape == this) ActiveDragShape = null;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                return;
            }

            // Do not drag shape if player is aiming a booster
            if (GridManager.Instance != null && GridManager.Instance.ActiveBooster != BoosterType.None)
            {
                return;
            }

            HandleInput();
        }

        private void HandleInput()
        {
            if (m_MainCamera == null) m_MainCamera = Camera.main;
            if (m_MainCamera == null) return;

            Vector2 pointerScreen = InputHelper.GetPointerScreenPosition();
            Vector3 pointerWorld = m_MainCamera.ScreenToWorldPoint(new Vector3(pointerScreen.x, pointerScreen.y, -m_MainCamera.transform.position.z));
            pointerWorld.z = 0f;

            // Start Drag: Only if no other shape is currently being dragged
            if (!IsDragging && ActiveDragShape == null && InputHelper.IsPointerDown())
            {
                if (InputHelper.IsPointerOverUI()) return;

                // Effortless touch pickup: Check collider overlap OR proximity to slot box center
                bool canPickUp = m_Collider.OverlapPoint(pointerWorld) || (Vector2.Distance(pointerWorld, SlotPosition) < 1.35f);

                if (canPickUp)
                {
                    StartDrag(pointerWorld);
                }
            }
            // Continue Drag
            else if (IsDragging && ActiveDragShape == this && InputHelper.IsPointerPressed())
            {
                UpdateDrag(pointerWorld);
            }
            // End Drag or Touch Cancelled
            else if (IsDragging && ActiveDragShape == this && (InputHelper.IsPointerUp() || !InputHelper.IsPointerPressed()))
            {
                EndDrag(pointerWorld);
            }
        }

        private void StartDrag(Vector3 pointerWorld)
        {
            if (m_ReturnCoroutine != null)
            {
                StopCoroutine(m_ReturnCoroutine);
            }

            ActiveDragShape = this;
            IsDragging = true;
            m_CurrentLiftOffset = 0f; // Smoothly glides up on pickup
            m_CurrentScale = m_SlotScale;
            m_LastPos = transform.position;
            SetBlocksSortingOrder(20);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPickUp();
            }

            HapticManager.TriggerLight();
            UpdateDrag(pointerWorld);
        }

        private float GetTargetDragOffsetY()
        {
            if (InputHelper.IsMobileTouchActive())
            {
                // Dynamic thumb clearance: taller pieces get more vertical lift so thumb never obscures lower blocks
                float halfHeight = (Data != null ? Data.Height : 1) * 0.9f * 0.5f;
                return Mathf.Max(1.4f, halfHeight + 0.65f);
            }
            // On desktop mouse, center slightly above cursor
            return 0.15f;
        }

        private void UpdateDrag(Vector3 pointerWorld)
        {
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);

            // 1. Smoothly glide lift offset up above the thumb
            float targetOffsetY = GetTargetDragOffsetY();
            m_CurrentLiftOffset = Mathf.Lerp(m_CurrentLiftOffset, targetOffsetY, 1f - Mathf.Exp(-22f * dt));

            // 2. Juicy elastic pop scale from slot scale (0.52) to board scale (1.0)
            m_CurrentScale = Mathf.Lerp(m_CurrentScale, m_BoardScale, 1f - Mathf.Exp(-24f * dt));
            transform.localScale = Vector3.one * m_CurrentScale;

            // 3. Jitter-free high-frequency finger tracking (zero lag, filters digitizer noise)
            Vector3 targetPos = new Vector3(pointerWorld.x, pointerWorld.y + m_CurrentLiftOffset, 0f);
            Vector3 newPos = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-55f * dt));

            // 4. Subtle dynamic physical tilt based on horizontal drag velocity
            Vector3 velocity = (newPos - m_LastPos) / dt;
            m_LastPos = newPos;

            float targetAngle = -Mathf.Clamp(velocity.x * 0.45f, -7.5f, 7.5f);
            m_CurrentZAngle = Mathf.Lerp(m_CurrentZAngle, targetAngle, 1f - Mathf.Exp(-18f * dt));
            transform.rotation = Quaternion.Euler(0f, 0f, m_CurrentZAngle);

            transform.position = newPos;

            // 5. Test grid intersection for ghost preview
            if (GridManager.Instance != null)
            {
                if (GridManager.Instance.WorldToGridOrigin(newPos, Data, out Vector2Int origin))
                {
                    GridManager.Instance.ShowPreview(Data, origin.x, origin.y);
                }
                else
                {
                    GridManager.Instance.ClearPreview();
                }
            }
        }

        private void EndDrag(Vector3 pointerWorld)
        {
            IsDragging = false;
            if (ActiveDragShape == this) ActiveDragShape = null;

            // Use current transform position directly so block placement exactly matches the ghost preview
            Vector3 targetPos = transform.position;

            bool placed = false;
            if (GridManager.Instance != null)
            {
                if (GridManager.Instance.WorldToGridOrigin(targetPos, Data, out Vector2Int origin))
                {
                    placed = GridManager.Instance.PlaceShape(Data, origin.x, origin.y, GetBlockGems());
                }
                GridManager.Instance.ClearPreview();
            }

            if (placed)
            {
                HapticManager.TriggerLight();
                if (ShapeSpawner.Instance != null)
                {
                    ShapeSpawner.Instance.OnShapePlaced(this);
                }
                Destroy(gameObject);
            }
            else
            {
                SetBlocksSortingOrder(10);
                m_ReturnCoroutine = StartCoroutine(AnimateReturn());
            }
        }

        public List<GemType> GetBlockGems()
        {
            List<GemType> gems = new List<GemType>();
            for (int i = 0; i < m_Blocks.Count; i++)
            {
                gems.Add(m_Blocks[i].EmbeddedGem);
            }
            return gems;
        }

        private IEnumerator AnimateReturn()
        {
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            Quaternion startRot = transform.rotation;
            Vector3 endScale = Vector3.one * m_SlotScale;

            float elapsed = 0f;
            float duration = 0.16f; // Snappy, responsive return

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth elastic overshoot curve
                float curvedT = Mathf.Sin(t * Mathf.PI * 0.5f);

                transform.position = Vector3.Lerp(startPos, SlotPosition, curvedT);
                transform.localScale = Vector3.Lerp(startScale, endScale, curvedT);
                transform.rotation = Quaternion.Slerp(startRot, Quaternion.identity, curvedT);
                yield return null;
            }

            transform.position = SlotPosition;
            transform.localScale = endScale;
            transform.rotation = Quaternion.identity;
        }

        public void SetDimmed(bool dimmed)
        {
            float targetAlpha = dimmed ? 0.35f : 1.0f;
            for (int i = 0; i < m_Blocks.Count; i++)
            {
                if (m_Blocks[i] != null)
                {
                    SpriteRenderer sr = m_Blocks[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = targetAlpha;
                        sr.color = c;
                    }
                }
            }
        }

        private void SetBlocksSortingOrder(int order)
        {
            for (int i = 0; i < m_Blocks.Count; i++)
            {
                if (m_Blocks[i] != null)
                {
                    m_Blocks[i].SetSortingOrder(order);
                }
            }
        }
    }
}
