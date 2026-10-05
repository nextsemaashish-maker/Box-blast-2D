using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// Master controller for dynamic, cinematic line blast animations.
    /// Features sweeping glowing neon laser beams, traveling energy shockwaves,
    /// directional plasma spark sprays, and lens flare supernova intersections.
    /// Provides toggle between the Dynamic Animated Version (V2) and Classic Version (V1).
    /// </summary>
    public class LineBlastAnimator : MonoBehaviour
    {
        public static LineBlastAnimator Instance { get; private set; }

        private const string PrefsKey = "BoxBlast_BlastFX_Dynamic";
        public bool IsAnimationEnabled { get; private set; } = true;

        // Pooled visual objects
        private class BeamItem
        {
            public GameObject obj;
            public Transform transform;
            public SpriteRenderer renderer;
            public bool inUse;
        }

        private class StarFlareItem
        {
            public GameObject obj;
            public Transform transform;
            public SpriteRenderer renderer;
            public bool inUse;
        }

        private const int BeamPoolSize = 16;
        private const int StarPoolSize = 16;
        private readonly List<BeamItem> m_BeamPool = new List<BeamItem>(BeamPoolSize);
        private readonly List<StarFlareItem> m_StarPool = new List<StarFlareItem>(StarPoolSize);

        private Sprite m_LaserBeamSprite;
        private Sprite m_LensFlareStarSprite;

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

            IsAnimationEnabled = PlayerPrefs.GetInt(PrefsKey, 1) == 1;

            m_LaserBeamSprite = SpriteFactory.GetLaserBeamSprite();
            m_LensFlareStarSprite = SpriteFactory.GetLensFlareStarSprite();

            InitializePools();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool ToggleAnimationMode()
        {
            IsAnimationEnabled = !IsAnimationEnabled;
            PlayerPrefs.SetInt(PrefsKey, IsAnimationEnabled ? 1 : 0);
            PlayerPrefs.Save();
            return IsAnimationEnabled;
        }

        private void InitializePools()
        {
            // 1. Beam Pool
            for (int i = 0; i < BeamPoolSize; i++)
            {
                GameObject obj = new GameObject($"BlastBeam_{i}");
                obj.transform.SetParent(transform, false);
                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = m_LaserBeamSprite;
                sr.sortingOrder = 32; // On top of board & blocks
                obj.SetActive(false);

                m_BeamPool.Add(new BeamItem
                {
                    obj = obj,
                    transform = obj.transform,
                    renderer = sr,
                    inUse = false
                });
            }

            // 2. Star Flare Pool
            for (int i = 0; i < StarPoolSize; i++)
            {
                GameObject obj = new GameObject($"StarFlare_{i}");
                obj.transform.SetParent(transform, false);
                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = m_LensFlareStarSprite;
                sr.sortingOrder = 34; // Above laser beam
                obj.SetActive(false);

                m_StarPool.Add(new StarFlareItem
                {
                    obj = obj,
                    transform = obj.transform,
                    renderer = sr,
                    inUse = false
                });
            }
        }

        // ========================================================
        // ⚡ LINE BLAST SEQUENCER
        // ========================================================

        /// <summary>
        /// Orchestrates the entire line blast animation across rows, columns, and 3x3 sectors.
        /// </summary>
        public void PlayLineBlastSequence(
            List<int> fullRows,
            List<int> fullCols,
            List<Vector2Int> fullSectors,
            GridCell[,] cells,
            int boardWidth,
            int boardHeight,
            Vector3 gridCenter)
        {
            int totalClears = fullRows.Count + fullCols.Count + fullSectors.Count;
            if (totalClears <= 0) return;

            // Trigger intersections if both row and col cleared simultaneously
            if (fullRows.Count > 0 && fullCols.Count > 0)
            {
                foreach (int rowY in fullRows)
                {
                    foreach (int colX in fullCols)
                    {
                        if (cells[colX, rowY] != null)
                        {
                            Vector3 interPos = cells[colX, rowY].transform.position;
                            StartCoroutine(AnimateIntersectionFlare(interPos, 0.08f));
                        }
                    }
                }
            }

            // 1. Animate Rows (Horizontal laser beam sweep)
            float rowDelay = 0f;
            foreach (int y in fullRows)
            {
                List<GridCell> rowCells = new List<GridCell>(boardWidth);
                Color dominantColor = Color.cyan;
                for (int x = 0; x < boardWidth; x++)
                {
                    if (cells[x, y] != null)
                    {
                        rowCells.Add(cells[x, y]);
                        dominantColor = cells[x, y].CurrentColor;
                    }
                }

                StartCoroutine(AnimateRowBlast(y, rowCells, dominantColor, rowDelay));
                rowDelay += 0.045f;
            }

            // 2. Animate Columns (Vertical laser beam sweep)
            float colDelay = (fullRows.Count > 0) ? 0.035f : 0f;
            foreach (int x in fullCols)
            {
                List<GridCell> colCells = new List<GridCell>(boardHeight);
                Color dominantColor = Color.yellow;
                for (int y = 0; y < boardHeight; y++)
                {
                    if (cells[x, y] != null)
                    {
                        colCells.Add(cells[x, y]);
                        dominantColor = cells[x, y].CurrentColor;
                    }
                }

                StartCoroutine(AnimateColBlast(x, colCells, dominantColor, colDelay));
                colDelay += 0.045f;
            }

            // 3. Animate 3x3 Sectors
            if (fullSectors.Count > 0)
            {
                foreach (Vector2Int sector in fullSectors)
                {
                    StartCoroutine(AnimateSectorCells(sector, cells));
                }
            }
        }

        // ========================================================
        // 🌈 ROW BLAST (HORIZONTAL SWEEP)
        // ========================================================
        private IEnumerator AnimateRowBlast(int rowY, List<GridCell> rowCells, Color beamColor, float startDelay)
        {
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
            if (rowCells == null || rowCells.Count == 0) yield break;

            Vector3 startPos = rowCells[0].transform.position;
            Vector3 endPos = rowCells[rowCells.Count - 1].transform.position;
            Vector3 centerPos = (startPos + endPos) * 0.5f;
            float totalWidth = Vector3.Distance(startPos, endPos) + 0.95f;

            // Spawn laser beam across row
            BeamItem beam = GetAvailableBeam();
            if (beam != null)
            {
                beam.inUse = true;
                beam.transform.position = centerPos;
                beam.transform.rotation = Quaternion.identity;
                // Sprite width is 4 world units (128px / 32ppu)
                float scaleX = totalWidth / 4.0f;
                beam.transform.localScale = new Vector3(scaleX, 0f, 1f);

                Color neonColor = Color.Lerp(beamColor, Color.white, 0.40f);
                neonColor.a = 1.0f;
                beam.renderer.color = neonColor;
                beam.obj.SetActive(true);

                StartCoroutine(AnimateBeamPulse(beam, scaleX, 0.45f, 0.28f));
            }

            // Sequential Wave: pop cells from left to right along row
            float stepDelay = 0.024f;
            for (int i = 0; i < rowCells.Count; i++)
            {
                GridCell cell = rowCells[i];
                if (cell != null && cell.IsOccupied)
                {
                    Color cellColor = cell.CurrentColor;
                    cell.Clear(true, 0f);

                    if (ParticleFXManager.Instance != null)
                    {
                        // Directional plasma sparks spraying UP and DOWN
                        ParticleFXManager.Instance.PlayDirectionalBlast(cell.transform.position, cellColor, Vector2.right);
                    }
                }
                yield return new WaitForSeconds(stepDelay);
            }
        }

        // ========================================================
        // ⚡ COLUMN BLAST (VERTICAL SWEEP)
        // ========================================================
        private IEnumerator AnimateColBlast(int colX, List<GridCell> colCells, Color beamColor, float startDelay)
        {
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
            if (colCells == null || colCells.Count == 0) yield break;

            Vector3 startPos = colCells[0].transform.position;
            Vector3 endPos = colCells[colCells.Count - 1].transform.position;
            Vector3 centerPos = (startPos + endPos) * 0.5f;
            float totalHeight = Vector3.Distance(startPos, endPos) + 0.95f;

            // Spawn vertical laser beam across column
            BeamItem beam = GetAvailableBeam();
            if (beam != null)
            {
                beam.inUse = true;
                beam.transform.position = centerPos;
                beam.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                float scaleY = totalHeight / 4.0f;
                beam.transform.localScale = new Vector3(scaleY, 0f, 1f);

                Color neonColor = Color.Lerp(beamColor, Color.white, 0.40f);
                neonColor.a = 1.0f;
                beam.renderer.color = neonColor;
                beam.obj.SetActive(true);

                StartCoroutine(AnimateBeamPulse(beam, scaleY, 0.45f, 0.28f));
            }

            // Sequential Wave: pop cells from bottom to top along column
            float stepDelay = 0.024f;
            for (int i = 0; i < colCells.Count; i++)
            {
                GridCell cell = colCells[i];
                if (cell != null && cell.IsOccupied)
                {
                    Color cellColor = cell.CurrentColor;
                    cell.Clear(true, 0f);

                    if (ParticleFXManager.Instance != null)
                    {
                        // Directional plasma sparks spraying LEFT and RIGHT
                        ParticleFXManager.Instance.PlayDirectionalBlast(cell.transform.position, cellColor, Vector2.up);
                    }
                }
                yield return new WaitForSeconds(stepDelay);
            }
        }

        // ========================================================
        // 💥 BEAM PULSE & DISSOLVE
        // ========================================================
        private IEnumerator AnimateBeamPulse(BeamItem beam, float lengthScale, float targetThickness, float duration)
        {
            float elapsed = 0f;
            Color baseColor = beam.renderer.color;

            // 1. Instant Flash-Expand (0 to max thickness in ~0.08s)
            float flashDuration = 0.08f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / flashDuration;
                float thick = Mathf.Lerp(0f, targetThickness * 1.25f, p);
                beam.transform.localScale = new Vector3(lengthScale, thick, 1f);
                yield return null;
            }

            // 2. Glowing Dissolve & Collapse
            elapsed = 0f;
            float fadeDuration = duration - flashDuration;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / fadeDuration;

                float thick = Mathf.Lerp(targetThickness * 1.25f, 0f, p * p);
                beam.transform.localScale = new Vector3(lengthScale, thick, 1f);

                Color c = baseColor;
                c.a = Mathf.Clamp01(1f - p);
                beam.renderer.color = c;
                yield return null;
            }

            beam.obj.SetActive(false);
            beam.inUse = false;
        }

        // ========================================================
        // ✨ INTERSECTION SUPERNOVA FLARE
        // ========================================================
        private IEnumerator AnimateIntersectionFlare(Vector3 worldPos, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            StarFlareItem star = GetAvailableStar();
            if (star != null)
            {
                star.inUse = true;
                star.transform.position = worldPos;
                star.transform.localScale = Vector3.zero;
                star.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
                star.renderer.color = new Color(1.0f, 0.95f, 0.60f, 1f);
                star.obj.SetActive(true);

                if (ParticleFXManager.Instance != null)
                {
                    ParticleFXManager.Instance.PlayIntersectionBurst(worldPos, new Color(1f, 0.85f, 0.3f));
                }

                float elapsed = 0f;
                float duration = 0.34f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float p = elapsed / duration;

                    float scale;
                    if (p < 0.35f)
                    {
                        scale = Mathf.Lerp(0f, 2.2f, p / 0.35f);
                    }
                    else
                    {
                        scale = Mathf.Lerp(2.2f, 0f, (p - 0.35f) / 0.65f);
                    }

                    star.transform.localScale = Vector3.one * scale;
                    star.transform.Rotate(0f, 0f, 280f * Time.deltaTime);

                    Color c = star.renderer.color;
                    c.a = Mathf.Clamp01(1f - p);
                    star.renderer.color = c;

                    yield return null;
                }

                star.obj.SetActive(false);
                star.inUse = false;
            }
        }

        // ========================================================
        // 🔮 3x3 SECTOR RIPPLE
        // ========================================================
        private IEnumerator AnimateSectorCells(Vector2Int sector, GridCell[,] cells)
        {
            int startX = sector.x * 3;
            int startY = sector.y * 3;
            float delay = 0.015f;

            for (int dy = 0; dy < 3; dy++)
            {
                for (int dx = 0; dx < 3; dx++)
                {
                    GridCell cell = cells[startX + dx, startY + dy];
                    if (cell != null && cell.IsOccupied)
                    {
                        cell.Clear(true, 0f);
                        if (ParticleFXManager.Instance != null)
                        {
                            ParticleFXManager.Instance.PlayCellBlast(cell.transform.position, cell.CurrentColor, 0f);
                        }
                    }
                    yield return new WaitForSeconds(delay);
                }
            }
        }

        private BeamItem GetAvailableBeam()
        {
            for (int i = 0; i < m_BeamPool.Count; i++)
            {
                if (!m_BeamPool[i].inUse) return m_BeamPool[i];
            }
            return null;
        }

        private StarFlareItem GetAvailableStar()
        {
            for (int i = 0; i < m_StarPool.Count; i++)
            {
                if (!m_StarPool[i].inUse) return m_StarPool[i];
            }
            return null;
        }
    }
}
