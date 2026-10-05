using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// Master game controller coordinating score, combo streaks, high scores, 
    /// coin economy, booster inventory, shop transactions, and game restarts.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private const string PrefsBestScore = "BoxBlast_BestScore";
        private const string PrefsCoins = "BoxBlast_Coins";
        private const string PrefsBoosterPrefix = "BoxBlast_Booster_";

        public int CurrentScore { get; private set; }
        public int BestScore { get; private set; }
        public int CurrentCoins { get; private set; }
        public int CurrentCombo { get; private set; }
        public bool IsGameOver { get; private set; }

        public const int MaxRevivesPerGame = 3;
        public int RevivesUsed { get; private set; } = 0;
        public int RevivesRemaining => Mathf.Max(0, MaxRevivesPerGame - RevivesUsed);
        public bool CanRevive => RevivesRemaining > 0;

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

            BestScore = PlayerPrefs.GetInt(PrefsBestScore, 0);
            CurrentCoins = PlayerPrefs.GetInt(PrefsCoins, 200); // 200 initial welcome coins

            // Initialize default boosters if first time playing
            if (!PlayerPrefs.HasKey(PrefsBoosterPrefix + BoosterType.Cannon))
            {
                PlayerPrefs.SetInt(PrefsBoosterPrefix + BoosterType.Cannon, 2);
                PlayerPrefs.SetInt(PrefsBoosterPrefix + BoosterType.Bomb, 2);
                PlayerPrefs.SetInt(PrefsBoosterPrefix + BoosterType.Arrow, 2);
                PlayerPrefs.SetInt(PrefsBoosterPrefix + BoosterType.Shuffle, 2);
                PlayerPrefs.Save();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (LevelManager.Instance != null && LevelManager.Instance.IsLevelActive)
            {
                return;
            }
            RestartGame();
        }

        // ==========================================
        // 🪙 COINS & ECONOMY
        // ==========================================
        public void AddCoins(int amount, bool playSound = true)
        {
            if (amount <= 0) return;
            CurrentCoins += amount;
            PlayerPrefs.SetInt(PrefsCoins, CurrentCoins);
            PlayerPrefs.Save();

            if (playSound && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayCoin();
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCoins(CurrentCoins);
            }
        }

        public bool SpendCoins(int amount)
        {
            if (CurrentCoins >= amount)
            {
                CurrentCoins -= amount;
                PlayerPrefs.SetInt(PrefsCoins, CurrentCoins);
                PlayerPrefs.Save();

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateCoins(CurrentCoins);
                }
                return true;
            }
            return false;
        }

        // ==========================================
        // 💥 BOOSTER INVENTORY & SHOP
        // ==========================================
        public int GetBoosterCount(BoosterType type)
        {
            return PlayerPrefs.GetInt(PrefsBoosterPrefix + type, 0);
        }

        public void AddBooster(BoosterType type, int count = 1)
        {
            int current = GetBoosterCount(type);
            PlayerPrefs.SetInt(PrefsBoosterPrefix + type, current + count);
            PlayerPrefs.Save();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateBoosterBar();
            }
        }

        public bool ConsumeBooster(BoosterType type)
        {
            int count = GetBoosterCount(type);
            if (count > 0)
            {
                PlayerPrefs.SetInt(PrefsBoosterPrefix + type, count - 1);
                PlayerPrefs.Save();

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateBoosterBar();
                }
                return true;
            }
            return false;
        }

        public static int GetBoosterPrice(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Cannon: return 50;
                case BoosterType.Bomb: return 75;
                case BoosterType.Arrow: return 40;
                case BoosterType.Shuffle: return 30;
                default: return 50;
            }
        }

        public bool BuyBooster(BoosterType type)
        {
            int price = GetBoosterPrice(type);
            if (SpendCoins(price))
            {
                AddBooster(type, 1);
                if (SoundManager.Instance != null) SoundManager.Instance.PlayPurchase();
                FloatingText.Create(Vector3.zero, "+1 " + type + "!", new Color(0.2f, 0.95f, 0.4f), 7f);
                return true;
            }
            else
            {
                FloatingText.Create(Vector3.zero, "Not enough coins!", new Color(1.0f, 0.35f, 0.35f), 6f);
                return false;
            }
        }

        public void UseBoosterOrOpenShop(BoosterType type)
        {
            int count = GetBoosterCount(type);

            if (type == BoosterType.Shuffle)
            {
                if (count > 0)
                {
                    ConsumeBooster(BoosterType.Shuffle);
                    if (ShapeSpawner.Instance != null)
                    {
                        ShapeSpawner.Instance.ShuffleShapes();
                    }
                }
                else
                {
                    if (UIManager.Instance != null) UIManager.Instance.OpenBoosterShop();
                }
                return;
            }

            if (count > 0)
            {
                if (GridManager.Instance != null)
                {
                    GridManager.Instance.SelectBooster(type);
                }
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.OpenBoosterShop();
                }
            }
        }

        // ==========================================
        // 🏆 SCORE & LINE CLEAR
        // ==========================================
        public void AddScore(int points, Vector3 worldPos)
        {
            if (IsGameOver) return;

            CurrentScore += points;
            CheckBestScore();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateScore(CurrentScore, BestScore);
            }

            FloatingText.Create(worldPos, $"+{points}", new Color(1f, 0.9f, 0.4f), 5f);
        }

        public void RegisterLineClear(int linesCleared, Vector3 boardCenter)
        {
            if (IsGameOver) return;

            CurrentCombo++;

            int basePoints = linesCleared * 100;
            int multiLineBonus = (linesCleared > 1) ? (linesCleared - 1) * 60 : 0;
            int totalBonus = (basePoints + multiLineBonus) * CurrentCombo;

            CurrentScore += totalBonus;
            CheckBestScore();

            // Award Coins on line clear!
            int coinsEarned = 10 * linesCleared * CurrentCombo;
            AddCoins(coinsEarned, false);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateScore(CurrentScore, BestScore);
            }

            if (CurrentCombo > 1)
            {
                HapticManager.TriggerHeavy();
            }
            else
            {
                HapticManager.TriggerMedium();
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBlast(CurrentCombo);
                if (CurrentCombo > 1)
                {
                    SoundManager.Instance.PlayCombo(CurrentCombo);
                }
            }

            // Simple, clean score popup matching classic puzzle games
            string text = $"+{totalBonus}";
            FloatingText.Create(boardCenter + new Vector3(0f, 0.5f, 0f), text, new Color(1f, 0.90f, 0.35f), 5.5f);
        }

        public void RegisterNoLineClear()
        {
            CurrentCombo = 0;
        }

        private void CheckBestScore()
        {
            if (CurrentScore > BestScore)
            {
                BestScore = CurrentScore;
                PlayerPrefs.SetInt(PrefsBestScore, BestScore);
                PlayerPrefs.Save();
            }
        }

        public void TriggerGameOver()
        {
            if (IsGameOver) return;
            IsGameOver = true;

            bool isNewBest = (CurrentScore == BestScore && CurrentScore > 0);

            HapticManager.TriggerHeavy();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayGameOver();
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOver(CurrentScore, BestScore, isNewBest);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PlayerPrefs.Save();
                if (UIManager.Instance != null && !IsGameOver)
                {
                    UIManager.Instance.OpenSettings();
                }
            }
        }

        public bool ReviveGame()
        {
            if (CanRevive)
            {
                RevivesUsed++;
                IsGameOver = false;
                if (GridManager.Instance != null)
                {
                    GridManager.Instance.ReviveClear();
                }
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.HideGameOver();
                }
                if (ShapeSpawner.Instance != null)
                {
                    ShapeSpawner.Instance.SpawnBatch();
                    ShapeSpawner.Instance.CheckMoveAvailability();
                }
                string status = RevivesRemaining > 0 ? $"REVIVED! ({RevivesRemaining} Left)" : "REVIVED! (Last Revive)";
                FloatingText.Create(Vector3.zero, status, new Color(1f, 0.85f, 0.2f), 8f);
                return true;
            }
            return false;
        }

        public void ResetScoreForLevel()
        {
            RevivesUsed = 0;
            IsGameOver = false;
            CurrentScore = 0;
            CurrentCombo = 0;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideGameOver();
                UIManager.Instance.UpdateScore(CurrentScore, BestScore);
                UIManager.Instance.UpdateCoins(CurrentCoins);
            }
        }

        public void RestartGame()
        {
            RevivesUsed = 0;
            IsGameOver = false;
            CurrentScore = 0;
            CurrentCombo = 0;

            if (LevelManager.Instance != null && LevelManager.Instance.IsLevelActive)
            {
                LevelManager.Instance.StopLevel();
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideGameOver();
                UIManager.Instance.HideLevelHUD();
                UIManager.Instance.UpdateScore(CurrentScore, BestScore);
                UIManager.Instance.UpdateCoins(CurrentCoins);
                UIManager.Instance.UpdateBoosterBar();
            }

            if (GridManager.Instance != null)
            {
                GridManager.Instance.ResetGrid();
            }

            if (ShapeSpawner.Instance != null)
            {
                ShapeSpawner.Instance.SpawnBatch();
            }
        }
    }
}
