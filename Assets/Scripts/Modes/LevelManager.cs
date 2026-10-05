using System.Collections.Generic;
using UnityEngine;

namespace BoxBlast
{
    public enum DefeatReason
    {
        OutOfMoves,
        BoardFull
    }

    /// <summary>
    /// Master controller for Adventure, Event, and Task modes.
    /// Manages level goals, moves limit, victory/defeat evaluation, and persistent progression.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        public GameModeType CurrentMode { get; private set; } = GameModeType.Adventure;
        public int CurrentLevelNumber { get; private set; } = 1;
        public LevelData CurrentLevel { get; private set; }

        public int MovesRemaining { get; private set; }
        public int LinesCleared { get; private set; }
        public int ColorsCollected { get; private set; }
        public Dictionary<GemType, int> CollectedGems { get; private set; } = new Dictionary<GemType, int>();
        public bool IsLevelActive { get; private set; }
        public bool IsLevelCompleted { get; private set; }

        public const int MaxExtraMovesPerLevel = 3;
        public int ExtraMovesUsed { get; private set; } = 0;
        public int ExtraMovesRemaining => Mathf.Max(0, MaxExtraMovesPerLevel - ExtraMovesUsed);
        public bool CanAddExtraMoves => ExtraMovesRemaining > 0;

        private const string PrefsPrefix = "BoxBlast_Progression_";

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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartLevel(GameModeType mode, int levelNum)
        {
            CurrentMode = mode;
            int maxLvl = (mode == GameModeType.Adventure) ? 40 : 20;
            CurrentLevelNumber = Mathf.Clamp(levelNum, 1, maxLvl);

            if (!IsLevelUnlocked(mode, CurrentLevelNumber))
            {
                Debug.LogWarning($"Level {CurrentLevelNumber} in {mode} is locked!");
                return;
            }

            CurrentLevel = LevelDatabase.GetLevel(mode, CurrentLevelNumber);

            MovesRemaining = CurrentLevel.MovesLimit;
            LinesCleared = 0;
            ColorsCollected = 0;
            CollectedGems.Clear();
            ExtraMovesUsed = 0;
            // CRITICAL: Level is inactive during setup so cleanup/resets cannot trigger gameplay events
            IsLevelActive = false;
            IsLevelCompleted = false;

            // 1. Reset Board & Load Pattern with Embedded Gems
            if (GridManager.Instance != null)
            {
                GridManager.Instance.ResetGrid();
                GridManager.Instance.LoadPattern(CurrentLevel.InitialPattern, CurrentLevel.InitialGems);
            }

            // 2. Refill bottom dock
            if (ShapeSpawner.Instance != null)
            {
                ShapeSpawner.Instance.SpawnBatch();
            }

            // 3. Reset Score in GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetScoreForLevel();
            }

