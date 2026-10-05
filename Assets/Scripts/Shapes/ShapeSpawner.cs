using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// Controls the 3 square puzzle slot boxes (vertical in Landscape, horizontal in Portrait).
    /// Each puzzle piece sits inside its own square box. When a puzzle is used, that box
    /// becomes empty. When all 3 puzzles are used, a brand new batch of 3 puzzles spawns.
    /// </summary>
    public class ShapeSpawner : MonoBehaviour
    {
        public static ShapeSpawner Instance { get; private set; }

        private Vector3[] m_LandscapePositions = new Vector3[]
        {
            new Vector3(4.05f,  2.25f, 0f),
            new Vector3(4.05f,  0.0f,  0f),
            new Vector3(4.05f, -2.25f, 0f)
        };

        private Vector3[] m_PortraitPositions = new Vector3[]
        {
            new Vector3(-2.10f, -4.30f, 0f),
            new Vector3( 0.00f, -4.30f, 0f),
            new Vector3( 2.10f, -4.30f, 0f)
        };

        private Vector3[] m_CurrentSlotPositions;
        private readonly GameObject[] m_SlotBoxObjects = new GameObject[3];
        private readonly DraggableShape[] m_SlotShapes = new DraggableShape[3];
        private bool m_IsLandscape = true;

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

            m_IsLandscape = Screen.width > Screen.height;
            m_CurrentSlotPositions = m_IsLandscape ? m_LandscapePositions : m_PortraitPositions;

            CreateSlotBoxes();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void CreateSlotBoxes()
        {
            if (m_CurrentSlotPositions == null)
            {
                m_CurrentSlotPositions = m_IsLandscape ? m_LandscapePositions : m_PortraitPositions;
            }

            for (int i = 0; i < 3; i++)
            {
                if (m_SlotBoxObjects[i] == null)
                {
                    GameObject boxObj = new GameObject($"SlotBox_{i}");
                    boxObj.transform.SetParent(transform, false);

                    SpriteRenderer sr = boxObj.AddComponent<SpriteRenderer>();
                    sr.sprite = SpriteFactory.GetPanelSprite(
                        new Color(0.06f, 0.09f, 0.18f, 0.90f),
                        new Color(0.18f, 0.28f, 0.52f, 0.80f),
                        2.5f, 20
                    );
                    sr.drawMode = SpriteDrawMode.Sliced;
                    sr.size = new Vector2(1.98f, 1.98f);
                    sr.sortingOrder = 2; // Behind shape blocks (sortingOrder 10)

                    m_SlotBoxObjects[i] = boxObj;
                }

                m_SlotBoxObjects[i].transform.position = m_CurrentSlotPositions[i];
            }
        }

        public void UpdateLayout(bool isLandscape)
        {
            m_IsLandscape = isLandscape;
            m_CurrentSlotPositions = isLandscape ? m_LandscapePositions : m_PortraitPositions;

            CreateSlotBoxes();

            for (int i = 0; i < 3; i++)
            {
                if (m_SlotBoxObjects[i] != null)
                {
                    m_SlotBoxObjects[i].transform.position = m_CurrentSlotPositions[i];
                }

                if (m_SlotShapes[i] != null)
                {
                    m_SlotShapes[i].UpdateSlotPosition(m_CurrentSlotPositions[i]);
                }
            }
        }

        public void SpawnBatch()
        {
            ClearAll();

            if (m_CurrentSlotPositions == null)
            {
                m_CurrentSlotPositions = m_IsLandscape ? m_LandscapePositions : m_PortraitPositions;
            }

            CreateSlotBoxes();

            List<ShapeData> allShapes = ShapeCatalog.GetAllShapes();
            List<ShapeData> fittingShapes = GetFittingShapes(allShapes);

            int occupiedCount = (GridManager.Instance != null) ? GridManager.Instance.GetOccupiedCount() : 0;
            bool isCrowded = occupiedCount >= 22; // > 35% board full
            bool isLevelMode = (LevelManager.Instance != null && LevelManager.Instance.IsLevelActive);

            for (int i = 0; i < 3; i++)
            {
                ShapeData chosenData = null;

                // Slot 0: Guaranteed to be a shape that CAN fit on the board right now!
                if (i == 0 && fittingShapes.Count > 0)
                {
                    chosenData = fittingShapes[Random.Range(0, fittingShapes.Count)];
                }
                // Slot 1: High probability to fit, or a small versatile piece if crowded / level mode
                else if (i == 1)
                {
                    float fitChance = isLevelMode ? 0.98f : 0.85f;
                    if (fittingShapes.Count > 0 && Random.value < fitChance)
                    {
                        chosenData = fittingShapes[Random.Range(0, fittingShapes.Count)];
                    }
                    else if (isCrowded || isLevelMode)
                    {
                        List<ShapeData> smallShapes = GetSmallShapes(allShapes);
                        chosenData = smallShapes[Random.Range(0, smallShapes.Count)];
                    }
                }
                // Slot 2: High probability to fit, or standard shape
                else if (i == 2)
                {
                    float fitChance = isLevelMode ? 0.95f : 0.70f;
                    if (fittingShapes.Count > 0 && Random.value < fitChance)
                    {
                        chosenData = fittingShapes[Random.Range(0, fittingShapes.Count)];
                    }
                    else if (isCrowded || isLevelMode)
                    {
                        List<ShapeData> smallShapes = GetSmallShapes(allShapes);
                        chosenData = smallShapes[Random.Range(0, smallShapes.Count)];
                    }
                }

                // Fallback guarantee: if still null, pick from fitting or small shapes
                if (chosenData == null)
                {
                    if (fittingShapes.Count > 0)
                    {
                        chosenData = fittingShapes[Random.Range(0, fittingShapes.Count)];
                    }
                    else
                    {
                        List<ShapeData> smallShapes = GetSmallShapes(allShapes);
                        chosenData = (smallShapes.Count > 0) ? smallShapes[Random.Range(0, smallShapes.Count)] : allShapes[Random.Range(0, allShapes.Count)];
                    }
                }

                SpawnShapeAtSlot(chosenData, m_CurrentSlotPositions[i], i);
            }

            CheckMoveAvailability();
        }

        private List<ShapeData> GetFittingShapes(List<ShapeData> allShapes)
        {
            List<ShapeData> fitting = new List<ShapeData>();
            if (GridManager.Instance == null) return allShapes;

            for (int i = 0; i < allShapes.Count; i++)
            {
                ShapeData s = allShapes[i];
                if (s == null) continue;

                bool canFit = false;
                for (int x = 0; x <= GridManager.BoardWidth - s.Width; x++)
                {
                    for (int y = 0; y <= GridManager.BoardHeight - s.Height; y++)
                    {
                        if (GridManager.Instance.CanShapeFit(s, x, y))
                        {
                            canFit = true;
                            break;
                        }
                    }
                    if (canFit) break;
                }

                if (canFit) fitting.Add(s);
            }
            return fitting;
        }

        private List<ShapeData> GetSmallShapes(List<ShapeData> allShapes)
        {
            List<ShapeData> small = new List<ShapeData>();
            for (int i = 0; i < allShapes.Count; i++)
            {
                ShapeData s = allShapes[i];
                if (s != null && s.Blocks != null && s.Blocks.Length <= 3)
                {
                    small.Add(s);
                }
            }
            return small.Count > 0 ? small : allShapes;
        }

        public void ShuffleShapes()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayShuffle();
            SpawnBatch();
            FloatingText.Create(new Vector3(m_IsLandscape ? 3.55f : 0f, 0f, 0f), "SHUFFLED!", new Color(1.0f, 0.45f, 0.85f), 6f);
        }

        private void SpawnShapeAtSlot(ShapeData data, Vector3 slotPos, int slotIndex)
        {
            GameObject shapeObj = new GameObject($"Shape_Slot{slotIndex}_{data.ShapeName}");
            shapeObj.transform.SetParent(transform, false);

            DraggableShape draggable = shapeObj.AddComponent<DraggableShape>();
            float cellSize = (GridManager.Instance != null) ? GridManager.Instance.TotalCellStep : 0.72f;

            // In levels with gem objectives, occasionally embed a target gemstone inside one of the shape's spheres!
            List<GemType> gems = null;
            if (LevelManager.Instance != null && LevelManager.Instance.IsLevelActive && LevelManager.Instance.CurrentLevel != null && LevelManager.Instance.CurrentLevel.TargetGems != null && LevelManager.Instance.CurrentLevel.TargetGems.Count > 0)
            {
                if (UnityEngine.Random.value < 0.35f)
                {
                    gems = new List<GemType>();
                    for (int b = 0; b < data.Blocks.Length; b++) gems.Add(GemType.None);

                    List<GemType> availableGems = new List<GemType>(LevelManager.Instance.CurrentLevel.TargetGems.Keys);
                    if (availableGems.Count > 0)
                    {
                        GemType chosenGem = availableGems[UnityEngine.Random.Range(0, availableGems.Count)];
                        int gemBlockIdx = UnityEngine.Random.Range(0, data.Blocks.Length);
                        gems[gemBlockIdx] = chosenGem;
                    }
                }
            }

            draggable.Initialize(data, slotPos, cellSize, slotIndex, gems);

            m_SlotShapes[slotIndex] = draggable;
            StartCoroutine(AnimateSpawn(shapeObj.transform));
        }

        private IEnumerator AnimateSpawn(Transform t)
        {
            Vector3 targetScale = t.localScale;
            t.localScale = Vector3.zero;

            float elapsed = 0f;
            float duration = 0.22f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float scaleMul = Mathf.Sin(progress * Mathf.PI * 0.5f);
                t.localScale = targetScale * scaleMul;
                yield return null;
            }

            t.localScale = targetScale;
        }

        public void OnShapePlaced(DraggableShape placedShape)
        {
            int slotIdx = placedShape.SlotIndex;
            if (slotIdx >= 0 && slotIdx < 3 && m_SlotShapes[slotIdx] == placedShape)
            {
                m_SlotShapes[slotIdx] = null;
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    if (m_SlotShapes[i] == placedShape)
                    {
                        m_SlotShapes[i] = null;
                        break;
                    }
                }
            }

            // Check how many shapes remain in the 3 square boxes
            int remainingCount = 0;
            for (int i = 0; i < 3; i++)
            {
                if (m_SlotShapes[i] != null) remainingCount++;
            }

            // If all 3 puzzles have been used -> Spawn 3 fresh new puzzles into all boxes!
            if (remainingCount == 0)
            {
                SpawnBatch();
            }
            else
            {
                CheckMoveAvailability();
            }
        }

        public void CheckMoveAvailability()
        {
            if (GridManager.Instance == null) return;

            List<ShapeData> remainingData = new List<ShapeData>();
            for (int i = 0; i < 3; i++)
            {
                if (m_SlotShapes[i] != null && m_SlotShapes[i].Data != null)
                {
                    remainingData.Add(m_SlotShapes[i].Data);

                    bool canFitThis = false;
                    for (int x = 0; x <= GridManager.BoardWidth - m_SlotShapes[i].Data.Width; x++)
                    {
                        for (int y = 0; y <= GridManager.BoardHeight - m_SlotShapes[i].Data.Height; y++)
                        {
                            if (GridManager.Instance.CanShapeFit(m_SlotShapes[i].Data, x, y))
                            {
                                canFitThis = true;
                                break;
                            }
                        }
                        if (canFitThis) break;
                    }

                    m_SlotShapes[i].SetDimmed(!canFitThis);
                }
            }

            if (!GridManager.Instance.CanAnyShapeFit(remainingData))
            {
                if (LevelManager.Instance != null && LevelManager.Instance.IsLevelActive)
                {
                    // 1. If player STILL has moves remaining in Level Mode:
                    if (LevelManager.Instance.MovesRemaining > 0)
                    {
                        List<ShapeData> allShapes = ShapeCatalog.GetAllShapes();
                        List<ShapeData> fittingShapes = GetFittingShapes(allShapes);

                        // If there are shapes that CAN fit on the board, AUTO-SHUFFLE instead of failing!
                        if (fittingShapes.Count > 0)
                        {
                            if (!m_IsAutoShuffling)
                            {
                                StartCoroutine(AutoShuffleUnplaceableShapes());
                            }
                            return;
                        }
                    }

                    // 2. Only if board is truly 100% full (no shape in existence can fit) or out of moves:
                    DefeatReason reason = (LevelManager.Instance.MovesRemaining <= 0) ? DefeatReason.OutOfMoves : DefeatReason.BoardFull;
                    LevelManager.Instance.FailLevel(reason);
                }
                else if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver();
                }
            }
        }

        private bool m_IsAutoShuffling = false;

        private IEnumerator AutoShuffleUnplaceableShapes()
        {
            m_IsAutoShuffling = true;
            yield return new WaitForSeconds(0.2f);

            FloatingText.Create(new Vector3(m_IsLandscape ? 3.55f : 0f, -2.1f, 0f), "PIECES SHUFFLED! 🔄", new Color(1.0f, 0.88f, 0.25f), 6.5f);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayShuffle();

            for (int i = 0; i < 3; i++)
            {
                if (m_SlotShapes[i] != null)
                {
                    Destroy(m_SlotShapes[i].gameObject);
                    m_SlotShapes[i] = null;
                }
            }

            SpawnBatch();
            m_IsAutoShuffling = false;
        }

        public void ClearAll()
        {
            m_IsAutoShuffling = false;
            StopAllCoroutines();
            for (int i = 0; i < 3; i++)
            {
                if (m_SlotShapes[i] != null)
                {
                    Destroy(m_SlotShapes[i].gameObject);
                    m_SlotShapes[i] = null;
                }
            }
        }

        public void RefreshActiveShapeSprites()
        {
            for (int i = 0; i < 3; i++)
            {
                if (m_SlotShapes[i] != null)
                {
                    m_SlotShapes[i].RefreshSprites();
                }
            }
        }
    }
}
