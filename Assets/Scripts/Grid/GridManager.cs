using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// Manages the 8x8 Grid, cell allocation, shape placement checks, ghost previews, 
    /// line clears, booster actions (Bomb, Cannon, Arrow), responsive layout, and camera shake.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        public const int BoardWidth = 8;
        public const int BoardHeight = 8;

        [Header("Grid Layout Settings")]
        [SerializeField] private float m_CellSize = 0.65f;
        [SerializeField] private float m_CellSpacing = 0.04f;
        [SerializeField] private Vector2 m_GridCenter = new Vector2(0f, 0.40f);

        private GridCell[,] m_Cells;
        private readonly List<GridCell> m_ActivePreviewCells = new List<GridCell>();
        private readonly List<GridCell> m_ActiveLineGlowCells = new List<GridCell>();
        private GameObject m_BoardBgObj;
        private Camera m_MainCamera;
        private Coroutine m_ShakeCoroutine;

        public float TotalCellStep => m_CellSize + m_CellSpacing;
        public Vector2 GridCenter => m_GridCenter;
        public BoosterType ActiveBooster { get; private set; } = BoosterType.None;

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

            m_MainCamera = Camera.main;
            BuildGrid();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Handle booster targeting click/tap
            if (ActiveBooster != BoosterType.None && InputHelper.IsPointerDown())
            {
                HandleBoosterTargetClick();
            }
        }

        public void BuildGrid()
        {
            // Clear existing cells if any
            if (m_Cells != null)
            {
                for (int x = 0; x < BoardWidth; x++)
                {
                    for (int y = 0; y < BoardHeight; y++)
                    {
                        if (m_Cells[x, y] != null)
                        {
                            Destroy(m_Cells[x, y].gameObject);
                        }
                    }
                }
            }

            m_Cells = new GridCell[BoardWidth, BoardHeight];

            // 1. Board Background Panel
            if (m_BoardBgObj == null)
            {
                m_BoardBgObj = new GameObject("BoardBackground");
                m_BoardBgObj.transform.SetParent(transform, false);
                SpriteRenderer bgSr = m_BoardBgObj.AddComponent<SpriteRenderer>();
                bgSr.sprite = SpriteFactory.GetBoardBackgroundSprite();
                bgSr.sortingOrder = 0;
            }

            float totalWidth = BoardWidth * TotalCellStep + 0.32f;
            float totalHeight = BoardHeight * TotalCellStep + 0.32f;
            m_BoardBgObj.transform.position = new Vector3(m_GridCenter.x, m_GridCenter.y, 0f);
            m_BoardBgObj.transform.localScale = new Vector3(totalWidth, totalHeight, 1f);

            // 2. Instantiate 9x9 Grid Cells
            float startX = m_GridCenter.x - ((BoardWidth - 1) * TotalCellStep * 0.5f);
            float startY = m_GridCenter.y - ((BoardHeight - 1) * TotalCellStep * 0.5f);

            for (int x = 0; x < BoardWidth; x++)
            {
                for (int y = 0; y < BoardHeight; y++)
                {
                    GameObject cellObj = new GameObject($"Cell_{x}_{y}");
                    cellObj.transform.SetParent(transform, false);

                    Vector3 worldPos = new Vector3(startX + x * TotalCellStep, startY + y * TotalCellStep, 0f);
                    cellObj.transform.position = worldPos;

                    GridCell cell = cellObj.AddComponent<GridCell>();
                    cell.Initialize(x, y, m_CellSize);
                    m_Cells[x, y] = cell;
                }
            }
        }

        public void UpdateLayout(bool isLandscape)
        {
            if (m_MainCamera == null) m_MainCamera = Camera.main;

            if (isLandscape)
            {
                m_GridCenter = new Vector2(-0.35f, 0f);
                m_CellSize = 0.60f;
                m_CellSpacing = 0.04f;
                if (m_MainCamera != null)
                {
                    float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
                    m_MainCamera.orthographicSize = Mathf.Max(5.5f, (14.5f / aspect) * 0.5f);
                }
            }
            else
            {
                m_GridCenter = new Vector2(0f, 0.40f);
                m_CellSize = 0.65f;
                m_CellSpacing = 0.04f;
                if (m_MainCamera != null)
                {
                    float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
                    m_MainCamera.orthographicSize = Mathf.Max(6.6f, (6.8f / aspect) * 0.5f);
                }
            }

            if (m_BoardBgObj != null)
            {
                float totalWidth = BoardWidth * TotalCellStep + 0.32f;
                float totalHeight = BoardHeight * TotalCellStep + 0.32f;
                m_BoardBgObj.transform.position = new Vector3(m_GridCenter.x, m_GridCenter.y, 0f);
                m_BoardBgObj.transform.localScale = new Vector3(totalWidth, totalHeight, 1f);
            }

            if (m_Cells != null)
            {
                float startX = m_GridCenter.x - ((BoardWidth - 1) * TotalCellStep * 0.5f);
                float startY = m_GridCenter.y - ((BoardHeight - 1) * TotalCellStep * 0.5f);

                for (int x = 0; x < BoardWidth; x++)
                {
                    for (int y = 0; y < BoardHeight; y++)
                    {
                        if (m_Cells[x, y] != null)
                        {
                            Vector3 worldPos = new Vector3(startX + x * TotalCellStep, startY + y * TotalCellStep, 0f);
                            m_Cells[x, y].transform.position = worldPos;
                            m_Cells[x, y].transform.localScale = Vector3.one * (m_CellSize * 0.98f);
                        }
                    }
                }
            }
        }

        public Vector3 GetCellWorldPosition(int x, int y)
        {
            float startX = m_GridCenter.x - ((BoardWidth - 1) * TotalCellStep * 0.5f);
            float startY = m_GridCenter.y - ((BoardHeight - 1) * TotalCellStep * 0.5f);
            return new Vector3(startX + x * TotalCellStep, startY + y * TotalCellStep, 0f);
        }

        public bool WorldToGridOrigin(Vector3 worldPos, ShapeData shape, out Vector2Int origin)
        {
            origin = Vector2Int.zero;
            if (shape == null) return false;

            float startX = m_GridCenter.x - ((BoardWidth - 1) * TotalCellStep * 0.5f);
            float startY = m_GridCenter.y - ((BoardHeight - 1) * TotalCellStep * 0.5f);

            Vector2 centerOffset = shape.GetCenterOffset() * TotalCellStep;
            float cornerWorldX = worldPos.x - centerOffset.x;
            float cornerWorldY = worldPos.y - centerOffset.y;

            int ox = Mathf.RoundToInt((cornerWorldX - startX) / TotalCellStep);
            int oy = Mathf.RoundToInt((cornerWorldY - startY) / TotalCellStep);

            origin = new Vector2Int(ox, oy);
            return CanShapeFit(shape, ox, oy);
        }

        public bool CanShapeFit(ShapeData shape, int originX, int originY)
        {
            if (shape == null || shape.Blocks == null) return false;

            for (int i = 0; i < shape.Blocks.Length; i++)
            {
                int gx = originX + shape.Blocks[i].x;
                int gy = originY + shape.Blocks[i].y;

                if (gx < 0 || gx >= BoardWidth || gy < 0 || gy >= BoardHeight)
                {
                    return false;
                }

                if (m_Cells[gx, gy] != null && m_Cells[gx, gy].IsOccupied)
                {
                    return false;
                }
            }

            return true;
        }

        private Vector2Int m_LastPreviewOrigin = new Vector2Int(-999, -999);
        private ShapeData m_LastPreviewShape = null;

        public void ShowPreview(ShapeData shape, int originX, int originY)
        {
            if (shape == null)
            {
                ClearPreview();
                return;
            }

            // High performance check: if position hasn't changed, skip re-evaluating cells!
            if (shape == m_LastPreviewShape && originX == m_LastPreviewOrigin.x && originY == m_LastPreviewOrigin.y)
            {
                return;
            }

            ClearPreview();
            m_LastPreviewShape = shape;
            m_LastPreviewOrigin = new Vector2Int(originX, originY);

            for (int i = 0; i < shape.Blocks.Length; i++)
            {
                int gx = originX + shape.Blocks[i].x;
                int gy = originY + shape.Blocks[i].y;

                if (gx >= 0 && gx < BoardWidth && gy >= 0 && gy < BoardHeight)
                {
                    GridCell cell = m_Cells[gx, gy];
                    if (cell != null && !cell.IsOccupied)
                    {
                        cell.SetPreview(true, shape.BlockColor);
                        m_ActivePreviewCells.Add(cell);
                    }
                }
            }

            // Anticipatory Line Clear Pre-Glow (Block Blast style)
            if (CanShapeFit(shape, originX, originY))
            {
                // Check rows
                for (int y = 0; y < BoardHeight; y++)
                {
                    bool willClear = true;
                    for (int x = 0; x < BoardWidth; x++)
                    {
                        if (m_Cells[x, y] == null) { willClear = false; break; }
                        if (!m_Cells[x, y].IsOccupied)
                        {
                            bool covered = false;
                            for (int b = 0; b < shape.Blocks.Length; b++)
                            {
                                if (originX + shape.Blocks[b].x == x && originY + shape.Blocks[b].y == y)
                                {
                                    covered = true;
                                    break;
                                }
                            }
                            if (!covered) { willClear = false; break; }
                        }
                    }

                    if (willClear)
                    {
                        for (int x = 0; x < BoardWidth; x++)
                        {
                            GridCell c = m_Cells[x, y];
                            if (c != null && !m_ActiveLineGlowCells.Contains(c))
                            {
                                c.SetLineClearHighlight(true);
                                m_ActiveLineGlowCells.Add(c);
                            }
                        }
                    }
                }

                // Check columns
                for (int x = 0; x < BoardWidth; x++)
                {
                    bool willClear = true;
                    for (int y = 0; y < BoardHeight; y++)
                    {
                        if (m_Cells[x, y] == null) { willClear = false; break; }
                        if (!m_Cells[x, y].IsOccupied)
                        {
                            bool covered = false;
                            for (int b = 0; b < shape.Blocks.Length; b++)
                            {
                                if (originX + shape.Blocks[b].x == x && originY + shape.Blocks[b].y == y)
                                {
                                    covered = true;
                                    break;
                                }
                            }
                            if (!covered) { willClear = false; break; }
                        }
                    }

                    if (willClear)
                    {
                        for (int y = 0; y < BoardHeight; y++)
                        {
                            GridCell c = m_Cells[x, y];
                            if (c != null && !m_ActiveLineGlowCells.Contains(c))
                            {
                                c.SetLineClearHighlight(true);
                                m_ActiveLineGlowCells.Add(c);
                            }
                        }
                    }
                }
            }
        }

        public void ClearPreview()
        {
            if (m_ActivePreviewCells.Count == 0 && m_ActiveLineGlowCells.Count == 0 && m_LastPreviewShape == null) return;

            for (int i = 0; i < m_ActivePreviewCells.Count; i++)
            {
                if (m_ActivePreviewCells[i] != null)
                {
                    m_ActivePreviewCells[i].SetPreview(false);
                }
            }
            m_ActivePreviewCells.Clear();

            for (int i = 0; i < m_ActiveLineGlowCells.Count; i++)
            {
                if (m_ActiveLineGlowCells[i] != null)
                {
                    m_ActiveLineGlowCells[i].SetLineClearHighlight(false);
                }
            }
            m_ActiveLineGlowCells.Clear();

            m_LastPreviewShape = null;
            m_LastPreviewOrigin = new Vector2Int(-999, -999);
        }

        public bool PlaceShape(ShapeData shape, int originX, int originY, List<GemType> blockGems = null)
        {
            if (!CanShapeFit(shape, originX, originY))
            {
                return false;
            }

            ClearPreview();

            for (int i = 0; i < shape.Blocks.Length; i++)
            {
                int gx = originX + shape.Blocks[i].x;
                int gy = originY + shape.Blocks[i].y;
                GemType gem = (blockGems != null && i < blockGems.Count) ? blockGems[i] : GemType.None;
                m_Cells[gx, gy].Fill(shape.BlockColor, gem, true);
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPlace();
            }

            // Award score and coins
            int blockPoints = shape.Blocks.Length * 10;
            GameManager.Instance.AddScore(blockPoints, GetCellWorldPosition(originX, originY));
            GameManager.Instance.AddCoins(shape.Blocks.Length, false); // 1 coin per placed block

            // First evaluate line clears and collect gems
            CheckAndClearLines();

            // Second evaluate moves and level win/loss
            if (LevelManager.Instance != null && LevelManager.Instance.IsLevelActive)
            {
                LevelManager.Instance.OnMoveMade();
            }

            return true;
        }

        public void CheckAndClearLines()
        {
            List<int> fullRows = new List<int>();
            List<int> fullCols = new List<int>();

            // Check Rows
            for (int y = 0; y < BoardHeight; y++)
            {
                bool rowFull = true;
                for (int x = 0; x < BoardWidth; x++)
                {
                    if (m_Cells[x, y] == null || !m_Cells[x, y].IsOccupied)
                    {
                        rowFull = false;
                        break;
                    }
                }
                if (rowFull) fullRows.Add(y);
            }

            // Check Columns
            for (int x = 0; x < BoardWidth; x++)
            {
                bool colFull = true;
                for (int y = 0; y < BoardHeight; y++)
                {
                    if (m_Cells[x, y] == null || !m_Cells[x, y].IsOccupied)
                    {
                        colFull = false;
                        break;
                    }
                }
                if (colFull) fullCols.Add(x);
            }

            // Check 3x3 Sectors (only if 9x9 board)
            List<Vector2Int> fullSectors = new List<Vector2Int>();
            if (BoardWidth >= 9 && BoardHeight >= 9)
            {
                for (int sx = 0; sx < 3; sx++)
                {
                    for (int sy = 0; sy < 3; sy++)
                    {
                        bool sectorFull = true;
                        int startX = sx * 3;
                        int startY = sy * 3;

                        for (int dx = 0; dx < 3; dx++)
                        {
                            for (int dy = 0; dy < 3; dy++)
                            {
                                int gx = startX + dx;
                                int gy = startY + dy;
                                if (m_Cells[gx, gy] == null || !m_Cells[gx, gy].IsOccupied)
                                {
                                    sectorFull = false;
                                    break;
                                }
                            }
                            if (!sectorFull) break;
                        }
                        if (sectorFull) fullSectors.Add(new Vector2Int(sx, sy));
                    }
                }
            }

            int totalClears = fullRows.Count + fullCols.Count + fullSectors.Count;

            if (totalClears > 0)
            {
                HashSet<GridCell> cellsToClear = new HashSet<GridCell>();
                List<Color> clearedColors = new List<Color>();

                foreach (int y in fullRows)
                {
                    for (int x = 0; x < BoardWidth; x++)
                    {
                        cellsToClear.Add(m_Cells[x, y]);
                    }
                }

                foreach (int x in fullCols)
                {
                    for (int y = 0; y < BoardHeight; y++)
                    {
                        cellsToClear.Add(m_Cells[x, y]);
                    }
                }

                foreach (Vector2Int sector in fullSectors)
                {
                    int startX = sector.x * 3;
                    int startY = sector.y * 3;
                    for (int dx = 0; dx < 3; dx++)
                    {
                        for (int dy = 0; dy < 3; dy++)
                        {
                            cellsToClear.Add(m_Cells[startX + dx, startY + dy]);
                        }
                    }
                }

                foreach (GridCell cell in cellsToClear)
                {
                    clearedColors.Add(cell.CurrentColor);
                }

                if (LineBlastAnimator.Instance != null && LineBlastAnimator.Instance.IsAnimationEnabled)
                {
                    LineBlastAnimator.Instance.PlayLineBlastSequence(
                        fullRows, fullCols, fullSectors, m_Cells, BoardWidth, BoardHeight, m_GridCenter);
                }
                else
                {
                    float delay = 0f;
                    foreach (GridCell cell in cellsToClear)
                    {
                        cell.Clear(true, delay);
                        if (ParticleFXManager.Instance != null)
                        {
                            ParticleFXManager.Instance.PlayCellBlast(cell.transform.position, cell.CurrentColor, delay);
                        }
                        delay += 0.012f;
                    }
                }

                GameManager.Instance.RegisterLineClear(totalClears, m_GridCenter);
                ShakeCamera(0.20f, 0.12f * totalClears);

                if (fullSectors.Count > 0)
                {
                    // Center position of all cleared 3x3 sectors
                    Vector3 popupPos = Vector3.zero;
                    foreach (Vector2Int sector in fullSectors)
                    {
                        int cx = sector.x * 3 + 1;
                        int cy = sector.y * 3 + 1;
                        popupPos += m_Cells[cx, cy].transform.position;
                    }
                    popupPos /= fullSectors.Count;

                    // Trigger expanding golden shockwave & star shower
                    if (ParticleFXManager.Instance != null)
                    {
                        ParticleFXManager.Instance.PlayZoneClearShockwave(popupPos);
                    }

                }

                // Multi-line combo celebration sparks & floating badge
                if (totalClears >= 2)
                {
                    if (ParticleFXManager.Instance != null)
                    {
                        ParticleFXManager.Instance.PlayComboCelebration(m_GridCenter, totalClears);
                    }

                    string blastBadge = totalClears == 2 ? "DOUBLE BLAST!" :
                                        totalClears == 3 ? "TRIPLE BLAST!" :
                                        totalClears == 4 ? "QUAD BLAST!" : "MEGA BLAST!";
                    Color badgeColor = totalClears == 2 ? new Color(1.0f, 0.82f, 0.20f) :
                                       totalClears == 3 ? new Color(1.0f, 0.40f, 0.15f) :
                                       new Color(0.95f, 0.15f, 0.65f);
                    FloatingText.Create(new Vector3(m_GridCenter.x, m_GridCenter.y + 0.85f, 0f), blastBadge, badgeColor, 8f);
                }

                if (LevelManager.Instance != null && LevelManager.Instance.IsLevelActive)
                {
                    LevelManager.Instance.OnLinesCleared(totalClears, clearedColors);
                }
            }
            else
            {
                GameManager.Instance.RegisterNoLineClear();
            }
        }

        // ========================================================
        // 💥 BOOSTER EXECUTION
        // ========================================================
        public void SelectBooster(BoosterType booster)
        {
            if (ActiveBooster == booster)
            {
                ActiveBooster = BoosterType.None; // Toggle off
            }
            else
            {
                ActiveBooster = booster;
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateBoosterSelection(ActiveBooster);
            }
        }

        private void HandleBoosterTargetClick()
        {
            if (InputHelper.IsPointerOverUI()) return;

            if (m_MainCamera == null) m_MainCamera = Camera.main;
            if (m_MainCamera == null) return;

            Vector2 pointerScreen = InputHelper.GetPointerScreenPosition();
            Vector3 worldPos = m_MainCamera.ScreenToWorldPoint(new Vector3(pointerScreen.x, pointerScreen.y, -m_MainCamera.transform.position.z));

            // Check which cell was clicked
            float startX = m_GridCenter.x - ((BoardWidth - 1) * TotalCellStep * 0.5f);
            float startY = m_GridCenter.y - ((BoardHeight - 1) * TotalCellStep * 0.5f);

            int cx = Mathf.RoundToInt((worldPos.x - startX) / TotalCellStep);
            int cy = Mathf.RoundToInt((worldPos.y - startY) / TotalCellStep);

            if (cx < 0 || cx >= BoardWidth || cy < 0 || cy >= BoardHeight)
            {
                return; // Clicked outside the grid
            }

            switch (ActiveBooster)
            {
                case BoosterType.Bomb:
                    ExecuteBomb(cx, cy);
                    GameManager.Instance.ConsumeBooster(BoosterType.Bomb);
                    break;

                case BoosterType.Cannon:
                    ExecuteCannon(cx, cy);
                    GameManager.Instance.ConsumeBooster(BoosterType.Cannon);
                    break;

                case BoosterType.Arrow:
                    if (m_Cells[cx, cy].IsOccupied)
                    {
                        ExecuteArrow(cx, cy);
                        GameManager.Instance.ConsumeBooster(BoosterType.Arrow);
                    }
                    else
                    {
                        return; // Must click an occupied block
                    }
                    break;
            }

            SelectBooster(BoosterType.None);
        }

        public void ExecuteBomb(int centerX, int centerY)
        {
            int cleared = 0;
            float delay = 0f;

            for (int x = centerX - 1; x <= centerX + 1; x++)
            {
                for (int y = centerY - 1; y <= centerY + 1; y++)
                {
                    if (x >= 0 && x < BoardWidth && y >= 0 && y < BoardHeight)
                    {
                        if (m_Cells[x, y].IsOccupied)
                        {
                            m_Cells[x, y].Clear(true, delay);
                            delay += 0.02f;
                            cleared++;
                        }
                    }
                }
            }

            HapticManager.TriggerHeavy();
            if (SoundManager.Instance != null) SoundManager.Instance.PlayBomb();
            ShakeCamera(0.25f, 0.22f);
            Vector3 centerPos = GetCellWorldPosition(centerX, centerY);
            if (ParticleFXManager.Instance != null) ParticleFXManager.Instance.PlayBombBurst(centerPos);
            FloatingText.Create(centerPos, "BOMB BLAST!", new Color(1.0f, 0.45f, 0.15f), 7f);

            int bonus = cleared * 20;
            if (bonus > 0)
            {
                GameManager.Instance.AddScore(bonus, centerPos);
                GameManager.Instance.AddCoins(cleared * 2, true);
            }

            // Check if game over condition changed
            if (ShapeSpawner.Instance != null) ShapeSpawner.Instance.CheckMoveAvailability();
        }

        public void ExecuteCannon(int targetX, int targetY)
        {
            int cleared = 0;
            float delay = 0f;

            // Clear row and column through this target cell
            for (int x = 0; x < BoardWidth; x++)
            {
                if (m_Cells[x, targetY].IsOccupied)
                {
                    m_Cells[x, targetY].Clear(true, delay);
                    if (ParticleFXManager.Instance != null)
                    {
                        ParticleFXManager.Instance.PlayCellBlast(m_Cells[x, targetY].transform.position, m_Cells[x, targetY].CurrentColor, delay);
                    }
                    delay += 0.015f;
                    cleared++;
                }
            }
            for (int y = 0; y < BoardHeight; y++)
            {
                if (m_Cells[targetX, y].IsOccupied)
                {
                    m_Cells[targetX, y].Clear(true, delay);
                    if (ParticleFXManager.Instance != null)
                    {
                        ParticleFXManager.Instance.PlayCellBlast(m_Cells[targetX, y].transform.position, m_Cells[targetX, y].CurrentColor, delay);
                    }
                    delay += 0.015f;
                    cleared++;
                }
            }

            HapticManager.TriggerHeavy();
            if (SoundManager.Instance != null) SoundManager.Instance.PlayCannon();
            ShakeCamera(0.22f, 0.18f);
            Vector3 centerPos = GetCellWorldPosition(targetX, targetY);
            FloatingText.Create(centerPos, "CANNON STRIKE!", new Color(0.25f, 0.85f, 1.0f), 7f);

            int bonus = cleared * 25;
            if (bonus > 0)
            {
                GameManager.Instance.AddScore(bonus, centerPos);
                GameManager.Instance.AddCoins(cleared * 2, true);
            }

            if (ShapeSpawner.Instance != null) ShapeSpawner.Instance.CheckMoveAvailability();
        }

        public void ExecuteArrow(int targetX, int targetY)
        {
            if (m_Cells[targetX, targetY].IsOccupied)
            {
                m_Cells[targetX, targetY].Clear(true, 0f);
                HapticManager.TriggerMedium();
                if (SoundManager.Instance != null) SoundManager.Instance.PlayArrow();
                Vector3 centerPos = GetCellWorldPosition(targetX, targetY);
                if (ParticleFXManager.Instance != null)
                {
                    ParticleFXManager.Instance.PlayCellBlast(centerPos, new Color(1.0f, 0.85f, 0.2f), 0f);
                }
                FloatingText.Create(centerPos, "ARROW POP!", new Color(1.0f, 0.85f, 0.2f), 6f);
                GameManager.Instance.AddScore(20, centerPos);
                GameManager.Instance.AddCoins(5, true);

                if (ShapeSpawner.Instance != null) ShapeSpawner.Instance.CheckMoveAvailability();
            }
        }

        public void ReviveClear()
        {
            float delay = 0f;
            for (int y = 2; y <= 6; y++)
            {
                for (int x = 2; x <= 6; x++)
                {
                    if (m_Cells[x, y].IsOccupied)
                    {
                        m_Cells[x, y].Clear(true, delay, false);
                        delay += 0.015f;
                    }
                }
            }
            HapticManager.TriggerHeavy();
            ShakeCamera(0.25f, 0.2f);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayBomb();
        }

        public void ShakeCamera(float duration, float magnitude)
        {
            if (m_MainCamera == null) m_MainCamera = Camera.main;
            if (m_MainCamera == null) return;

            if (m_ShakeCoroutine != null) StopCoroutine(m_ShakeCoroutine);
            m_ShakeCoroutine = StartCoroutine(AnimateCameraShake(duration, magnitude));
        }

        private IEnumerator AnimateCameraShake(float duration, float magnitude)
        {
            Vector3 originalPos = new Vector3(0f, 0f, -10f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damp = 1f - (elapsed / duration);
                float x = (Random.value * 2f - 1f) * magnitude * damp;
                float y = (Random.value * 2f - 1f) * magnitude * damp;
                m_MainCamera.transform.position = originalPos + new Vector3(x, y, 0f);
                yield return null;
            }

            m_MainCamera.transform.position = originalPos;
        }

        public bool CanAnyShapeFit(List<ShapeData> availableShapes)
        {
            if (availableShapes == null || availableShapes.Count == 0) return true;

            for (int s = 0; s < availableShapes.Count; s++)
            {
                ShapeData shape = availableShapes[s];
                if (shape == null) continue;

                for (int x = 0; x <= BoardWidth - shape.Width; x++)
                {
                    for (int y = 0; y <= BoardHeight - shape.Height; y++)
                    {
                        if (CanShapeFit(shape, x, y))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void ResetGrid()
        {
            ClearPreview();
            for (int x = 0; x < BoardWidth; x++)
            {
                for (int y = 0; y < BoardHeight; y++)
                {
                    if (m_Cells[x, y] != null)
                    {
                        m_Cells[x, y].Clear(false, 0f, false);
                    }
                }
            }
        }

        public void LoadPattern(int[,] pattern, GemType[,] gems = null)
        {
            if (pattern == null) return;

            int pWidth = pattern.GetLength(0);
            int pHeight = pattern.GetLength(1);

            for (int x = 0; x < BoardWidth && x < pWidth; x++)
            {
                for (int y = 0; y < BoardHeight && y < pHeight; y++)
                {
                    int colIndex = pattern[x, y];
                    if (colIndex > 0)
                    {
                        Color c = SpriteFactory.BlockColors[(colIndex - 1) % SpriteFactory.BlockColors.Length];
                        GemType gem = (gems != null && x < gems.GetLength(0) && y < gems.GetLength(1)) ? gems[x, y] : GemType.None;
                        m_Cells[x, y].Fill(c, gem, false);
                    }
                }
            }
        }

        public int GetOccupiedCount()
        {
            int count = 0;
            for (int x = 0; x < BoardWidth; x++)
            {
                for (int y = 0; y < BoardHeight; y++)
                {
                    if (m_Cells[x, y] != null && m_Cells[x, y].IsOccupied)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public void RefreshAllCellSprites()
        {
            if (m_Cells == null) return;
            for (int x = 0; x < BoardWidth; x++)
            {
                for (int y = 0; y < BoardHeight; y++)
                {
                    if (m_Cells[x, y] != null && m_Cells[x, y].IsOccupied)
                    {
                        m_Cells[x, y].RefreshBlockSprite();
                    }
                }
            }
        }
    }
}