            // 4. Update Level HUD
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowLevelHUD(CurrentLevel, MovesRemaining, GetGoalProgressText());
                UIManager.Instance.UpdateGemObjectives(CollectedGems, CurrentLevel.TargetGems);
            }

            // 5. Setup 100% complete: Now activate level for player moves!
            IsLevelActive = true;
        }

        public void StopLevel()
        {
            IsLevelActive = false;
            IsLevelCompleted = false;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideLevelHUD();
            }
        }

        public void OnMoveMade()
        {
            if (!IsLevelActive || IsLevelCompleted) return;

            if (CurrentLevel.MovesLimit > 0)
            {
                MovesRemaining--;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateMoves(MovesRemaining);
                }

                // Check victory after move
                if (CheckGoalReached())
                {
                    CompleteLevel();
                    return;
                }

                // Out of moves check
                if (MovesRemaining <= 0)
                {
                    FailLevel(DefeatReason.OutOfMoves);
                    return;
                }
            }
        }

        public void OnLinesCleared(int linesCount, List<Color> clearedBallColors)
        {
            if (!IsLevelActive || IsLevelCompleted) return;

            LinesCleared += linesCount;

            Color targetCol = SpriteFactory.BlockColors[CurrentLevel.TargetColorIndex];
            for (int i = 0; i < clearedBallColors.Count; i++)
            {
                if (IsMatchingColor(clearedBallColors[i], targetCol))
                {
                    ColorsCollected++;
                }
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateGoalProgress(GetGoalProgressText());
            }

            if (CheckGoalReached())
            {
                CompleteLevel();
            }
        }

        public void OnGemCollected(GemType gem, Vector3 worldPos)
        {
            if (!IsLevelActive || IsLevelCompleted || gem == GemType.None) return;

            if (!CollectedGems.ContainsKey(gem)) CollectedGems[gem] = 0;
            CollectedGems[gem]++;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayCoin();
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.FlyGemToHUD(gem, worldPos);
                UIManager.Instance.UpdateGemObjectives(CollectedGems, CurrentLevel?.TargetGems);
                UIManager.Instance.UpdateGoalProgress(GetGoalProgressText());
            }

            if (CheckGoalReached())
            {
                CompleteLevel();
            }
        }

        private bool CheckGoalReached()
        {
            switch (CurrentLevel.GoalType)
            {
                case LevelGoalType.ScoreTarget:
                    return GameManager.Instance != null && GameManager.Instance.CurrentScore >= CurrentLevel.TargetScore;

                case LevelGoalType.ClearLines:
                    return LinesCleared >= CurrentLevel.TargetLines;

                case LevelGoalType.CollectColors:
                    return ColorsCollected >= CurrentLevel.TargetColorCount;

                case LevelGoalType.ClearPattern:
                    return GridManager.Instance != null && GridManager.Instance.GetOccupiedCount() <= 3;

                case LevelGoalType.CollectGems:
                    if (CurrentLevel.TargetGems == null || CurrentLevel.TargetGems.Count == 0) return true;
                    foreach (var kvp in CurrentLevel.TargetGems)
                    {
                        int have = CollectedGems.ContainsKey(kvp.Key) ? CollectedGems[kvp.Key] : 0;
                        if (have < kvp.Value) return false;
                    }
                    return true;
            }
            return false;
        }

        public string GetGoalProgressText()
        {
            if (CurrentLevel == null) return "";

            switch (CurrentLevel.GoalType)
            {
                case LevelGoalType.ScoreTarget:
                    int curScore = (GameManager.Instance != null) ? GameManager.Instance.CurrentScore : 0;
                    return $"Score: {curScore}/{CurrentLevel.TargetScore}";

                case LevelGoalType.ClearLines:
                    return $"Lines: {LinesCleared}/{CurrentLevel.TargetLines}";

                case LevelGoalType.CollectColors:
                    return $"Gems: {ColorsCollected}/{CurrentLevel.TargetColorCount}";

                case LevelGoalType.ClearPattern:
                    int remaining = (GridManager.Instance != null) ? GridManager.Instance.GetOccupiedCount() : 0;
                    return $"Clear Pattern: {remaining} left";

                case LevelGoalType.CollectGems:
                    if (CurrentLevel.TargetGems == null || CurrentLevel.TargetGems.Count == 0) return "COLLECT GEMS";
                    int totalTarget = 0;
                    int totalHave = 0;
                    foreach (var kvp in CurrentLevel.TargetGems)
                    {
                        totalTarget += kvp.Value;
                        int have = CollectedGems.ContainsKey(kvp.Key) ? CollectedGems[kvp.Key] : 0;
                        totalHave += Mathf.Min(have, kvp.Value);
                    }
                    return $"GEMS: {totalHave}/{totalTarget}";
            }
            return "";
        }

        private void CompleteLevel()
        {
            if (IsLevelCompleted) return;
            IsLevelCompleted = true;
            IsLevelActive = false;

            // Calculate Stars (1 to 3 based on remaining moves efficiency)
            int stars = 1;
            float ratio = (CurrentLevel != null && CurrentLevel.MovesLimit > 0) ? (float)MovesRemaining / CurrentLevel.MovesLimit : 0.5f;
            if (ratio >= 0.30f || MovesRemaining >= 5) stars = 3;
            else if (ratio >= 0.12f || MovesRemaining >= 2) stars = 2;

            SaveProgress(CurrentMode, CurrentLevelNumber, stars);
            UnlockLevel(CurrentMode, CurrentLevelNumber + 1);

            SkinInfo unlockedSkin = null;
            if (CurrentMode == GameModeType.Adventure)
            {
                SkinManager.CheckAndUnlockLevelSkin(CurrentLevelNumber, out unlockedSkin);
            }

            HapticManager.TriggerHeavy();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayCombo();
            }

            if (UIManager.Instance != null)
            {
                int maxLvl = (CurrentMode == GameModeType.Adventure) ? 40 : 20;
                UIManager.Instance.ShowLevelCompleteModal(CurrentMode, CurrentLevelNumber, stars, CurrentLevelNumber < maxLvl, unlockedSkin);
            }
        }

        public void FailLevel(DefeatReason reason = DefeatReason.OutOfMoves)
        {
            if (IsLevelCompleted) return;
            IsLevelActive = false;

            HapticManager.TriggerMedium();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayGameOver();
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowLevelFailedModal(CurrentMode, CurrentLevelNumber, reason);
            }
        }

        public bool AddExtraMoves(int extraMoves = 5)
        {
            if (CanAddExtraMoves)
            {
                ExtraMovesUsed++;
                MovesRemaining += extraMoves;
                IsLevelActive = true;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.HideDefeatModal();
                    UIManager.Instance.UpdateMoves(MovesRemaining);
                }

                // If board is full / crowded, clear center 5x5 breathing room!
                if (GridManager.Instance != null && GridManager.Instance.GetOccupiedCount() >= 32)
                {
                    GridManager.Instance.ReviveClear();
                }

                // Immediately refresh all 3 boxes with a fresh, playable batch of shapes!
                if (ShapeSpawner.Instance != null)
                {
                    ShapeSpawner.Instance.SpawnBatch();
                }

                FloatingText.Create(Vector3.zero, $"+{extraMoves} MOVES!", new Color(0.2f, 0.95f, 0.4f), 7.5f);
                return true;
            }
            FloatingText.Create(Vector3.zero, "No Extra Moves Left!", Color.yellow, 6f);
            return false;
        }

        public void NextLevel()
        {
            int maxLvl = (CurrentMode == GameModeType.Adventure) ? 40 : 20;
            if (CurrentLevelNumber < maxLvl)
            {
                StartLevel(CurrentMode, CurrentLevelNumber + 1);
            }
            else
            {
                // Reached end of mode levels
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.OpenLevelSelect(CurrentMode);
                }
            }
        }

        public void RestartCurrentLevel()
        {
            StartLevel(CurrentMode, CurrentLevelNumber);
        }

        #region PlayerPrefs Persistence
        public static bool IsLevelUnlocked(GameModeType mode, int level)
        {
            if (level <= 1) return true;
            // Level is unlocked when the preceding level has been completed (has at least 1 star)
            // or when explicitly unlocked
            if (GetLevelStars(mode, level - 1) > 0) return true;
            string key = $"{PrefsPrefix}{mode}_{level}_unlocked";
            return PlayerPrefs.GetInt(key, 0) == 1;
        }

        public static int GetHighestUnlockedLevel(GameModeType mode)
        {
            int maxLvl = (mode == GameModeType.Adventure) ? 40 : 20;
            for (int i = maxLvl; i >= 1; i--)
            {
                if (IsLevelUnlocked(mode, i)) return i;
            }
            return 1;
        }

        public static void ResetAllProgress(GameModeType mode)
        {
            int maxLvl = (mode == GameModeType.Adventure) ? 40 : 20;
            for (int i = 1; i <= maxLvl; i++)
            {
                PlayerPrefs.DeleteKey($"{PrefsPrefix}{mode}_{i}_unlocked");
                PlayerPrefs.DeleteKey($"{PrefsPrefix}{mode}_{i}_stars");
            }
            PlayerPrefs.Save();
        }

        public static void UnlockLevel(GameModeType mode, int level)
        {
            int maxLvl = (mode == GameModeType.Adventure) ? 40 : 20;
            if (level > maxLvl) return;
            string key = $"{PrefsPrefix}{mode}_{level}_unlocked";
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        public static int GetLevelStars(GameModeType mode, int level)
        {
            string key = $"{PrefsPrefix}{mode}_{level}_stars";
            return PlayerPrefs.GetInt(key, 0);
        }

        public static void SaveProgress(GameModeType mode, int level, int stars)
        {
            string key = $"{PrefsPrefix}{mode}_{level}_stars";
            int previousStars = PlayerPrefs.GetInt(key, 0);
            if (stars > previousStars)
            {
                PlayerPrefs.SetInt(key, stars);
                PlayerPrefs.Save();
            }
        }
        #endregion

        private static bool IsMatchingColor(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.15f && Mathf.Abs(a.g - b.g) < 0.15f && Mathf.Abs(a.b - b.b) < 0.15f;
        }
    }
}
