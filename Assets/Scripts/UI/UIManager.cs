using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BoxBlast
{
    /// <summary>
    /// Master UI manager controlling Responsive Landscape & Portrait Layouts,
    /// Coin Wallet, Crown Score Card, Far-Right Boosters Sidebar, Booster Shop Modal,
    /// Settings Modal, Level HUDs, and Modals.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private Font m_DefaultFont;
        private Font m_CandyFont;
        private Transform m_RootCanvas;
        private GraphicRaycaster m_Raycaster;

        // HUD Panels
        private GameObject m_BestScoreObj;
        private GameObject m_ScoreCardObj;
        private GameObject m_CoinsWalletObj;
        private GameObject m_SettingsBtnObj;
        private GameObject m_HomeBtnObj;
        private GameObject m_BoosterSidebarObj;
        private GameObject m_ClassicHUD;
        private GameObject m_LevelHUD;
        private CanvasScaler m_CanvasScaler;

        // Score & Coin Texts
        private Text m_ScoreText;
        private Text m_BestScoreText;
        private Text m_CoinsText;
        private Text m_ComboText;

        // Game Over Texts & Buttons
        private Text m_GameOverScoreText;
        private Text m_GameOverBestText;
        private GameObject m_GameOverNewBestObj;
        private GameObject m_GameOverReviveBtn;
        private Text m_GameOverReviveText;
        private GameObject m_GameOverRestartBtn;
        private GameObject m_GameOverHomeBtn;

        // Level Defeat Modal Elements
        private GameObject m_DefeatExtraMovesBtn;
        private Text m_DefeatExtraMovesText;
        private Text m_DefeatTitleText;
        private Text m_DefeatSubtitleText;
        private GameObject m_DefeatRetryBtn;
        private GameObject m_DefeatLvlSelectBtn;
        private GameObject m_DefeatModeBtn;

        // Level In-Game HUD Texts
        private Text m_LevelTitleText;
        private Text m_LevelGoalText;
        private Text m_LevelMovesText;
        private Text m_LevelScoreText;

        // Booster UI Elements
        private readonly Dictionary<BoosterType, (GameObject buttonObj, Text badgeText, Image highlightImg)> m_BoosterButtons =
            new Dictionary<BoosterType, (GameObject, Text, Image)>();

        // Modals & Popups
        private GameObject m_TitleSplashScreen;
        private GameObject m_BoosterShopModal;
        private GameObject m_SettingsModal;
        private GameObject m_ModeSelectPanel;
        private GameObject m_HomeWardrobeBtn;
        private GameObject m_HomeSettingsBtn;
        private GameObject m_LevelSelectPanel;
        private GameObject m_GameOverPanel;
        private GameObject m_VictoryModal;
        private Text m_VictoryTitleText;
        private Text m_VictorySubtitleText;
        private GameObject m_VictoryNextBtnObj;
        private Image[] m_VictoryStarImgs = new Image[3];
        private GameObject m_DefeatModal;

        // Ball Skin Wardrobe & Victory Reward
        private GameObject m_SkinWardrobeModal;
        private Transform m_SkinGridContainer;
        private GameObject m_VictorySkinRewardObj;
        private Image m_VictorySkinPreviewImg;
        private Text m_VictorySkinNameText;
        private Button m_VictorySkinEquipBtn;
        private Text m_VictorySkinEquipBtnText;

        // Settings Dynamic Toggles
        private Image m_SfxBtnImg;
        private Text m_SfxBtnText;
        private Image m_MusicBtnImg;
        private Text m_MusicBtnText;
        private Image m_HapticBtnImg;
        private Text m_HapticBtnText;
        private Image m_BlastFxBtnImg;
        private Text m_BlastFxBtnText;
        private float m_LastToggleTime = 0f;

        // Level & Mode Select Dynamic Elements
        private Transform m_LevelGridContainer;
        private Text m_LevelSelectTitleText;
        private Text m_LevelSelectStarsText;
        private Text m_ModeCardAdvStarsText;
        private Text m_ModeCardEventStarsText;
        private Text m_ModeCardTaskStarsText;
        private Text m_ModeCardClassicBestText;
        private Text m_HomeClassicBadgeText;
        private Text m_HomeAdventureBadgeText;
        private Text m_DailyStreakText;

        // Adventure Gem Objectives in Level HUD
        private GameObject m_GemObjectivesBar;
        private GameObject m_LevelGoalBadgeObj;
        private readonly Dictionary<GemType, (GameObject rootObj, Image iconImg, Text countText, RectTransform iconRt)> m_GemObjectiveItems =
            new Dictionary<GemType, (GameObject, Image, Text, RectTransform)>();

        // Level Select Page Navigation (for 40 Adventure levels)
        private int m_LevelSelectPage = 0;
        private GameObject m_LevelPageNavObj;
        private Text m_LevelPageNavText;

        // Shop Coin Balance Text
        private Text m_ShopCoinsText;

        // Layout Tracking
        private bool m_IsLandscape = true;
        private int m_LastScreenWidth;
        private int m_LastScreenHeight;
        private GameModeType m_ActiveViewingMode = GameModeType.Adventure;

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

            m_CandyFont = Resources.Load<Font>("LilitaOne");
            if (m_CandyFont == null) m_CandyFont = Font.CreateDynamicFontFromOSFont("Comic Sans MS", 36);
            if (m_CandyFont == null) m_CandyFont = Font.CreateDynamicFontFromOSFont("Arial", 36);
            if (m_CandyFont == null) m_CandyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (m_CandyFont == null) m_CandyFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            m_DefaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (m_DefaultFont == null) m_DefaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (m_DefaultFont == null) m_DefaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);

            m_IsLandscape = Screen.width > Screen.height;
            m_LastScreenWidth = Screen.width;
            m_LastScreenHeight = Screen.height;

            BuildFullUI();
        }

        private void Start()
        {
            if (m_TitleSplashScreen != null) m_TitleSplashScreen.SetActive(false);
            if (m_LevelSelectPanel != null) m_LevelSelectPanel.SetActive(false);
            if (m_LevelHUD != null) m_LevelHUD.SetActive(false);
            if (m_VictoryModal != null) m_VictoryModal.SetActive(false);
            if (m_DefeatModal != null) m_DefeatModal.SetActive(false);

            // Open Home Page immediately on game start per user request
            OpenModeSelect();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Auto-detect screen rotation or resize
            if (Screen.width != m_LastScreenWidth || Screen.height != m_LastScreenHeight)
            {
                m_LastScreenWidth = Screen.width;
                m_LastScreenHeight = Screen.height;
                bool landscape = Screen.width > Screen.height;
                if (landscape != m_IsLandscape)
                {
                    ApplyOrientationLayout(landscape);
                }
            }

            // Android Hardware / Gesture Back Button handling
            if (InputHelper.IsBackButtonPressed())
            {
                HandleBackNavigation();
            }
        }

        public void HandleBackNavigation()
        {
            if (m_BoosterShopModal != null && m_BoosterShopModal.activeSelf)
            {
                m_BoosterShopModal.SetActive(false);
                return;
            }
            if (m_SkinWardrobeModal != null && m_SkinWardrobeModal.activeSelf)
            {
                m_SkinWardrobeModal.SetActive(false);
                return;
            }
            if (m_SettingsModal != null && m_SettingsModal.activeSelf)
            {
                m_SettingsModal.SetActive(false);
                return;
            }
            if (m_LevelSelectPanel != null && m_LevelSelectPanel.activeSelf)
            {
                m_LevelSelectPanel.SetActive(false);
                OpenModeSelect();
                return;
            }
            if (m_ModeSelectPanel != null && m_ModeSelectPanel.activeSelf)
            {
                // Already on Home Screen - quit on mobile back
                Application.Quit();
                return;
            }
            if (m_GameOverPanel != null && m_GameOverPanel.activeSelf)
            {
                m_GameOverPanel.SetActive(false);
                OpenModeSelect();
                return;
            }
            if (m_VictoryModal != null && m_VictoryModal.activeSelf)
            {
                m_VictoryModal.SetActive(false);
                OpenModeSelect();
                return;
            }
            if (m_DefeatModal != null && m_DefeatModal.activeSelf)
            {
                m_DefeatModal.SetActive(false);
                OpenModeSelect();
                return;
            }
            if (m_TitleSplashScreen != null && m_TitleSplashScreen.activeSelf)
            {
                Application.Quit();
                return;
            }

            // In gameplay: open settings as pause menu
            OpenSettings();
        }

        // ==========================================
        // 🔄 ORIENTATION & RESPONSIVE LAYOUT
        // ==========================================
        public void ApplyOrientationLayout(bool isLandscape)
        {
            m_IsLandscape = isLandscape;

            if (m_CanvasScaler != null)
            {
                if (isLandscape)
                {
                    m_CanvasScaler.referenceResolution = new Vector2(1920, 1080);
                    m_CanvasScaler.matchWidthOrHeight = 0.5f;
                }
                else
                {
                    m_CanvasScaler.referenceResolution = new Vector2(1080, 1920);
                    m_CanvasScaler.matchWidthOrHeight = 0f;
                }
                m_CanvasScaler.dynamicPixelsPerUnit = 2.5f;
            }

            if (GridManager.Instance != null) GridManager.Instance.UpdateLayout(isLandscape);
            if (ShapeSpawner.Instance != null) ShapeSpawner.Instance.UpdateLayout(isLandscape);

            // Safe Area calculations for mobile notches, dynamic islands, punch holes
            float safeTop = 0f;
            float safeLeft = 0f;
            float safeRight = 0f;
            if (Screen.height > 0 && Screen.width > 0)
            {
                Rect safeArea = Screen.safeArea;
                float topPixels = Screen.height - (safeArea.y + safeArea.height);
                float leftPixels = safeArea.x;
                float rightPixels = Screen.width - (safeArea.x + safeArea.width);

                float refHeight = isLandscape ? 1080f : 1920f;
                float refWidth = isLandscape ? 1920f : 1080f;

                safeTop = (topPixels / Screen.height) * refHeight;
                safeLeft = (leftPixels / Screen.width) * refWidth;
                safeRight = (rightPixels / Screen.width) * refWidth;

                if (Application.isMobilePlatform && !isLandscape)
                {
                    safeTop = Mathf.Max(safeTop, 45f);
                }
            }

            // 1. Best Score (Crown + High Score): Top-Left Corner
            if (m_BestScoreObj != null)
            {
                RectTransform rt = m_BestScoreObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(45 + safeLeft, -(45 + safeTop));
            }

            // 2. Settings Gear: Top-Right Corner
            if (m_SettingsBtnObj != null)
            {
                RectTransform rt = m_SettingsBtnObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-(45 + safeRight), -(45 + safeTop));
            }

            // 3. Current Score (Player's score): Top Center
            if (m_ScoreCardObj != null)
            {
                RectTransform rt = m_ScoreCardObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                float yPos = isLandscape ? -(35 + safeTop) : -(85 + safeTop);
                rt.anchoredPosition = new Vector2(0, yPos);
            }

            // 4. Adventure Level HUD: Top Center
            if (m_LevelHUD != null)
            {
                RectTransform rt = m_LevelHUD.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                float yPos = isLandscape ? -(20 + safeTop) : -(40 + safeTop);
                rt.anchoredPosition = new Vector2(0, yPos);
            }

            // 5. Home Screen Top Utility Buttons (Wardrobe Star & Settings Gear for Android)
            if (m_HomeWardrobeBtn != null)
            {
                RectTransform wbRt = m_HomeWardrobeBtn.GetComponent<RectTransform>();
                float padTop = Mathf.Max(safeTop + 30f, 80f);
                float padSide = Mathf.Max(safeLeft + 35f, 55f);
                wbRt.anchoredPosition = new Vector2(padSide, -padTop);
            }
            if (m_HomeSettingsBtn != null)
            {
                RectTransform stRt = m_HomeSettingsBtn.GetComponent<RectTransform>();
                float padTop = Mathf.Max(safeTop + 30f, 80f);
                float padSide = Mathf.Max(safeRight + 35f, 55f);
                stRt.anchoredPosition = new Vector2(-padSide, -padTop);
            }

            // Ensure other unused HUD items are disabled
            if (m_CoinsWalletObj != null) m_CoinsWalletObj.SetActive(false);
            if (m_HomeBtnObj != null) m_HomeBtnObj.SetActive(false);
            if (m_BoosterSidebarObj != null) m_BoosterSidebarObj.SetActive(false);

            UpdateHUDVisibility();
            ReorganizeBoosterButtons(isLandscape);
        }

        private void ReorganizeBoosterButtons(bool isLandscape)
        {
            BoosterType[] order = { BoosterType.Cannon, BoosterType.Bomb, BoosterType.Arrow, BoosterType.Shuffle };

            if (isLandscape)
            {
                // Vertical stack: Top to bottom
                float startY = 180f;
                float stepY = -120f;

                for (int i = 0; i < order.Length; i++)
                {
                    if (m_BoosterButtons.TryGetValue(order[i], out var item) && item.buttonObj != null)
                    {
                        RectTransform rt = item.buttonObj.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(0, startY + i * stepY);
                    }
                }
            }
            else
            {
                // Horizontal dock: Left to right
                float startX = -210f;
                float stepX = 140f;

                for (int i = 0; i < order.Length; i++)
                {
                    if (m_BoosterButtons.TryGetValue(order[i], out var item) && item.buttonObj != null)
                    {
                        RectTransform rt = item.buttonObj.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(startX + i * stepX, 0);
                    }
                }
            }
        }

        // ==========================================
        // 🏗️ UI CONSTRUCTION
        // ==========================================
        private void BuildFullUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("GameCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                m_CanvasScaler = canvasObj.AddComponent<CanvasScaler>();
                m_CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                m_CanvasScaler.referenceResolution = new Vector2(1920, 1080);
                m_CanvasScaler.matchWidthOrHeight = 0.5f;
                m_CanvasScaler.dynamicPixelsPerUnit = 2.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                m_CanvasScaler = canvas.GetComponent<CanvasScaler>();
                if (m_CanvasScaler != null) m_CanvasScaler.dynamicPixelsPerUnit = 2.5f;
            }

            m_RootCanvas = canvas.transform;
            m_Raycaster = canvas.GetComponent<GraphicRaycaster>();

            // 1. Top-Left: Best Score (👑 + Gold Score matching Image 1)
            BuildBestScoreHUD();

            // 2. Top-Right: Settings Gear Button (Clean gear icon matching Image 1)
            BuildSettingsButton();

            // 3. Top-Center: Current Score (Big White Bold Number matching Image 1)
            BuildScoreCard();

            // 4. Modals & Screens
            BuildSettingsModal();
            BuildClassicGameOverModal();
            BuildModeSelectScreen();
            BuildLevelSelectScreen();
            BuildLevelHUD();
            BuildVictoryModal();
            BuildDefeatModal();
            BuildSkinWardrobeModal();

            m_IsLandscape = Screen.width > Screen.height;
            ApplyOrientationLayout(m_IsLandscape);
            UpdateHUDVisibility();
        }

        private void BuildBestScoreHUD()
        {
            m_BestScoreObj = new GameObject("BestScoreTopLeft");
            m_BestScoreObj.transform.SetParent(m_RootCanvas, false);

            RectTransform rt = m_BestScoreObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(260, 60);

            // Crown Icon (Golden crown matching Image 1)
            GameObject crownIcon = new GameObject("CrownIcon");
            crownIcon.transform.SetParent(m_BestScoreObj.transform, false);
            RectTransform cIconRt = crownIcon.AddComponent<RectTransform>();
            cIconRt.anchorMin = new Vector2(0f, 0.5f);
            cIconRt.anchorMax = new Vector2(0f, 0.5f);
            cIconRt.pivot = new Vector2(0f, 0.5f);
            cIconRt.anchoredPosition = new Vector2(0, 0);
            cIconRt.sizeDelta = new Vector2(48, 48);
            Image cIconImg = crownIcon.AddComponent<Image>();
            cIconImg.sprite = GemIconFactory.GetCrownSprite();
            cIconImg.raycastTarget = false;

            // Best Score Value (Bold gold/yellow text matching Image 1)
            int best = (GameManager.Instance != null) ? GameManager.Instance.BestScore : 0;
            GameObject bestObj = CreateText(m_BestScoreObj.transform, new Vector2(56, 0), new Vector2(200, 56), best.ToString("N0"), 42, FontStyle.Bold, new Color(1.0f, 0.82f, 0.15f), TextAnchor.MiddleLeft);
            RectTransform btRt = bestObj.GetComponent<RectTransform>();
            btRt.anchorMin = new Vector2(0f, 0.5f);
            btRt.anchorMax = new Vector2(0f, 0.5f);
            btRt.pivot = new Vector2(0f, 0.5f);
            btRt.anchoredPosition = new Vector2(56, 0);
            m_BestScoreText = bestObj.GetComponent<Text>();
        }

        private void BuildSettingsButton()
        {
            m_SettingsBtnObj = new GameObject("SettingsTopButton");
            m_SettingsBtnObj.transform.SetParent(m_RootCanvas, false);

            RectTransform rt = m_SettingsBtnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(72, 72);

            // Transparent touch target for effortless tapping
            Image bg = m_SettingsBtnObj.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.001f);
            bg.raycastTarget = true;

            Button btn = m_SettingsBtnObj.AddComponent<Button>();
            btn.onClick.AddListener(OpenSettings);

            // Crisp Gear Icon matching Image 1
            GameObject iconObj = new GameObject("GearIcon");
            iconObj.transform.SetParent(m_SettingsBtnObj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(58, 58);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = GemIconFactory.GetGearSprite();
            iconImg.color = new Color(0.88f, 0.94f, 1.0f, 0.95f);
            iconImg.raycastTarget = false;
        }

        private void BuildHomeButton()
        {
            // Removed from gameplay per user request
        }

        private void BuildCoinsWallet()
        {
            // Removed from gameplay per user request
        }

        private void BuildScoreCard()
        {
            m_ScoreCardObj = new GameObject("CurrentScoreCenter");
            m_ScoreCardObj.transform.SetParent(m_RootCanvas, false);

            RectTransform rt = m_ScoreCardObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(400, 110);

            // Big White Current Score matching Image 1
            int score = (GameManager.Instance != null) ? GameManager.Instance.CurrentScore : 0;
            GameObject scoreObj = CreateText(m_ScoreCardObj.transform, Vector2.zero, new Vector2(400, 110), score.ToString("N0"), 76, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            m_ScoreText = scoreObj.GetComponent<Text>();

            // Combo Notification Popup
            GameObject comboObj = CreateText(m_RootCanvas, new Vector2(0, 280), new Vector2(600, 90), "", 46, FontStyle.Bold, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleCenter);
            m_ComboText = comboObj.GetComponent<Text>();
            comboObj.SetActive(false);
        }

        private void BuildBoosterSidebar()
        {
            // Boosters removed per user request
        }

        private void CreateBoosterButton(BoosterType type, Sprite iconSprite, string label)
        {
            GameObject btnObj = new GameObject($"Booster_{type}");
            btnObj.transform.SetParent(m_BoosterSidebarObj.transform, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(98, 98);

            // Rounded Squircle Outer Rim & Highlight
            Image highlight = btnObj.AddComponent<Image>();
            highlight.sprite = SpriteFactory.GetRoundedButtonSprite(new Color(0.10f, 0.16f, 0.30f, 0.95f), new Color(0.32f, 0.50f, 0.82f, 1.0f));

            // Button trigger
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.UseBoosterOrOpenShop(type);
            });

            // Center Icon (Ultra-sharp HD vector icon)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(80, 80);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.raycastTarget = false;

            // Pill Badge (Underneath)
            GameObject badgeObj = new GameObject("Badge");
            badgeObj.transform.SetParent(btnObj.transform, false);
            RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
            badgeRt.anchoredPosition = new Vector2(0, -44);
            badgeRt.sizeDelta = new Vector2(72, 28);
            Image badgeBg = badgeObj.AddComponent<Image>();
            badgeBg.sprite = SpriteFactory.GetPillBadgeSprite(new Color(0.12f, 0.78f, 0.32f));
            badgeBg.raycastTarget = false;

            GameObject badgeTxtObj = CreateText(badgeObj.transform, Vector2.zero, new Vector2(72, 28), "2", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Text badgeTxt = badgeTxtObj.GetComponent<Text>();

            m_BoosterButtons[type] = (btnObj, badgeTxt, highlight);
        }

        public void UpdateBoosterBar()
        {
            foreach (var kvp in m_BoosterButtons)
            {
                BoosterType type = kvp.Key;
                int count = (GameManager.Instance != null) ? GameManager.Instance.GetBoosterCount(type) : 0;

                if (kvp.Value.badgeText != null)
                {
                    kvp.Value.badgeText.text = (count > 0) ? $"{count}" : "SHOP";
                }
            }
        }

        public void UpdateBoosterSelection(BoosterType activeBooster)
        {
            foreach (var kvp in m_BoosterButtons)
            {
                if (kvp.Value.highlightImg != null)
                {
                    if (kvp.Key == activeBooster && activeBooster != BoosterType.None)
                    {
                        // Glowing golden highlight
                        kvp.Value.highlightImg.sprite = SpriteFactory.GetRoundedButtonSprite(new Color(0.18f, 0.22f, 0.38f, 1f), new Color(1.0f, 0.85f, 0.20f, 1f), 0.12f);
                    }
                    else
                    {
                        // Normal blue
                        kvp.Value.highlightImg.sprite = SpriteFactory.GetRoundedButtonSprite(new Color(0.10f, 0.16f, 0.30f, 0.95f), new Color(0.32f, 0.50f, 0.82f, 1.0f));
                    }
                }
            }
        }

        // ==========================================
        // 🛒 BOOSTER SHOP MODAL
        // ==========================================
        private void BuildBoosterShopModal()
        {
            m_BoosterShopModal = new GameObject("BoosterShopModal");
            m_BoosterShopModal.transform.SetParent(m_RootCanvas, false);

            RectTransform rt = m_BoosterShopModal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

            // Dim backdrop
            Image bg = m_BoosterShopModal.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.12f, 0.92f);

            // Dialog Window
            GameObject winObj = new GameObject("ShopDialog");
            winObj.transform.SetParent(m_BoosterShopModal.transform, false);
            RectTransform winRt = winObj.AddComponent<RectTransform>();
            winRt.sizeDelta = new Vector2(980, 720);
            Image winBg = winObj.AddComponent<Image>();
            winBg.sprite = SpriteFactory.GetPanelSprite(new Color(0.08f, 0.14f, 0.28f, 0.98f), new Color(0.24f, 0.40f, 0.68f, 1f), 2.5f, 20);
            winBg.type = Image.Type.Sliced;

            // Title Banner
            GameObject titleObj = CreateText(winObj.transform, new Vector2(0, 300), new Vector2(700, 90), "BOOSTER SHOP", 64, FontStyle.Bold, new Color(1.0f, 0.84f, 0.25f), TextAnchor.MiddleCenter);

            // Coins Balance Header Pill
            GameObject balanceObj = new GameObject("CoinBalance");
            balanceObj.transform.SetParent(winObj.transform, false);
            RectTransform balRt = balanceObj.AddComponent<RectTransform>();
            balRt.anchoredPosition = new Vector2(0, 220);
            balRt.sizeDelta = new Vector2(300, 60);
            Image balBg = balanceObj.AddComponent<Image>();
            balBg.color = new Color(0.12f, 0.22f, 0.40f);

            int coins = (GameManager.Instance != null) ? GameManager.Instance.CurrentCoins : 200;
            GameObject balTxtObj = CreateText(balanceObj.transform, Vector2.zero, new Vector2(300, 60), $"{coins:N0} COINS", 30, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            m_ShopCoinsText = balTxtObj.GetComponent<Text>();

            // 4 Shop Cards in a row
            BoosterType[] shopTypes = { BoosterType.Cannon, BoosterType.Bomb, BoosterType.Arrow, BoosterType.Shuffle };
            Sprite[] shopIcons = { GemIconFactory.GetCannonSprite(), GemIconFactory.GetBombSprite(), GemIconFactory.GetArrowSprite(), GemIconFactory.GetShuffleSprite() };
            string[] shopTitles = { "CANNON", "SUPER BOMB", "ARROW", "SHUFFLE" };
            string[] shopDescs = { "Blasts a full\nrow or column", "Blasts a 3x3\nboard area", "Removes any\none block", "Shuffle three\nfresh shapes" };

            float startX = -345f;
            float stepX = 230f;

            for (int i = 0; i < 4; i++)
            {
                BoosterType bType = shopTypes[i];
                int price = GameManager.GetBoosterPrice(bType);

                GameObject cardObj = new GameObject($"Card_{bType}");
                cardObj.transform.SetParent(winObj.transform, false);
                RectTransform cardRt = cardObj.AddComponent<RectTransform>();
                cardRt.anchoredPosition = new Vector2(startX + i * stepX, 20);
                cardRt.sizeDelta = new Vector2(210, 310);
                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.color = new Color(0.12f, 0.20f, 0.38f);

                // Title
                CreateText(cardObj.transform, new Vector2(0, 115), new Vector2(210, 45), shopTitles[i], 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

                // Icon
                GameObject icon = new GameObject("Icon");
                icon.transform.SetParent(cardObj.transform, false);
                RectTransform iRt = icon.AddComponent<RectTransform>();
                iRt.anchoredPosition = new Vector2(0, 35);
                iRt.sizeDelta = new Vector2(100, 100);
                Image iImg = icon.AddComponent<Image>();
                iImg.sprite = shopIcons[i];
                iImg.raycastTarget = false;

                // Description
                CreateText(cardObj.transform, new Vector2(0, -45), new Vector2(210, 50), shopDescs[i], 18, FontStyle.Normal, new Color(0.85f, 0.90f, 1f, 0.85f), TextAnchor.MiddleCenter);

                // BUY Button
                GameObject buyBtn = CreateButton(cardObj.transform, new Vector2(0, -105), new Vector2(180, 60), new Color(0.14f, 0.72f, 0.32f), $"BUY {price}", 24);
                buyBtn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null)
                    {
                        if (GameManager.Instance.BuyBooster(bType))
                        {
                            UpdateCoins(GameManager.Instance.CurrentCoins);
                        }
                    }
                });
            }

            // Free Coins Option at Bottom: "FREE +50 COINS"
            GameObject adBtn = CreateButton(winObj.transform, new Vector2(0, -220), new Vector2(520, 80), new Color(1.0f, 0.70f, 0.10f), "FREE +50 COINS (WATCH AD)", 30);
            adBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddCoins(50, true);
                    UpdateCoins(GameManager.Instance.CurrentCoins);
                    FloatingText.Create(Vector3.zero, "+50 COINS!", new Color(1f, 0.9f, 0.2f), 7f);
                }
            });

            // Close Button (X)
            GameObject closeBtn = CreateButton(winObj.transform, new Vector2(430, 310), new Vector2(60, 60), new Color(0.85f, 0.25f, 0.25f), "X", 30);
            closeBtn.GetComponent<Button>().onClick.AddListener(() => m_BoosterShopModal.SetActive(false));

            m_BoosterShopModal.SetActive(false);
        }

        public void OpenBoosterShop()
        {
            if (m_BoosterShopModal != null)
            {
                m_BoosterShopModal.SetActive(true);
                if (m_ShopCoinsText != null && GameManager.Instance != null)
                {
                    m_ShopCoinsText.text = $"{GameManager.Instance.CurrentCoins:N0} COINS";
                }
            }
        }

        // ==========================================
        // ⚙️ SETTINGS MODAL
        // ==========================================
        private void BuildSettingsModal()
        {
            m_SettingsModal = new GameObject("SettingsModal");
            m_SettingsModal.transform.SetParent(m_RootCanvas, false);

            RectTransform rt = m_SettingsModal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

            Image bg = m_SettingsModal.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.12f, 0.92f);

            GameObject winObj = new GameObject("SettingsDialog");
            winObj.transform.SetParent(m_SettingsModal.transform, false);
            RectTransform winRt = winObj.AddComponent<RectTransform>();
            winRt.sizeDelta = new Vector2(680, 890);
            Image winBg = winObj.AddComponent<Image>();
            winBg.sprite = SpriteFactory.GetPanelSprite(new Color(0.08f, 0.14f, 0.28f, 0.98f), new Color(0.24f, 0.40f, 0.68f, 1f), 2.5f, 20);
            winBg.type = Image.Type.Sliced;

            CreateText(winObj.transform, new Vector2(0, 375), new Vector2(500, 60), "SETTINGS", 52, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

            // Sound FX Button
            GameObject sfxBtn = CreateButton(winObj.transform, new Vector2(0, 295), new Vector2(480, 60), new Color(0.12f, 0.62f, 0.32f), "SOUND EFFECTS: ON", 25);
            m_SfxBtnImg = sfxBtn.GetComponent<Image>();
            m_SfxBtnText = sfxBtn.GetComponentInChildren<Text>();
            sfxBtn.GetComponent<Button>().onClick.AddListener(OnToggleSfxClicked);

            // Music Button
            GameObject musicBtn = CreateButton(winObj.transform, new Vector2(0, 225), new Vector2(480, 60), new Color(0.12f, 0.62f, 0.32f), "MUSIC: ON", 25);
            m_MusicBtnImg = musicBtn.GetComponent<Image>();
            m_MusicBtnText = musicBtn.GetComponentInChildren<Text>();
            musicBtn.GetComponent<Button>().onClick.AddListener(OnToggleMusicClicked);

            // Mobile Vibration / Haptics Button
            GameObject hapticBtn = CreateButton(winObj.transform, new Vector2(0, 155), new Vector2(480, 60), new Color(0.12f, 0.62f, 0.32f), "VIBRATION: ON", 25);
            m_HapticBtnImg = hapticBtn.GetComponent<Image>();
            m_HapticBtnText = hapticBtn.GetComponentInChildren<Text>();
            hapticBtn.GetComponent<Button>().onClick.AddListener(OnToggleHapticsClicked);

            // Blast Animation Version Toggle (Dynamic New vs Classic)
            GameObject blastFxBtn = CreateButton(winObj.transform, new Vector2(0, 85), new Vector2(480, 60), new Color(0.12f, 0.48f, 0.88f), "BLAST ANIMATION: DYNAMIC (NEW)", 24);
            m_BlastFxBtnImg = blastFxBtn.GetComponent<Image>();
            m_BlastFxBtnText = blastFxBtn.GetComponentInChildren<Text>();
            blastFxBtn.GetComponent<Button>().onClick.AddListener(OnToggleBlastFxClicked);

            // Ball Designs Wardrobe Button
            GameObject skinBtn = CreateButton(winObj.transform, new Vector2(0, 15), new Vector2(480, 60), new Color(0.55f, 0.25f, 0.85f), "BALL DESIGNS", 24);
            skinBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_SettingsModal.SetActive(false);
                OpenSkinWardrobe();
            });

            // Restart Game Button
            GameObject restartBtn = CreateButton(winObj.transform, new Vector2(0, -55), new Vector2(480, 60), new Color(0.85f, 0.35f, 0.20f), "RESTART GAME", 25);
            restartBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_SettingsModal.SetActive(false);
                if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            });

            // Home Button
            GameObject homeBtn = CreateIconButtonWithText(winObj.transform, new Vector2(0, -125), new Vector2(480, 60), new Color(0.18f, 0.42f, 0.82f), GemIconFactory.GetHomeSprite(), "HOME", 25);
            homeBtn.GetComponent<Button>().onClick.AddListener(OnHomeClicked);

            // Resume Game Button
            GameObject closeBtn = CreateButton(winObj.transform, new Vector2(0, -210), new Vector2(340, 64), new Color(0.25f, 0.65f, 0.40f), "RESUME GAME", 26);
            closeBtn.GetComponent<Button>().onClick.AddListener(() => m_SettingsModal.SetActive(false));

            // Close (X) Button
            GameObject xBtn = CreateButton(winObj.transform, new Vector2(285, 375), new Vector2(56, 56), new Color(0.85f, 0.25f, 0.25f), "X", 28);
            xBtn.GetComponent<Button>().onClick.AddListener(() => m_SettingsModal.SetActive(false));

            RefreshSettingsUI();
            m_SettingsModal.SetActive(false);
        }

        private void OnHomeClicked()
        {
            if (m_SettingsModal != null) m_SettingsModal.SetActive(false);
            OpenModeSelect();
        }

        private void OnToggleSfxClicked()
        {
            if (Time.unscaledTime - m_LastToggleTime < 0.25f) return;
            m_LastToggleTime = Time.unscaledTime;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ToggleSFX();
            }
            else
            {
                bool cur = PlayerPrefs.GetInt("BoxBlast_SFX", 1) == 1;
                PlayerPrefs.SetInt("BoxBlast_SFX", cur ? 0 : 1);
                PlayerPrefs.Save();
            }
            RefreshSettingsUI();
        }

        private void OnToggleMusicClicked()
        {
            if (Time.unscaledTime - m_LastToggleTime < 0.25f) return;
            m_LastToggleTime = Time.unscaledTime;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ToggleMusic();
            }
            else
            {
                bool cur = PlayerPrefs.GetInt("BoxBlast_Music", 1) == 1;
                PlayerPrefs.SetInt("BoxBlast_Music", cur ? 0 : 1);
                PlayerPrefs.Save();
            }
            RefreshSettingsUI();
        }

        private void OnToggleHapticsClicked()
        {
            if (Time.unscaledTime - m_LastToggleTime < 0.25f) return;
            m_LastToggleTime = Time.unscaledTime;

            HapticManager.ToggleHaptics();
            RefreshSettingsUI();
        }

        private void RefreshSettingsUI()
        {
            bool sfxOn = (SoundManager.Instance != null) ? SoundManager.Instance.IsSfxEnabled : (PlayerPrefs.GetInt("BoxBlast_SFX", 1) == 1);
            if (m_SfxBtnText != null)
            {
                m_SfxBtnText.text = sfxOn ? "SOUND EFFECTS: ON" : "SOUND EFFECTS: OFF";
                m_SfxBtnText.color = sfxOn ? Color.white : new Color(0.80f, 0.85f, 0.92f, 0.75f);
            }
            if (m_SfxBtnImg != null)
            {
                m_SfxBtnImg.sprite = SpriteFactory.GetRoundedButtonSprite(
                    sfxOn ? new Color(0.12f, 0.62f, 0.32f, 1f) : new Color(0.24f, 0.28f, 0.38f, 1f),
                    sfxOn ? new Color(0.30f, 0.88f, 0.50f, 1f) : new Color(0.40f, 0.45f, 0.58f, 0.6f),
                    0.05f
                );
            }

            bool musicOn = (SoundManager.Instance != null) ? SoundManager.Instance.IsMusicEnabled : (PlayerPrefs.GetInt("BoxBlast_Music", 1) == 1);
            if (m_MusicBtnText != null)
            {
                m_MusicBtnText.text = musicOn ? "MUSIC: ON" : "MUSIC: OFF";
                m_MusicBtnText.color = musicOn ? Color.white : new Color(0.80f, 0.85f, 0.92f, 0.75f);
            }
            if (m_MusicBtnImg != null)
            {
                m_MusicBtnImg.sprite = SpriteFactory.GetRoundedButtonSprite(
                    musicOn ? new Color(0.12f, 0.62f, 0.32f, 1f) : new Color(0.24f, 0.28f, 0.38f, 1f),
                    musicOn ? new Color(0.30f, 0.88f, 0.50f, 1f) : new Color(0.40f, 0.45f, 0.58f, 0.6f),
                    0.05f
                );
            }

            bool hapticOn = HapticManager.IsHapticsEnabled;
            if (m_HapticBtnText != null)
            {
                m_HapticBtnText.text = hapticOn ? "VIBRATION: ON" : "VIBRATION: OFF";
                m_HapticBtnText.color = hapticOn ? Color.white : new Color(0.80f, 0.85f, 0.92f, 0.75f);
            }
            if (m_HapticBtnImg != null)
            {
                m_HapticBtnImg.sprite = SpriteFactory.GetRoundedButtonSprite(
                    hapticOn ? new Color(0.12f, 0.62f, 0.32f, 1f) : new Color(0.24f, 0.28f, 0.38f, 1f),
                    hapticOn ? new Color(0.30f, 0.88f, 0.50f, 1f) : new Color(0.40f, 0.45f, 0.58f, 0.6f),
                    0.05f
                );
            }

            bool blastDynamic = (LineBlastAnimator.Instance != null) ? LineBlastAnimator.Instance.IsAnimationEnabled : (PlayerPrefs.GetInt("BoxBlast_BlastFX_Dynamic", 1) == 1);
            if (m_BlastFxBtnText != null)
            {
                m_BlastFxBtnText.text = blastDynamic ? "BLAST ANIMATION: DYNAMIC (NEW)" : "BLAST ANIMATION: CLASSIC";
                m_BlastFxBtnText.color = blastDynamic ? Color.white : new Color(0.80f, 0.85f, 0.92f, 0.75f);
            }
            if (m_BlastFxBtnImg != null)
            {
                m_BlastFxBtnImg.sprite = SpriteFactory.GetRoundedButtonSprite(
                    blastDynamic ? new Color(0.12f, 0.48f, 0.88f, 1f) : new Color(0.24f, 0.28f, 0.38f, 1f),
                    blastDynamic ? new Color(0.35f, 0.75f, 1.0f, 1f) : new Color(0.40f, 0.45f, 0.58f, 0.6f),
                    0.05f
                );
            }
        }

        private void OnToggleBlastFxClicked()
        {
            if (Time.unscaledTime - m_LastToggleTime < 0.25f) return;
            m_LastToggleTime = Time.unscaledTime;

            bool isDynamic = true;
            if (LineBlastAnimator.Instance != null)
            {
                isDynamic = LineBlastAnimator.Instance.ToggleAnimationMode();
            }

            HapticManager.TriggerLight();
            FloatingText.Create(Vector3.zero, isDynamic ? "Blast FX: Dynamic (New)" : "Blast FX: Classic", Color.yellow, 6f);
            RefreshSettingsUI();
        }

        public void OpenSettings()
        {
            if (m_SettingsModal == null) BuildSettingsModal();
            RefreshSettingsUI();
            if (m_SettingsModal != null)
            {
                m_SettingsModal.SetActive(true);
                m_SettingsModal.transform.SetAsLastSibling();
            }
        }

        // ==========================================
        // 🚀 TITLE & SPLASH SCREEN
        // ==========================================
        private void BuildTitleSplashScreen()
        {
            m_TitleSplashScreen = new GameObject("TitleSplashScreen");
            m_TitleSplashScreen.transform.SetParent(m_RootCanvas, false);

            RectTransform rt = m_TitleSplashScreen.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            // Fullscreen backdrop
            Image bg = m_TitleSplashScreen.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.07f, 0.14f, 0.98f);
            bg.raycastTarget = true;

            // Tap anywhere on background advances to mode select
            Button tapBtn = m_TitleSplashScreen.AddComponent<Button>();
            tapBtn.onClick.AddListener(OnTitleScreenTapped);

            // Centered Main Content Dialog Card
            GameObject cardObj = new GameObject("TitleDialog");
            cardObj.transform.SetParent(m_TitleSplashScreen.transform, false);
            RectTransform cardRt = cardObj.AddComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(700, 780);
            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = SpriteFactory.GetPanelSprite(new Color(0.07f, 0.12f, 0.22f, 0.98f), new Color(0.25f, 0.45f, 0.80f, 1f), 3f, 24);
            cardBg.type = Image.Type.Sliced;
            cardBg.raycastTarget = false;

            // 1. Welcome Pill Badge
            GameObject pillObj = new GameObject("WelcomeBadge");
            pillObj.transform.SetParent(cardObj.transform, false);
            RectTransform pRt = pillObj.AddComponent<RectTransform>();
            pRt.anchoredPosition = new Vector2(0, 310);
            pRt.sizeDelta = new Vector2(380, 44);
            Image pBg = pillObj.AddComponent<Image>();
            pBg.sprite = SpriteFactory.GetPillBadgeSprite(new Color(0.12f, 0.24f, 0.48f, 0.9f));
            pBg.raycastTarget = false;

            CreateText(pillObj.transform, Vector2.zero, new Vector2(380, 44), "WELCOME PUZZLE MASTER", 20, FontStyle.Bold, new Color(1.0f, 0.85f, 0.25f), TextAnchor.MiddleCenter);

            // 2. Big Vibrant Game Title
            CreateText(cardObj.transform, new Vector2(0, 225), new Vector2(660, 95), "BOX BLAST 2D", 68, FontStyle.Bold, new Color(1.0f, 0.88f, 0.25f), TextAnchor.MiddleCenter);

            // 3. Subtitle Tagline
            CreateText(cardObj.transform, new Vector2(0, 155), new Vector2(600, 40), "MATCH - BLAST - SOLVE", 24, FontStyle.Bold, new Color(0.55f, 0.82f, 1.0f), TextAnchor.MiddleCenter);

            // 4. Center Golden Trophy / Crown Emblem
            GameObject emblemObj = new GameObject("CenterEmblem");
            emblemObj.transform.SetParent(cardObj.transform, false);
            RectTransform eRt = emblemObj.AddComponent<RectTransform>();
            eRt.anchoredPosition = new Vector2(0, 15);
            eRt.sizeDelta = new Vector2(170, 170);

            Image eBg = emblemObj.AddComponent<Image>();
            eBg.sprite = SpriteFactory.GetRoundedButtonSprite(new Color(0.10f, 0.16f, 0.32f, 0.95f), new Color(1.0f, 0.84f, 0.25f, 1f), 0.08f);
            eBg.raycastTarget = false;

            GameObject iconObj = new GameObject("EmblemIcon");
            iconObj.transform.SetParent(emblemObj.transform, false);
            RectTransform iRt = iconObj.AddComponent<RectTransform>();
            iRt.sizeDelta = new Vector2(120, 120);
            Image iImg = iconObj.AddComponent<Image>();
            iImg.sprite = GemIconFactory.GetCrownSprite();
            iImg.raycastTarget = false;

            // 5. Feature Highlights
            CreateText(cardObj.transform, new Vector2(0, -115), new Vector2(620, 50), "4 Game Modes  -  80+ Levels  -  Juicy Combos", 22, FontStyle.Normal, new Color(0.78f, 0.85f, 0.98f, 0.9f), TextAnchor.MiddleCenter);

            // 6. Big Radiant Start Button
            GameObject startBtn = CreateButton(cardObj.transform, new Vector2(0, -210), new Vector2(460, 80), new Color(0.12f, 0.72f, 0.35f), "START GAME", 32);
            startBtn.GetComponent<Button>().onClick.AddListener(OnTitleScreenTapped);

            // 7. Hint text
            CreateText(cardObj.transform, new Vector2(0, -310), new Vector2(500, 36), "Tap anywhere to continue", 18, FontStyle.Normal, new Color(0.55f, 0.65f, 0.80f, 0.7f), TextAnchor.MiddleCenter);

            m_TitleSplashScreen.SetActive(false);
        }

        public void ShowTitleSplashScreen()
        {
            if (m_TitleSplashScreen != null)
            {
                m_TitleSplashScreen.SetActive(true);
                m_TitleSplashScreen.transform.SetAsLastSibling();
            }
        }

        public void HideTitleSplashScreen()
        {
            if (m_TitleSplashScreen != null)
            {
                m_TitleSplashScreen.SetActive(false);
            }
        }

        public void OnTitleScreenTapped()
        {
            HideTitleSplashScreen();
            OpenModeSelect();
        }

        public void OpenLevelSelect(GameModeType mode)
        {
            if (m_LevelSelectPanel == null) BuildLevelSelectScreen();
            m_ActiveViewingMode = mode;
            int currentLevel = (LevelManager.Instance != null && LevelManager.Instance.CurrentMode == mode) ? LevelManager.Instance.CurrentLevelNumber : 1;
            m_LevelSelectPage = Mathf.Clamp((currentLevel - 1) / 20, 0, (mode == GameModeType.Adventure ? 1 : 0));
            RefreshLevelSelectGrid(mode);
            if (m_LevelSelectPanel != null)
            {
                m_LevelSelectPanel.SetActive(true);
                m_LevelSelectPanel.transform.SetAsLastSibling();
            }
            UpdateHUDVisibility();
        }

        public void OpenModeSelect()
        {
            if (m_ModeSelectPanel == null) BuildModeSelectScreen();
            RefreshModeCardStats();
            if (m_ModeSelectPanel != null)
            {
                m_ModeSelectPanel.SetActive(true);
                m_ModeSelectPanel.transform.SetAsLastSibling();
            }
            UpdateHUDVisibility();
        }

        public int GetTotalModeStars(GameModeType mode)
        {
            int total = 0;
            int maxLvl = (mode == GameModeType.Adventure) ? 40 : 20;
            for (int i = 1; i <= maxLvl; i++)
            {
                total += LevelManager.GetLevelStars(mode, i);
            }
            return total;
        }

        private void RefreshModeCardStats()
        {
            int bestScore = (GameManager.Instance != null) ? GameManager.Instance.BestScore : PlayerPrefs.GetInt("BoxBlast_BestScore", 0);
            int dailyStreak = PlayerPrefs.GetInt("BoxBlast_DailyStreak", 0);
            if (m_DailyStreakText != null)
            {
                m_DailyStreakText.text = $"×{dailyStreak}";
            }
            if (m_HomeClassicBadgeText != null)
            {
                m_HomeClassicBadgeText.text = bestScore > 0 ? $"👑 {bestScore:N0}" : "PLAY ▶";
                m_HomeClassicBadgeText.color = bestScore > 0 ? new Color(1.0f, 0.88f, 0.25f) : Color.white;
            }
            if (m_HomeAdventureBadgeText != null)
            {
                int currentAdvLvl = LevelManager.GetHighestUnlockedLevel(GameModeType.Adventure);
                m_HomeAdventureBadgeText.text = $"LVL {currentAdvLvl} ▶";
            }
            if (m_ModeCardClassicBestText != null && GameManager.Instance != null)
            {
                m_ModeCardClassicBestText.text = $"Best: {GameManager.Instance.BestScore:N0}";
            }
            if (m_ModeCardAdvStarsText != null)
            {
                m_ModeCardAdvStarsText.text = $"{GetTotalModeStars(GameModeType.Adventure)} / 120 Stars";
            }
            if (m_ModeCardEventStarsText != null)
            {
                m_ModeCardEventStarsText.text = $"{GetTotalModeStars(GameModeType.Event)} / 60 Stars";
            }
            if (m_ModeCardTaskStarsText != null)
            {
                m_ModeCardTaskStarsText.text = $"{GetTotalModeStars(GameModeType.Task)} / 60 Stars";
            }
        }

        // ==========================================
        // 📊 HUD & LEVEL SUPPORT
        // ==========================================
        public void UpdateScore(int currentScore, int bestScore)
        {
            if (m_ScoreText != null) m_ScoreText.text = currentScore.ToString("N0");
            if (m_BestScoreText != null) m_BestScoreText.text = bestScore.ToString("N0");
            if (m_LevelScoreText != null) m_LevelScoreText.text = $"SCORE: {currentScore:N0}";
        }

        public void UpdateCoins(int currentCoins)
        {
            if (m_CoinsText != null) m_CoinsText.text = currentCoins.ToString("N0");
            if (m_ShopCoinsText != null) m_ShopCoinsText.text = $"{currentCoins:N0} COINS";
        }

        public void ShowCombo(int comboCount, int bonusPoints)
        {
            // Kept simple: combo text disabled per user preference
        }

        private void HideCombo()
        {
            if (m_ComboText != null) m_ComboText.gameObject.SetActive(false);
        }

        public void UpdateHUDVisibility()
        {
            bool isHomeOpen = (m_ModeSelectPanel != null && m_ModeSelectPanel.activeSelf);
            bool isLevelSelectOpen = (m_LevelSelectPanel != null && m_LevelSelectPanel.activeSelf);
            bool isLevel = (LevelManager.Instance != null && LevelManager.Instance.IsLevelActive);

            bool showGameplayHUD = !isHomeOpen && !isLevelSelectOpen;

            if (m_BestScoreObj != null) m_BestScoreObj.SetActive(showGameplayHUD && !isLevel);
            if (m_SettingsBtnObj != null) m_SettingsBtnObj.SetActive(showGameplayHUD);
            if (m_ScoreCardObj != null) m_ScoreCardObj.SetActive(showGameplayHUD && !isLevel);
            if (m_LevelHUD != null) m_LevelHUD.SetActive(showGameplayHUD && isLevel);
            if (m_CoinsWalletObj != null) m_CoinsWalletObj.SetActive(false);
            if (m_HomeBtnObj != null) m_HomeBtnObj.SetActive(false);
            if (m_BoosterSidebarObj != null) m_BoosterSidebarObj.SetActive(false);
        }

        public void ShowLevelHUD(LevelData level, int moves, string goalText)
        {
            if (m_LevelHUD != null)
            {
                if (m_LevelTitleText != null) m_LevelTitleText.text = $"{level.Mode.ToString().ToUpper()} - LVL {level.LevelNumber}";
                if (m_LevelMovesText != null) m_LevelMovesText.text = (level.MovesLimit > 0) ? $"MOVES: {moves}" : "MOVES: --";
                if (m_LevelGoalText != null) m_LevelGoalText.text = goalText;
                if (m_LevelScoreText != null && GameManager.Instance != null)
                {
                    m_LevelScoreText.text = $"SCORE: {GameManager.Instance.CurrentScore:N0}";
                }

                bool isGemLevel = (level.GoalType == LevelGoalType.CollectGems && level.TargetGems != null && level.TargetGems.Count > 0);
                if (m_LevelGoalBadgeObj != null) m_LevelGoalBadgeObj.SetActive(!isGemLevel);
                if (m_GemObjectivesBar != null) m_GemObjectivesBar.SetActive(isGemLevel);
            }
            UpdateHUDVisibility();
        }

        public void HideLevelHUD()
        {
            UpdateHUDVisibility();
        }

        public void UpdateMoves(int movesRemaining)
        {
            if (m_LevelMovesText != null)
            {
                m_LevelMovesText.text = $"MOVES: {movesRemaining}";
                if (movesRemaining <= 3 && movesRemaining > 0)
                {
                    m_LevelMovesText.color = new Color(1.0f, 0.35f, 0.35f);
                    StartCoroutine(PunchScale(m_LevelMovesText.rectTransform, 1.22f, 0.18f));
                }
                else
                {
                    m_LevelMovesText.color = new Color(1.0f, 0.88f, 0.25f);
                }
            }
        }

        public void UpdateGoalProgress(string goalProgressText)
        {
            if (m_LevelGoalText != null) m_LevelGoalText.text = goalProgressText;
        }

        public void UpdateGemObjectives(Dictionary<GemType, int> collected, Dictionary<GemType, int> targets)
        {
            if (m_GemObjectivesBar == null) return;

            if (targets == null || targets.Count == 0)
            {
                m_GemObjectivesBar.SetActive(false);
                if (m_LevelGoalBadgeObj != null) m_LevelGoalBadgeObj.SetActive(true);
                return;
            }

            m_GemObjectivesBar.SetActive(true);
            if (m_LevelGoalBadgeObj != null) m_LevelGoalBadgeObj.SetActive(false);

            List<GemType> activeGems = new List<GemType>();
            GemType[] allGems = new GemType[] { GemType.BlueDiamond, GemType.OrangePentagon, GemType.YellowStar, GemType.RedRuby };
            for (int i = 0; i < allGems.Length; i++)
            {
                if (targets.ContainsKey(allGems[i]) && targets[allGems[i]] > 0)
                {
                    activeGems.Add(allGems[i]);
                }
            }

            float slotSpacing = 175f;
            if (activeGems.Count == 3) slotSpacing = 220f;
            else if (activeGems.Count == 2) slotSpacing = 240f;
            else if (activeGems.Count <= 1) slotSpacing = 0f;

            float totalSpan = (activeGems.Count - 1) * slotSpacing;
            float startX = -totalSpan * 0.5f;

            for (int i = 0; i < allGems.Length; i++)
            {
                GemType g = allGems[i];
                if (!m_GemObjectiveItems.ContainsKey(g)) continue;

                var item = m_GemObjectiveItems[g];
                int activeIndex = activeGems.IndexOf(g);
                if (activeIndex >= 0)
                {
                    item.rootObj.SetActive(true);
                    float posX = startX + activeIndex * slotSpacing;
                    item.rootObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(posX, 0);

                    int targetVal = targets[g];
                    int currentVal = (collected != null && collected.ContainsKey(g)) ? collected[g] : 0;
                    int remaining = Mathf.Max(0, targetVal - currentVal);

                    if (remaining == 0)
                    {
                        item.countText.text = "✓";
                        item.countText.color = new Color(0.25f, 1f, 0.50f);
                        item.countText.fontSize = 32;
                    }
                    else
                    {
                        item.countText.text = remaining.ToString();
                        item.countText.color = Color.white;
                        item.countText.fontSize = 26;
                    }
                }
                else
                {
                    item.rootObj.SetActive(false);
                }
            }
        }

        public void FlyGemToHUD(GemType gem, Vector3 worldStartPos)
        {
            if (m_RootCanvas == null || gem == GemType.None) return;
            StartCoroutine(AnimateFlyingGem(gem, worldStartPos));
        }

        private System.Collections.IEnumerator AnimateFlyingGem(GemType gem, Vector3 worldStartPos)
        {
            GameObject flyObj = new GameObject($"FlyGem_{gem}");
            flyObj.transform.SetParent(m_RootCanvas, false);
            RectTransform rt = flyObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48, 48);

            Image img = flyObj.AddComponent<Image>();
            img.sprite = GemIconFactory.GetGemSprite(gem);
            img.raycastTarget = false;

            Vector3 targetScreenPos = new Vector3(Screen.width * 0.5f, Screen.height - 80f, 0f);
            if (m_GemObjectiveItems.ContainsKey(gem) && m_GemObjectiveItems[gem].iconRt != null && m_GemObjectiveItems[gem].rootObj.activeInHierarchy)
            {
                targetScreenPos = m_GemObjectiveItems[gem].iconRt.position;
            }

            Vector3 startScreenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(worldStartPos) : worldStartPos;
            startScreenPos.z = 0f;
            targetScreenPos.z = 0f;

            RectTransform canvasRt = m_RootCanvas.GetComponent<RectTransform>();
            Vector2 startCanvasPos, targetCanvasPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, startScreenPos, null, out startCanvasPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, targetScreenPos, null, out targetCanvasPos);

            rt.anchoredPosition = startCanvasPos;
            rt.localScale = Vector3.one * 1.3f;

            float duration = 0.5f;
            float elapsed = 0f;
            Vector2 midPoint = (startCanvasPos + targetCanvasPos) * 0.5f + new Vector2(Random.Range(-40f, 40f), 70f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeT = t * t * (3f - 2f * t);

                Vector2 pos = (1f - easeT) * (1f - easeT) * startCanvasPos + 2f * (1f - easeT) * easeT * midPoint + easeT * easeT * targetCanvasPos;
                rt.anchoredPosition = pos;

                float scale = Mathf.Lerp(1.3f, 0.9f, easeT);
                rt.localScale = new Vector3(scale, scale, 1f);

                yield return null;
            }

            Destroy(flyObj);

            if (m_GemObjectiveItems.ContainsKey(gem) && m_GemObjectiveItems[gem].iconRt != null)
            {
                StartCoroutine(PunchScale(m_GemObjectiveItems[gem].iconRt, 1.4f, 0.2f));
            }
        }

        private System.Collections.IEnumerator PunchScale(RectTransform targetRt, float punchScale, float duration)
        {
            if (targetRt == null) yield break;
            Vector3 originalScale = Vector3.one;
            float half = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                if (targetRt == null) yield break;
                float t = elapsed / half;
                targetRt.localScale = Vector3.Lerp(originalScale, originalScale * punchScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                if (targetRt == null) yield break;
                float t = elapsed / half;
                targetRt.localScale = Vector3.Lerp(originalScale * punchScale, originalScale, t);
                yield return null;
            }

            if (targetRt != null) targetRt.localScale = originalScale;
        }

        public void ShowGameOver(int finalScore, int bestScore, bool isNewBest)
        {
            if (m_GameOverPanel == null) BuildClassicGameOverModal();
            if (m_GameOverScoreText != null) m_GameOverScoreText.text = $"{finalScore:N0}";
            if (m_GameOverBestText != null) m_GameOverBestText.text = $"BEST: {bestScore:N0}";
            if (m_GameOverNewBestObj != null) m_GameOverNewBestObj.SetActive(isNewBest);

            bool canRevive = (GameManager.Instance != null && GameManager.Instance.CanRevive);
            int remaining = (GameManager.Instance != null) ? GameManager.Instance.RevivesRemaining : 0;

            if (m_GameOverReviveBtn != null)
            {
                m_GameOverReviveBtn.SetActive(canRevive);
                if (canRevive && m_GameOverReviveText != null)
                {
                    m_GameOverReviveText.text = remaining == 1 ? "REVIVE (1 CHANCE LEFT)" : $"REVIVE ({remaining} CHANCES LEFT)";
                }
            }

            if (m_GameOverRestartBtn != null)
            {
                RectTransform rt = m_GameOverRestartBtn.GetComponent<RectTransform>();
                rt.anchoredPosition = canRevive ? new Vector2(0, -128) : new Vector2(0, -70);
            }
            if (m_GameOverHomeBtn != null)
            {
                RectTransform rt = m_GameOverHomeBtn.GetComponent<RectTransform>();
                rt.anchoredPosition = canRevive ? new Vector2(0, -206) : new Vector2(0, -150);
            }

            if (m_GameOverPanel != null)
            {
                m_GameOverPanel.SetActive(true);
                m_GameOverPanel.transform.SetAsLastSibling();
            }
        }

        public void HideGameOver()
        {
            if (m_GameOverPanel != null) m_GameOverPanel.SetActive(false);
        }

        public void ShowLevelCompleteModal(GameModeType mode, int levelNum, int stars, bool hasNextLevel, SkinInfo unlockedSkin = null)
        {
            if (m_VictoryModal == null) BuildVictoryModal();
            if (m_VictoryModal != null)
            {
                if (m_VictoryTitleText != null)
                {
                    m_VictoryTitleText.text = $"LEVEL {levelNum} CLEAR!";
                }
                if (m_VictorySubtitleText != null)
                {
                    int totalLvl = (mode == GameModeType.Adventure) ? 40 : 20;
                    m_VictorySubtitleText.text = hasNextLevel ? $"Earned {stars} Star{(stars > 1 ? "s" : "")}! Level {levelNum + 1} Unlocked" : $"All {totalLvl} Levels Completed! Master Champion!";
                }
                if (m_VictoryNextBtnObj != null)
                {
                    m_VictoryNextBtnObj.SetActive(hasNextLevel);
                }
                if (m_VictoryStarImgs != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (m_VictoryStarImgs[i] != null)
                        {
                            m_VictoryStarImgs[i].sprite = GemIconFactory.GetStarSprite(i < stars);
                        }
                    }
                }

                // Ball Skin Reward Showcase
                if (m_VictorySkinRewardObj != null)
                {
                    if (unlockedSkin != null)
                    {
                        m_VictorySkinRewardObj.SetActive(true);
                        if (m_VictorySkinPreviewImg != null)
                        {
                            m_VictorySkinPreviewImg.sprite = SpriteFactory.GetBlockSprite(new Color(0.18f, 0.50f, 0.95f), unlockedSkin.Type);
                        }
                        if (m_VictorySkinNameText != null)
                        {
                            m_VictorySkinNameText.text = $"{unlockedSkin.Name.ToUpper()} BALL UNLOCKED!";
                        }
                        if (m_VictorySkinEquipBtn != null)
                        {
                            m_VictorySkinEquipBtn.onClick.RemoveAllListeners();
                            m_VictorySkinEquipBtn.onClick.AddListener(() =>
                            {
                                SkinManager.EquipSkin(unlockedSkin.Type);
                                if (m_VictorySkinEquipBtnText != null) m_VictorySkinEquipBtnText.text = "EQUIPPED";
                                m_VictorySkinEquipBtn.interactable = false;
                            });
                            if (m_VictorySkinEquipBtnText != null) m_VictorySkinEquipBtnText.text = "EQUIP NOW";
                            m_VictorySkinEquipBtn.interactable = true;
                        }
                    }
                    else
                    {
                        m_VictorySkinRewardObj.SetActive(false);
                    }
                }

                m_VictoryModal.SetActive(true);
                m_VictoryModal.transform.SetAsLastSibling();

                // Celebratory Confetti & Fireworks
                if (ParticleFXManager.Instance != null)
                {
                    ParticleFXManager.Instance.PlayComboCelebration(Vector3.zero, 5);
                }
            }
        }

        public void ShowLevelFailedModal(GameModeType mode, int levelNum, DefeatReason reason = DefeatReason.OutOfMoves)
        {
            if (m_DefeatModal == null) BuildDefeatModal();
            if (m_DefeatModal != null)
            {
                if (reason == DefeatReason.BoardFull)
                {
                    if (m_DefeatTitleText != null)
                    {
                        m_DefeatTitleText.text = "NO SPACE LEFT!";
                        m_DefeatTitleText.color = new Color(1.0f, 0.60f, 0.20f);
                    }
                    if (m_DefeatSubtitleText != null)
                    {
                        m_DefeatSubtitleText.text = "No pieces can fit on the board. Don't give up!";
                    }
                }
                else
                {
                    if (m_DefeatTitleText != null)
                    {
                        m_DefeatTitleText.text = "OUT OF MOVES!";
                        m_DefeatTitleText.color = new Color(0.96f, 0.35f, 0.35f);
                    }
                    if (m_DefeatSubtitleText != null)
                    {
                        m_DefeatSubtitleText.text = "You ran out of moves. Don't give up! Try again.";
                    }
                }

                bool canExtra = (LevelManager.Instance != null && LevelManager.Instance.CanAddExtraMoves);
                int remaining = (LevelManager.Instance != null) ? LevelManager.Instance.ExtraMovesRemaining : 0;

                if (m_DefeatExtraMovesBtn != null)
                {
                    m_DefeatExtraMovesBtn.SetActive(canExtra);
                    if (canExtra && m_DefeatExtraMovesText != null)
                    {
                        m_DefeatExtraMovesText.text = $"+5 MOVES ({remaining} LEFT)";
                    }
                }

                if (m_DefeatRetryBtn != null)
                {
                    RectTransform rt = m_DefeatRetryBtn.GetComponent<RectTransform>();
                    rt.anchoredPosition = canExtra ? new Vector2(0, -2) : new Vector2(0, 50);
                }
                if (m_DefeatLvlSelectBtn != null)
                {
                    RectTransform rt = m_DefeatLvlSelectBtn.GetComponent<RectTransform>();
                    rt.anchoredPosition = canExtra ? new Vector2(0, -82) : new Vector2(0, -30);
                }
                if (m_DefeatModeBtn != null)
                {
                    RectTransform rt = m_DefeatModeBtn.GetComponent<RectTransform>();
                    rt.anchoredPosition = canExtra ? new Vector2(0, -160) : new Vector2(0, -108);
                }

                m_DefeatModal.SetActive(true);
                m_DefeatModal.transform.SetAsLastSibling();
            }
        }

        public void HideDefeatModal()
        {
            if (m_DefeatModal != null)
            {
                m_DefeatModal.SetActive(false);
            }
        }

        private void BuildLevelHUD()
        {
            m_LevelHUD = new GameObject("LevelHUD");
            m_LevelHUD.transform.SetParent(m_RootCanvas, false);

            RectTransform hudRt = m_LevelHUD.AddComponent<RectTransform>();
            hudRt.anchorMin = new Vector2(0.5f, 1f);
            hudRt.anchorMax = new Vector2(0.5f, 1f);
            hudRt.pivot = new Vector2(0.5f, 1f);
            hudRt.anchoredPosition = new Vector2(0f, -40f);
            hudRt.sizeDelta = new Vector2(800, 168);

            // Sleek Rounded Card Background
            Image bg = m_LevelHUD.AddComponent<Image>();
            bg.sprite = SpriteFactory.GetPanelSprite(new Color(0.07f, 0.12f, 0.25f, 0.98f), new Color(0.28f, 0.50f, 0.88f, 1f), 2.5f, 20);
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            // Top Row Left: Mode & Level Name
            GameObject titleObj = CreateText(m_LevelHUD.transform, new Vector2(-260, 46), new Vector2(250, 40), "ADVENTURE - LVL 1", 24, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            m_LevelTitleText = titleObj.GetComponent<Text>();

            // Top Row Center: Moves Pill Badge
            GameObject movesBadge = new GameObject("MovesBadge");
            movesBadge.transform.SetParent(m_LevelHUD.transform, false);
            RectTransform mRt = movesBadge.AddComponent<RectTransform>();
            mRt.anchoredPosition = new Vector2(0, 46);
            mRt.sizeDelta = new Vector2(190, 42);
            Image mBg = movesBadge.AddComponent<Image>();
            mBg.sprite = SpriteFactory.GetPillBadgeSprite(new Color(0.55f, 0.38f, 0.08f, 0.95f));
            mBg.type = Image.Type.Sliced;
            mBg.raycastTarget = false;

            GameObject movesObj = CreateText(movesBadge.transform, Vector2.zero, new Vector2(185, 40), "MOVES: 25", 21, FontStyle.Bold, new Color(1.0f, 0.90f, 0.30f), TextAnchor.MiddleCenter);
            m_LevelMovesText = movesObj.GetComponent<Text>();

            // Top Row Right: Level Score
            int score = (GameManager.Instance != null) ? GameManager.Instance.CurrentScore : 0;
            GameObject lScoreObj = CreateText(m_LevelHUD.transform, new Vector2(260, 46), new Vector2(240, 40), $"SCORE: {score:N0}", 22, FontStyle.Bold, new Color(1.0f, 0.85f, 0.25f), TextAnchor.MiddleRight);
            m_LevelScoreText = lScoreObj.GetComponent<Text>();

            // Bottom Row: Target Goal Pill Badge (Used when not collecting gems e.g. line clear goals)
            GameObject goalBadge = new GameObject("GoalBadge");
            goalBadge.transform.SetParent(m_LevelHUD.transform, false);
            RectTransform gRt = goalBadge.AddComponent<RectTransform>();
            gRt.anchoredPosition = new Vector2(0, -32);
            gRt.sizeDelta = new Vector2(360, 56);
            Image gBg = goalBadge.AddComponent<Image>();
            gBg.sprite = SpriteFactory.GetPillBadgeSprite(new Color(0.10f, 0.45f, 0.30f, 0.95f));
            gBg.type = Image.Type.Sliced;
            gBg.raycastTarget = false;

            GameObject goalObj = CreateText(goalBadge.transform, Vector2.zero, new Vector2(350, 50), "Lines: 0/3", 24, FontStyle.Bold, new Color(0.40f, 0.98f, 0.70f), TextAnchor.MiddleCenter);
            m_LevelGoalText = goalObj.GetComponent<Text>();
            m_LevelGoalBadgeObj = goalBadge;

            // Bottom Row: Gem Objectives Bar (Spacious bar dedicated to Gem Collection Goals)
            m_GemObjectivesBar = new GameObject("GemObjectivesBar");
            m_GemObjectivesBar.transform.SetParent(m_LevelHUD.transform, false);
            RectTransform gobRt = m_GemObjectivesBar.AddComponent<RectTransform>();
            gobRt.anchoredPosition = new Vector2(0, -32);
            gobRt.sizeDelta = new Vector2(740, 72);
            Image gobBg = m_GemObjectivesBar.AddComponent<Image>();
            gobBg.sprite = SpriteFactory.GetPillBadgeSprite(new Color(0.04f, 0.08f, 0.18f, 0.70f));
            gobBg.type = Image.Type.Sliced;
            gobBg.raycastTarget = false;

            m_GemObjectiveItems.Clear();
            GemType[] hudGems = new GemType[] { GemType.BlueDiamond, GemType.OrangePentagon, GemType.YellowStar, GemType.RedRuby };
            float[] defaultOffsets = new float[] { -262.5f, -87.5f, 87.5f, 262.5f };

            for (int i = 0; i < hudGems.Length; i++)
            {
                GemType gType = hudGems[i];
                GameObject itemObj = new GameObject($"GemSlot_{gType}");
                itemObj.transform.SetParent(m_GemObjectivesBar.transform, false);
                RectTransform itemRt = itemObj.AddComponent<RectTransform>();
                itemRt.anchoredPosition = new Vector2(defaultOffsets[i], 0);
                itemRt.sizeDelta = new Vector2(160, 62);

                // Individual capsule container for each gem slot
                Image slotBg = itemObj.AddComponent<Image>();
                slotBg.sprite = SpriteFactory.GetPillBadgeSprite(new Color(0.06f, 0.12f, 0.26f, 0.90f));
                slotBg.type = Image.Type.Sliced;
                slotBg.raycastTarget = false;

                // Prominent Gem Icon (enlarged to 48x48)
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(itemObj.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchoredPosition = new Vector2(-34, 0);
                iconRt.sizeDelta = new Vector2(48, 48);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = GemIconFactory.GetGemSprite(gType);
                iconImg.raycastTarget = false;

                // Crisp, bold counter text
                GameObject countObj = CreateText(itemObj.transform, new Vector2(30, 0), new Vector2(76, 48), "0", 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                Text countTxt = countObj.GetComponent<Text>();

                m_GemObjectiveItems[gType] = (itemObj, iconImg, countTxt, iconRt);
            }
            m_GemObjectivesBar.SetActive(false);

            m_LevelHUD.SetActive(false);
        }

        private void BuildModeSelectScreen()
        {
            m_ModeSelectPanel = new GameObject("ModeSelectPanel");
            m_ModeSelectPanel.transform.SetParent(m_RootCanvas, false);
            RectTransform rt = m_ModeSelectPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

            // Fullscreen Backdrop matching Block Blast vivid royal blue color (#1465DF)
            Image bg = m_ModeSelectPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.39f, 0.87f, 1f);
            bg.raycastTarget = true;

            // Safe Area calculations for Android mobile notches, punch holes, and curved edges
            float safeTop = 0f;
            float safeBottom = 0f;
            float safeLeft = 0f;
            float safeRight = 0f;
            float refHeight = 1920f;
            float refWidth = 1080f;
            if (Screen.height > 0 && Screen.width > 0)
            {
                Rect safeArea = Screen.safeArea;
                float topPixels = Screen.height - (safeArea.y + safeArea.height);
                float bottomPixels = safeArea.y;
                float leftPixels = safeArea.x;
                float rightPixels = Screen.width - (safeArea.x + safeArea.width);

                float scale = refWidth / Screen.width;
                refHeight = Screen.height * scale;

                safeTop = topPixels * scale;
                safeBottom = bottomPixels * scale;
                safeLeft = leftPixels * scale;
                safeRight = rightPixels * scale;
            }
            float padTop = Mathf.Max(safeTop + 30f, 80f);
            float padBottom = Mathf.Max(safeBottom + 40f, 90f);
            float padLeft = Mathf.Max(safeLeft + 35f, 55f);
            float padRight = Mathf.Max(safeRight + 35f, 55f);

            // Top-Left: Settings Gear Button (Circular tactile button)
            m_HomeSettingsBtn = CreateCircularIconButton(m_ModeSelectPanel.transform, new Vector2(padLeft, -padTop), 72, GemIconFactory.GetGearSprite(), new Color(0.10f, 0.28f, 0.70f, 0.85f));
            RectTransform stRt = m_HomeSettingsBtn.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0f, 1f); stRt.anchorMax = new Vector2(0f, 1f); stRt.pivot = new Vector2(0f, 1f);
            m_HomeSettingsBtn.GetComponent<Button>().onClick.AddListener(OpenSettings);

            // Top-Right: "Medal" Icon Button (matching reference image: Medal icon with "Medal" text underneath)
            GameObject medalBtnObj = new GameObject("Button_Medal");
            medalBtnObj.transform.SetParent(m_ModeSelectPanel.transform, false);
            RectTransform medRt = medalBtnObj.AddComponent<RectTransform>();
            medRt.anchorMin = new Vector2(1f, 1f); medRt.anchorMax = new Vector2(1f, 1f); medRt.pivot = new Vector2(1f, 1f);
            medRt.anchoredPosition = new Vector2(-padRight, -padTop);
            medRt.sizeDelta = new Vector2(80, 90);

            Image medImg = medalBtnObj.AddComponent<Image>();
            medImg.color = Color.clear;
            medImg.raycastTarget = true;

            GameObject medalIconObj = new GameObject("MedalIcon");
            medalIconObj.transform.SetParent(medalBtnObj.transform, false);
            RectTransform miRt = medalIconObj.AddComponent<RectTransform>();
            miRt.anchoredPosition = new Vector2(0, 14);
            miRt.sizeDelta = new Vector2(60, 60);
            Image miImg = medalIconObj.AddComponent<Image>();
            miImg.sprite = GemIconFactory.GetMedalSprite();
            miImg.raycastTarget = false;

            GameObject medTxtObj = CreateText(medalBtnObj.transform, new Vector2(0, -28), new Vector2(80, 24), "Medal", 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, m_CandyFont);
            Shadow medTxtSh = medTxtObj.AddComponent<Shadow>();
            medTxtSh.effectColor = new Color(0.04f, 0.15f, 0.45f, 0.90f);
            medTxtSh.effectDistance = new Vector2(1, -1);

            Button medBtn = medalBtnObj.AddComponent<Button>();
            medBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticManager.TriggerLight();
                StartCoroutine(AnimateButtonPress(medRt, OpenSkinWardrobe));
            });
            m_HomeWardrobeBtn = medalBtnObj;

            // Center Content Holder (scaled dynamically for 1080p Android phones across 16:9, 18:9, 19.5:9, 20:9)
            GameObject centerObj = new GameObject("HomeCenterContent");
            centerObj.transform.SetParent(m_ModeSelectPanel.transform, false);
            RectTransform cRt = centerObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(900, refHeight);

            // Responsive vertical layout coordinates:
            // 1. Title section gracefully positioned near top (raised slightly so "ADVENTURE MASTER" text is clearly visible)
            float titleOffsetY = Mathf.Clamp((refHeight - 1920f) * 0.18f, 0f, 85f);
            float titleCenterY = 350f + titleOffsetY;

            // 2. Play buttons placed comfortably in lower third thumb zone (matching Block Blast bottom ~16-18% margin)
            float clBtnY = -(refHeight * 0.30f);
            float minAllowedClY = -(refHeight / 2f) + padBottom + 64f + 25f;
            if (clBtnY < minAllowedClY) clBtnY = minAllowedClY;
            float advBtnY = clBtnY + 126f + 20f;

            // 3. Rotating 3D spheres cluster centered symmetrically between title logo and Adventure button
            float logoBottomY = titleCenterY - 217f; // 434 / 2 = 217 -> bottom edge of full logo
            float advTopY = advBtnY + 63f;
            float clusterY = (logoBottomY + advTopY) / 2f;

            // 0. Ambient Celestial Logo Glow Halo (Soft radial glow centered behind title)
            GameObject titleGlowObj = new GameObject("TitleBackdropGlow");
            titleGlowObj.transform.SetParent(centerObj.transform, false);
            RectTransform tgRt = titleGlowObj.AddComponent<RectTransform>();
            tgRt.anchoredPosition = new Vector2(0, titleCenterY);
            tgRt.sizeDelta = new Vector2(720, 420);
            Image tgImg = titleGlowObj.AddComponent<Image>();
            tgImg.sprite = SpriteFactory.GetSoftGlowOrbSprite();
            tgImg.color = new Color(0.20f, 0.52f, 1.0f, 0.28f);
            tgImg.raycastTarget = false;

            // 1. Center 3D Spheres Orbital Cluster with Dynamic Visuals & Juicy Animations!
            // Placed symmetrically in the center between Logo and Buttons
            GameObject spheresClusterObj = new GameObject("CenterSpheresCluster");
            spheresClusterObj.transform.SetParent(centerObj.transform, false);
            RectTransform clusterRt = spheresClusterObj.AddComponent<RectTransform>();
            clusterRt.anchoredPosition = new Vector2(0, clusterY);
            clusterRt.sizeDelta = new Vector2(420, 260);

            HomeSpheresAnimator sphereAnim = spheresClusterObj.AddComponent<HomeSpheresAnimator>();
            sphereAnim.SetBaseCenterY(clusterY);
            sphereAnim.Initialize();

            // 2. Master 3D Candy Balloon Title Logo ("SPHERE BLAST" + Chubby Golden Crown + "ADVENTURE MASTER")
            // Rendered in front of the sphere cluster so "ADVENTURE MASTER" text is always completely clear and never obscured!
            GameObject logoObj = new GameObject("Title_SphereBlast_MasterLogo");
            logoObj.transform.SetParent(centerObj.transform, false);
            RectTransform logoRt = logoObj.AddComponent<RectTransform>();
            logoRt.anchoredPosition = new Vector2(0, titleCenterY);
            logoRt.sizeDelta = new Vector2(600, 434);

            Image logoImg = logoObj.AddComponent<Image>();
            logoImg.sprite = SpriteFactory.GetSphereBlastTitleLogoSprite();
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;

            // Interactive Golden Crown tap reaction area
            GameObject crownTapObj = new GameObject("Crown_InteractiveZone");
            crownTapObj.transform.SetParent(logoObj.transform, false);
            RectTransform crRt = crownTapObj.AddComponent<RectTransform>();
            crRt.anchoredPosition = new Vector2(-110f, 120f);
            crRt.sizeDelta = new Vector2(140, 120);
            Button crBtn = crownTapObj.AddComponent<Button>();
            crBtn.transition = Selectable.Transition.None;
            crBtn.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                HapticManager.TriggerLight();
                StartCoroutine(AnimateButtonPress(logoRt, null));
            });

            // Diamond Twinkle Star on crown peak
            GameObject crownStarGlint = new GameObject("CrownStarGlint");
            crownStarGlint.transform.SetParent(crownTapObj.transform, false);
            RectTransform csgRt = crownStarGlint.AddComponent<RectTransform>();
            csgRt.anchoredPosition = new Vector2(0f, 32f);
            csgRt.sizeDelta = new Vector2(34, 34);
            Image csgImg = crownStarGlint.AddComponent<Image>();
            csgImg.sprite = GemIconFactory.GetStarSprite(true);
            csgImg.color = new Color(1f, 1f, 1f, 0.95f);
            csgImg.raycastTarget = false;

            // 5a. Adventure Button Ambient Drop Shadow (Soft dark-blue shadow badge floating underneath)
            GameObject advShadowObj = new GameObject("Button_Adventure_Shadow");
            advShadowObj.transform.SetParent(centerObj.transform, false);
            RectTransform advShadowRt = advShadowObj.AddComponent<RectTransform>();
            advShadowRt.anchoredPosition = new Vector2(0, advBtnY - 7f);
            advShadowRt.sizeDelta = new Vector2(644, 126);
            Image advShadowImg = advShadowObj.AddComponent<Image>();
            advShadowImg.sprite = SpriteFactory.GetPillSprite(new Color(0.04f, 0.08f, 0.22f, 0.55f));
            advShadowImg.type = Image.Type.Sliced;
            advShadowImg.raycastTarget = false;

            // 5b. Adventure Button (Chunky 3D Golden-Orange Candy Capsule with Gloss Sheen & Location Pin)
            GameObject advBtnObj = new GameObject("Button_Adventure");
            advBtnObj.transform.SetParent(centerObj.transform, false);
            RectTransform advRt = advBtnObj.AddComponent<RectTransform>();
            advRt.anchoredPosition = new Vector2(0, advBtnY);
            advRt.sizeDelta = new Vector2(640, 126);

            Image advImg = advBtnObj.AddComponent<Image>();
            advImg.sprite = SpriteFactory.GetChunkyBlockBlastButtonSprite(new Color(0.99f, 0.62f, 0.08f), new Color(0.74f, 0.36f, 0.01f), new Color(1f, 1f, 1f, 0.85f));
            advImg.type = Image.Type.Sliced;
            advImg.raycastTarget = true;

            Button advBtn = advBtnObj.AddComponent<Button>();
            advBtn.onClick.AddListener(() =>
            {
                StartCoroutine(AnimateButtonPress(advRt, () =>
                {
                    m_ModeSelectPanel.SetActive(false);
                    OpenLevelSelect(GameModeType.Adventure);
                }));
            });

            // Curved Gloss Glass Sheen Overlay on Top Half
            GameObject advGlossObj = new GameObject("GlossShineOverlay");
            advGlossObj.transform.SetParent(advBtnObj.transform, false);
            RectTransform advGlossRt = advGlossObj.AddComponent<RectTransform>();
            advGlossRt.anchorMin = new Vector2(0.5f, 1f);
            advGlossRt.anchorMax = new Vector2(0.5f, 1f);
            advGlossRt.pivot = new Vector2(0.5f, 1f);
            advGlossRt.anchoredPosition = new Vector2(0, -6f);
            advGlossRt.sizeDelta = new Vector2(560, 42);
            Image advGlossImg = advGlossObj.AddComponent<Image>();
            advGlossImg.sprite = SpriteFactory.GetButtonGlossOverlaySprite();
            advGlossImg.type = Image.Type.Sliced;
            advGlossImg.color = new Color(1f, 1f, 1f, 0.42f);
            advGlossImg.raycastTarget = false;

            // Sparkle Glint Dot Highlight on upper-right rim
            GameObject advGlintObj = new GameObject("GlossDotGlint");
            advGlintObj.transform.SetParent(advBtnObj.transform, false);
            RectTransform advGlintRt = advGlintObj.AddComponent<RectTransform>();
            advGlintRt.anchorMin = new Vector2(0.5f, 1f);
            advGlintRt.anchorMax = new Vector2(0.5f, 1f);
            advGlintRt.pivot = new Vector2(0.5f, 0.5f);
            advGlintRt.anchoredPosition = new Vector2(250, -18f);
            advGlintRt.sizeDelta = new Vector2(16, 16);
            Image advGlintImg = advGlintObj.AddComponent<Image>();
            advGlintImg.sprite = GemIconFactory.GetStarSprite(true);
            advGlintImg.color = new Color(1f, 1f, 1f, 0.75f);
            advGlintImg.raycastTarget = false;

            // Content lockup with HorizontalLayoutGroup so Icon + Text NEVER overlap!
            GameObject advContent = new GameObject("ContentLockup");
            advContent.transform.SetParent(advBtnObj.transform, false);
            RectTransform acRt = advContent.AddComponent<RectTransform>();
            acRt.anchoredPosition = new Vector2(0, 8); // Centered on raised cap
            acRt.sizeDelta = new Vector2(500, 70);

            HorizontalLayoutGroup aHlg = advContent.AddComponent<HorizontalLayoutGroup>();
            aHlg.childAlignment = TextAnchor.MiddleCenter;
            aHlg.childForceExpandWidth = false;
            aHlg.childForceExpandHeight = false;
            aHlg.spacing = 20f;

            // Location Pin Icon with Embossed 3D Shadow
            GameObject pinIconObj = new GameObject("PinIcon");
            pinIconObj.transform.SetParent(advContent.transform, false);
            RectTransform pinRt = pinIconObj.AddComponent<RectTransform>();
            pinRt.sizeDelta = new Vector2(44, 54);
            LayoutElement pinLe = pinIconObj.AddComponent<LayoutElement>();
            pinLe.preferredWidth = 44;
            pinLe.preferredHeight = 54;
            Image pinImg = pinIconObj.AddComponent<Image>();
            pinImg.sprite = GemIconFactory.GetMapPinSprite();
            pinImg.raycastTarget = false;
            Shadow pinSh = pinIconObj.AddComponent<Shadow>();
            pinSh.effectColor = new Color(0.45f, 0.18f, 0.01f, 0.65f);
            pinSh.effectDistance = new Vector2(0, -3);

            // Mode Title: "Adventure" with Deep 3D Drop Shadow + Subtle Outline
            GameObject advTitleObj = CreateText(advContent.transform, Vector2.zero, new Vector2(290, 60), "Adventure", 52, FontStyle.BoldAndItalic, Color.white, TextAnchor.MiddleLeft, m_CandyFont);
            LayoutElement advTxtLe = advTitleObj.AddComponent<LayoutElement>();
            advTxtLe.preferredWidth = 290;
            advTxtLe.preferredHeight = 60;
            Shadow advTxtSh = advTitleObj.AddComponent<Shadow>();
            advTxtSh.effectColor = new Color(0.48f, 0.18f, 0.01f, 0.95f);
            advTxtSh.effectDistance = new Vector2(0, -4);
            Outline advTxtOut = advTitleObj.AddComponent<Outline>();
            advTxtOut.effectColor = new Color(0.65f, 0.28f, 0.01f, 0.40f);
            advTxtOut.effectDistance = new Vector2(1, -1);

            // 6a. Classic Button Ambient Drop Shadow (Soft dark-blue shadow badge floating underneath)
            GameObject clShadowObj = new GameObject("Button_Classic_Shadow");
            clShadowObj.transform.SetParent(centerObj.transform, false);
            RectTransform clShadowRt = clShadowObj.AddComponent<RectTransform>();
            clShadowRt.anchoredPosition = new Vector2(0, clBtnY - 7f);
            clShadowRt.sizeDelta = new Vector2(644, 126);
            Image clShadowImg = clShadowObj.AddComponent<Image>();
            clShadowImg.sprite = SpriteFactory.GetPillSprite(new Color(0.04f, 0.08f, 0.22f, 0.55f));
            clShadowImg.type = Image.Type.Sliced;
            clShadowImg.raycastTarget = false;

            // 6b. Classic Button (Chunky 3D Mint-Green Candy Capsule with Gloss Sheen & Infinity Icon)
            GameObject classicBtnObj = new GameObject("Button_Classic");
            classicBtnObj.transform.SetParent(centerObj.transform, false);
            RectTransform clRt = classicBtnObj.AddComponent<RectTransform>();
            clRt.anchoredPosition = new Vector2(0, clBtnY);
            clRt.sizeDelta = new Vector2(640, 126);

            Image clImg = classicBtnObj.AddComponent<Image>();
            clImg.sprite = SpriteFactory.GetChunkyBlockBlastButtonSprite(new Color(0.12f, 0.88f, 0.62f), new Color(0.05f, 0.58f, 0.40f), new Color(1f, 1f, 1f, 0.85f));
            clImg.type = Image.Type.Sliced;
            clImg.raycastTarget = true;

            Button clBtn = classicBtnObj.AddComponent<Button>();
            clBtn.onClick.AddListener(() =>
            {
                StartCoroutine(AnimateButtonPress(clRt, () =>
                {
                    m_ModeSelectPanel.SetActive(false);
                    UpdateHUDVisibility();
                    if (LevelManager.Instance != null && LevelManager.Instance.IsLevelActive)
                    {
                        LevelManager.Instance.StopLevel();
                        GameManager.Instance.RestartGame();
                    }
                    else if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                    {
                        GameManager.Instance.RestartGame();
                    }
                }));
            });

            // Curved Gloss Glass Sheen Overlay on Top Half
            GameObject clGlossObj = new GameObject("GlossShineOverlay");
            clGlossObj.transform.SetParent(classicBtnObj.transform, false);
            RectTransform clGlossRt = clGlossObj.AddComponent<RectTransform>();
            clGlossRt.anchorMin = new Vector2(0.5f, 1f);
            clGlossRt.anchorMax = new Vector2(0.5f, 1f);
            clGlossRt.pivot = new Vector2(0.5f, 1f);
            clGlossRt.anchoredPosition = new Vector2(0, -6f);
            clGlossRt.sizeDelta = new Vector2(560, 42);
            Image clGlossImg = clGlossObj.AddComponent<Image>();
            clGlossImg.sprite = SpriteFactory.GetButtonGlossOverlaySprite();
            clGlossImg.type = Image.Type.Sliced;
            clGlossImg.color = new Color(1f, 1f, 1f, 0.42f);
            clGlossImg.raycastTarget = false;

            // Sparkle Glint Dot Highlight on upper-right rim
            GameObject clGlintObj = new GameObject("GlossDotGlint");
            clGlintObj.transform.SetParent(classicBtnObj.transform, false);
            RectTransform clGlintRt = clGlintObj.AddComponent<RectTransform>();
            clGlintRt.anchorMin = new Vector2(0.5f, 1f);
            clGlintRt.anchorMax = new Vector2(0.5f, 1f);
            clGlintRt.pivot = new Vector2(0.5f, 0.5f);
            clGlintRt.anchoredPosition = new Vector2(250, -18f);
            clGlintRt.sizeDelta = new Vector2(16, 16);
            Image clGlintImg = clGlintObj.AddComponent<Image>();
            clGlintImg.sprite = GemIconFactory.GetStarSprite(true);
            clGlintImg.color = new Color(1f, 1f, 1f, 0.75f);
            clGlintImg.raycastTarget = false;

            // Content lockup with HorizontalLayoutGroup so Icon + Text NEVER overlap!
            GameObject clContent = new GameObject("ContentLockup");
            clContent.transform.SetParent(classicBtnObj.transform, false);
            RectTransform ccRt = clContent.AddComponent<RectTransform>();
            ccRt.anchoredPosition = new Vector2(0, 8); // Centered on raised cap
            ccRt.sizeDelta = new Vector2(500, 70);

            HorizontalLayoutGroup cHlg = clContent.AddComponent<HorizontalLayoutGroup>();
            cHlg.childAlignment = TextAnchor.MiddleCenter;
            cHlg.childForceExpandWidth = false;
            cHlg.childForceExpandHeight = false;
            cHlg.spacing = 20f;

            // Infinity Icon with Embossed 3D Shadow
            GameObject infIconObj = new GameObject("InfinityIcon");
            infIconObj.transform.SetParent(clContent.transform, false);
            RectTransform infRt = infIconObj.AddComponent<RectTransform>();
            infRt.sizeDelta = new Vector2(58, 32);
            LayoutElement infLe = infIconObj.AddComponent<LayoutElement>();
            infLe.preferredWidth = 58;
            infLe.preferredHeight = 32;
            Image infImg = infIconObj.AddComponent<Image>();
            infImg.sprite = GemIconFactory.GetInfinitySprite();
            infImg.raycastTarget = false;
            Shadow infSh = infIconObj.AddComponent<Shadow>();
            infSh.effectColor = new Color(0.02f, 0.32f, 0.22f, 0.65f);
            infSh.effectDistance = new Vector2(0, -3);

            // Mode Title: "Classic" with Deep 3D Drop Shadow + Subtle Outline
            GameObject clTitleObj = CreateText(clContent.transform, Vector2.zero, new Vector2(220, 60), "Classic", 52, FontStyle.BoldAndItalic, Color.white, TextAnchor.MiddleLeft, m_CandyFont);
            LayoutElement clTxtLe = clTitleObj.AddComponent<LayoutElement>();
            clTxtLe.preferredWidth = 220;
            clTxtLe.preferredHeight = 60;
            Shadow clTxtSh = clTitleObj.AddComponent<Shadow>();
            clTxtSh.effectColor = new Color(0.02f, 0.35f, 0.24f, 0.95f);
            clTxtSh.effectDistance = new Vector2(0, -4);
            Outline clTxtOut = clTitleObj.AddComponent<Outline>();
            clTxtOut.effectColor = new Color(0.04f, 0.45f, 0.30f, 0.40f);
            clTxtOut.effectDistance = new Vector2(1, -1);

            // 7. Living Animations: Add HomeButtonBreather for gentle idle pulse & crown twinkle
            HomeButtonBreather breather = centerObj.AddComponent<HomeButtonBreather>();
            breather.classicBtnRt = clRt;
            breather.classicShadowRt = clShadowRt;
            breather.adventureBtnRt = advRt;
            breather.adventureShadowRt = advShadowRt;
            breather.logoRt = logoRt;
            breather.crownStarGlintRt = csgRt;

            m_ModeSelectPanel.SetActive(false);
        }

        private void BuildLevelSelectScreen()
        {
            m_LevelSelectPanel = new GameObject("LevelSelectPanel");
            m_LevelSelectPanel.transform.SetParent(m_RootCanvas, false);
            RectTransform rt = m_LevelSelectPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

            Image bg = m_LevelSelectPanel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.11f, 1f);

            // Center Dialog Window
            GameObject winObj = new GameObject("LevelSelectDialog");
            winObj.transform.SetParent(m_LevelSelectPanel.transform, false);
            RectTransform winRt = winObj.AddComponent<RectTransform>();
            winRt.sizeDelta = new Vector2(980, 750);
            Image winBg = winObj.AddComponent<Image>();
            winBg.sprite = SpriteFactory.GetPanelSprite(new Color(0.07f, 0.12f, 0.22f, 0.98f), new Color(0.25f, 0.40f, 0.68f, 1f), 2.5f, 20);
            winBg.type = Image.Type.Sliced;

            // Back Button (MODES)
            GameObject backBtn = CreateButton(winObj.transform, new Vector2(-410, 320), new Vector2(140, 56), new Color(0.18f, 0.28f, 0.48f), "< MODES", 20);
            backBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_LevelSelectPanel.SetActive(false);
                OpenModeSelect();
            });

            // Title
            GameObject titleObj = CreateText(winObj.transform, new Vector2(0, 325), new Vector2(500, 50), "ADVENTURE JOURNEY", 42, FontStyle.Bold, new Color(1.0f, 0.85f, 0.25f), TextAnchor.MiddleCenter);
            m_LevelSelectTitleText = titleObj.GetComponent<Text>();

            // Stars Counter Pill
            GameObject starsPill = new GameObject("StarsPill");
            starsPill.transform.SetParent(winObj.transform, false);
            RectTransform spRt = starsPill.AddComponent<RectTransform>();
            spRt.anchoredPosition = new Vector2(-125, 275);
            spRt.sizeDelta = new Vector2(210, 38);
            Image spBg = starsPill.AddComponent<Image>();
            spBg.sprite = SpriteFactory.GetPillBadgeSprite(new Color(0.35f, 0.28f, 0.08f, 0.95f));
            spBg.raycastTarget = false;

            // Stars counter value
            GameObject starsObj = CreateText(starsPill.transform, Vector2.zero, new Vector2(200, 34), "0 / 60 STARS", 20, FontStyle.Bold, new Color(1f, 0.88f, 0.25f), TextAnchor.MiddleCenter);
            m_LevelSelectStarsText = starsObj.GetComponent<Text>();

            // Ball Designs Wardrobe Quick Button
            GameObject wardrobeBtn = CreateButton(winObj.transform, new Vector2(125, 275), new Vector2(210, 38), new Color(0.55f, 0.25f, 0.85f), "BALL DESIGNS", 18);
            wardrobeBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                OpenSkinWardrobe();
            });

            // Close Button (X) returns to Home screen
            GameObject closeBtn = CreateButton(winObj.transform, new Vector2(430, 320), new Vector2(56, 56), new Color(0.85f, 0.25f, 0.25f), "X", 28);
            closeBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_LevelSelectPanel.SetActive(false);
                OpenModeSelect();
            });

            // Grid Container (for 20 level buttons per page)
            GameObject gridObj = new GameObject("GridContainer");
            gridObj.transform.SetParent(winObj.transform, false);
            RectTransform gRt = gridObj.AddComponent<RectTransform>();
            gRt.anchoredPosition = new Vector2(0, -45);
            gRt.sizeDelta = new Vector2(920, 520);
            m_LevelGridContainer = gridObj.transform;

            // Page Navigation Bar (Bottom of dialog)
            m_LevelPageNavObj = new GameObject("PageNav");
            m_LevelPageNavObj.transform.SetParent(winObj.transform, false);
            RectTransform pRt = m_LevelPageNavObj.AddComponent<RectTransform>();
            pRt.anchoredPosition = new Vector2(0, -325);
            pRt.sizeDelta = new Vector2(400, 48);

            GameObject prevBtn = CreateButton(m_LevelPageNavObj.transform, new Vector2(-130, 0), new Vector2(56, 42), new Color(0.18f, 0.32f, 0.58f), "<", 24);
            prevBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (m_LevelSelectPage > 0)
                {
                    m_LevelSelectPage--;
                    HapticManager.TriggerLight();
                    RefreshLevelSelectGrid(m_ActiveViewingMode);
                }
            });

            GameObject pageTxtObj = CreateText(m_LevelPageNavObj.transform, Vector2.zero, new Vector2(180, 38), "PAGE 1 (1-20)", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            m_LevelPageNavText = pageTxtObj.GetComponent<Text>();

            GameObject nextBtn = CreateButton(m_LevelPageNavObj.transform, new Vector2(130, 0), new Vector2(56, 42), new Color(0.18f, 0.32f, 0.58f), ">", 24);
            nextBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                int totalLevels = (m_ActiveViewingMode == GameModeType.Adventure) ? 40 : 20;
                int maxPages = Mathf.CeilToInt(totalLevels / 20f);
                if (m_LevelSelectPage < maxPages - 1)
                {
                    m_LevelSelectPage++;
                    HapticManager.TriggerLight();
                    RefreshLevelSelectGrid(m_ActiveViewingMode);
                }
            });

            m_LevelSelectPanel.SetActive(false);
        }

        private void RefreshLevelSelectGrid(GameModeType mode)
        {
            if (m_LevelGridContainer == null) return;
            m_ActiveViewingMode = mode;

            // Update Title
            string modeName = "ADVENTURE JOURNEY";
            if (mode == GameModeType.Event) modeName = "EVENT RUSH";
            else if (mode == GameModeType.Task) modeName = "TASK PUZZLES";

            if (m_LevelSelectTitleText != null) m_LevelSelectTitleText.text = modeName;

            int totalLevels = (mode == GameModeType.Adventure) ? 40 : 20;
            int maxStars = totalLevels * 3;

            // Update Total Stars
            int totalStars = GetTotalModeStars(mode);
            if (m_LevelSelectStarsText != null) m_LevelSelectStarsText.text = $"{totalStars} / {maxStars} STARS";

            int maxPages = Mathf.CeilToInt(totalLevels / 20f);
            if (m_LevelSelectPage >= maxPages) m_LevelSelectPage = 0;

            if (m_LevelPageNavObj != null)
            {
                m_LevelPageNavObj.SetActive(maxPages > 1);
            }
            if (m_LevelPageNavText != null)
            {
                int pageStart = m_LevelSelectPage * 20 + 1;
                int pageEnd = Mathf.Min((m_LevelSelectPage + 1) * 20, totalLevels);
                m_LevelPageNavText.text = $"PAGE {m_LevelSelectPage + 1} ({pageStart}-{pageEnd})";
            }

            // Clear previous buttons
            for (int i = m_LevelGridContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_LevelGridContainer.GetChild(i).gameObject);
            }

            // Grid Layout: 4 rows x 5 columns = 20 levels
            float startX = -360f;
            float stepX = 180f;
            float startY = 170f;
            float stepY = -115f;

            int startLvl = m_LevelSelectPage * 20 + 1;
            int endLvl = Mathf.Min(startLvl + 19, totalLevels);

            for (int lvlNum = startLvl; lvlNum <= endLvl; lvlNum++)
            {
                int localIndex = lvlNum - startLvl;
                int col = localIndex % 5;
                int row = localIndex / 5;
                Vector2 pos = new Vector2(startX + col * stepX, startY + row * stepY);

                int targetLvl = lvlNum;
                bool isUnlocked = LevelManager.IsLevelUnlocked(mode, targetLvl);
                int stars = LevelManager.GetLevelStars(mode, targetLvl);
                LevelData data = LevelDatabase.GetLevel(mode, targetLvl);

                GameObject btnObj = new GameObject($"LvlBtn_{targetLvl}");
                btnObj.transform.SetParent(m_LevelGridContainer, false);
                RectTransform bRt = btnObj.AddComponent<RectTransform>();
                bRt.anchoredPosition = pos;
                bRt.sizeDelta = new Vector2(164, 100);

                Image bg = btnObj.AddComponent<Image>();
                bg.type = Image.Type.Sliced;
                Button btn = btnObj.AddComponent<Button>();

                if (isUnlocked)
                {
                    bg.sprite = SpriteFactory.GetRoundedButtonSprite(new Color(0.12f, 0.20f, 0.38f, 0.98f), new Color(0.35f, 0.58f, 0.95f, 1f), 0.06f);

                    // Big Level Number
                    CreateText(btnObj.transform, new Vector2(0, 16), new Vector2(160, 42), targetLvl.ToString(), 32, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

                    // Level Name preview (e.g. "Royal Chalice")
                    string shortName = (data != null && !string.IsNullOrEmpty(data.LevelName)) ? data.LevelName : $"Level {targetLvl}";
                    CreateText(btnObj.transform, new Vector2(0, -9), new Vector2(160, 24), shortName, 15, FontStyle.Normal, new Color(0.72f, 0.85f, 1f, 0.9f), TextAnchor.MiddleCenter);

                    // 3 Star Icons
                    GameObject starsRow = new GameObject("StarsRow");
                    starsRow.transform.SetParent(btnObj.transform, false);
                    RectTransform sRt = starsRow.AddComponent<RectTransform>();
                    sRt.anchoredPosition = new Vector2(0, -32);
                    sRt.sizeDelta = new Vector2(80, 22);

                    float[] starXs = { -22f, 0f, 22f };
                    for (int s = 0; s < 3; s++)
                    {
                        GameObject sObj = new GameObject($"Star_{s}");
                        sObj.transform.SetParent(starsRow.transform, false);
                        RectTransform siRt = sObj.AddComponent<RectTransform>();
                        siRt.anchoredPosition = new Vector2(starXs[s], 0);
                        siRt.sizeDelta = new Vector2(20, 20);
                        Image sImg = sObj.AddComponent<Image>();
                        sImg.sprite = GemIconFactory.GetStarSprite(s < stars);
                        sImg.raycastTarget = false;
                    }

                    btn.onClick.AddListener(() =>
                    {
                        if (LevelManager.Instance != null)
                        {
                            LevelManager.Instance.StartLevel(mode, targetLvl);
                        }
                        m_LevelSelectPanel.SetActive(false);
                        UpdateHUDVisibility();
                    });
                }
                else
                {
                    bg.sprite = SpriteFactory.GetRoundedButtonSprite(new Color(0.08f, 0.10f, 0.16f, 0.85f), new Color(0.18f, 0.24f, 0.35f, 0.7f), 0.06f);

                    // Padlock Icon
                    GameObject lockObj = new GameObject("LockIcon");
                    lockObj.transform.SetParent(btnObj.transform, false);
                    RectTransform lRt = lockObj.AddComponent<RectTransform>();
                    lRt.anchoredPosition = new Vector2(0, 10);
                    lRt.sizeDelta = new Vector2(38, 38);
                    Image lImg = lockObj.AddComponent<Image>();
                    lImg.sprite = GemIconFactory.GetLockSprite();
                    lImg.raycastTarget = false;

                    // Level label
                    CreateText(btnObj.transform, new Vector2(0, -25), new Vector2(160, 24), $"Level {targetLvl}", 16, FontStyle.Normal, new Color(0.55f, 0.62f, 0.75f, 0.8f), TextAnchor.MiddleCenter);

                    btn.onClick.AddListener(() =>
                    {
                        HapticManager.TriggerLight();
                        FloatingText.Create(Vector3.zero, $"Complete Level {targetLvl - 1} first!", Color.yellow, 6f);
                    });
                }

                // Reward Gift badge on milestone levels that award a new Ball Design!
                SkinInfo rewardSkin = (mode == GameModeType.Adventure) ? SkinManager.GetSkinForLevel(targetLvl) : null;
                if (rewardSkin != null)
                {
                    GameObject giftObj = new GameObject("RewardBadge");
                    giftObj.transform.SetParent(btnObj.transform, false);
                    RectTransform gRt = giftObj.AddComponent<RectTransform>();
                    gRt.anchoredPosition = new Vector2(48, 30);
                    gRt.sizeDelta = new Vector2(56, 22);

                    Image gBg = giftObj.AddComponent<Image>();
                    bool skinUnlocked = SkinManager.IsSkinUnlocked(rewardSkin.Type);
                    Color badgeBg = skinUnlocked ? new Color(0.12f, 0.55f, 0.28f, 0.95f) : new Color(0.85f, 0.60f, 0.10f, 0.95f);
                    gBg.sprite = SpriteFactory.GetPillBadgeSprite(badgeBg);
                    gBg.raycastTarget = false;

                    string bTxt = skinUnlocked ? "BALL" : "GIFT";
                    CreateText(giftObj.transform, Vector2.zero, new Vector2(54, 20), bTxt, 11, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                }
            }
        }

                private void BuildVictoryModal()
        {
            m_VictoryModal = new GameObject("VictoryModal");
            m_VictoryModal.transform.SetParent(m_RootCanvas, false);
            RectTransform rt = m_VictoryModal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

            Image bg = m_VictoryModal.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.12f, 0.95f);

            GameObject winObj = new GameObject("VictoryDialog");
            winObj.transform.SetParent(m_VictoryModal.transform, false);
            RectTransform winRt = winObj.AddComponent<RectTransform>();
            winRt.sizeDelta = new Vector2(680, 740);
            Image winBg = winObj.AddComponent<Image>();
            winBg.sprite = SpriteFactory.GetPanelSprite(new Color(0.08f, 0.14f, 0.28f, 0.98f), new Color(0.24f, 0.45f, 0.85f, 1f), 2.5f, 20);
            winBg.type = Image.Type.Sliced;

            GameObject titleObj = CreateText(winObj.transform, new Vector2(0, 275), new Vector2(600, 60), "VICTORY!", 54, FontStyle.Bold, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter);
            m_VictoryTitleText = titleObj.GetComponent<Text>();

            // 3 Stars Celebration Row
            GameObject starsRow = new GameObject("VictoryStarsRow");
            starsRow.transform.SetParent(winObj.transform, false);
            RectTransform sRt = starsRow.AddComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0, 215);
            sRt.sizeDelta = new Vector2(180, 48);

            float[] starXs = { -50f, 0f, 50f };
            for (int s = 0; s < 3; s++)
            {
                GameObject sObj = new GameObject($"Star_{s}");
                sObj.transform.SetParent(starsRow.transform, false);
                RectTransform siRt = sObj.AddComponent<RectTransform>();
                siRt.anchoredPosition = new Vector2(starXs[s], 0);
                siRt.sizeDelta = new Vector2(44, 44);
                Image sImg = sObj.AddComponent<Image>();
                sImg.sprite = GemIconFactory.GetStarSprite(true);
                sImg.raycastTarget = false;
                m_VictoryStarImgs[s] = sImg;
            }

            GameObject subObj = CreateText(winObj.transform, new Vector2(0, 165), new Vector2(600, 32), "Next Level Unlocked!", 21, FontStyle.Normal, new Color(0.8f, 0.9f, 1f), TextAnchor.MiddleCenter);
            m_VictorySubtitleText = subObj.GetComponent<Text>();

            // Ball Skin Reward Showcase Card (Appears when an Adventure level awards a new ball design!)
            m_VictorySkinRewardObj = new GameObject("SkinRewardCard");
            m_VictorySkinRewardObj.transform.SetParent(winObj.transform, false);
            RectTransform rwRt = m_VictorySkinRewardObj.AddComponent<RectTransform>();
            rwRt.anchoredPosition = new Vector2(0, 85);
            rwRt.sizeDelta = new Vector2(580, 92);
            Image rwBg = m_VictorySkinRewardObj.AddComponent<Image>();
            rwBg.sprite = SpriteFactory.GetPanelSprite(new Color(0.10f, 0.18f, 0.38f, 0.98f), new Color(0.35f, 0.65f, 1f, 1f), 2f, 16);
            rwBg.type = Image.Type.Sliced;

            // Reward Ball Icon Preview (Left)
            GameObject pIconObj = new GameObject("RewardIcon");
            pIconObj.transform.SetParent(m_VictorySkinRewardObj.transform, false);
            RectTransform pIconRt = pIconObj.AddComponent<RectTransform>();
            pIconRt.anchoredPosition = new Vector2(-215, 0);
            pIconRt.sizeDelta = new Vector2(64, 64);
            m_VictorySkinPreviewImg = pIconObj.AddComponent<Image>();
            m_VictorySkinPreviewImg.raycastTarget = false;

            // Reward Subtitle & Name
            CreateText(m_VictorySkinRewardObj.transform, new Vector2(0, 16), new Vector2(300, 26), "NEW BALL DESIGN UNLOCKED!", 18, FontStyle.Bold, new Color(1f, 0.85f, 0.25f), TextAnchor.MiddleCenter);
            GameObject nameTxtObj = CreateText(m_VictorySkinRewardObj.transform, new Vector2(0, -14), new Vector2(300, 26), "STAR CORE BALL", 18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            m_VictorySkinNameText = nameTxtObj.GetComponent<Text>();

            // Equip Button (Right)
            GameObject equipBtn = CreateButton(m_VictorySkinRewardObj.transform, new Vector2(205, 0), new Vector2(135, 52), new Color(0.12f, 0.68f, 0.35f), "EQUIP", 18);
            m_VictorySkinEquipBtn = equipBtn.GetComponent<Button>();
            m_VictorySkinEquipBtnText = equipBtn.GetComponentInChildren<Text>();

            m_VictorySkinRewardObj.SetActive(false);

            // Next Level Button
            GameObject nextBtn = CreateButton(winObj.transform, new Vector2(0, -5), new Vector2(460, 74), new Color(0.12f, 0.72f, 0.32f), "NEXT LEVEL", 30);
            m_VictoryNextBtnObj = nextBtn;
            nextBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_VictoryModal.SetActive(false);
                if (LevelManager.Instance != null) LevelManager.Instance.NextLevel();
            });

            // Level Select Grid Button
            GameObject lvlSelectBtn = CreateButton(winObj.transform, new Vector2(0, -88), new Vector2(460, 64), new Color(0.18f, 0.38f, 0.72f), "LEVEL SELECT", 25);
            lvlSelectBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_VictoryModal.SetActive(false);
                if (LevelManager.Instance != null) OpenLevelSelect(LevelManager.Instance.CurrentMode);
            });

            // Ball Designs Wardrobe Button
            GameObject wardrobeBtn = CreateButton(winObj.transform, new Vector2(0, -162), new Vector2(460, 60), new Color(0.55f, 0.25f, 0.85f), "BALL DESIGNS", 23);
            wardrobeBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_VictoryModal.SetActive(false);
                OpenSkinWardrobe();
            });

            // Mode Select Button
            GameObject modeBtn = CreateButton(winObj.transform, new Vector2(0, -232), new Vector2(460, 56), new Color(0.25f, 0.28f, 0.40f), "MAIN MENU", 22);
            modeBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_VictoryModal.SetActive(false);
                OpenModeSelect();
            });

            m_VictoryModal.SetActive(false);
        }

        private void BuildDefeatModal()
        {
            m_DefeatModal = new GameObject("DefeatModal");
            m_DefeatModal.transform.SetParent(m_RootCanvas, false);
            RectTransform rt = m_DefeatModal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

            Image bg = m_DefeatModal.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.10f, 0.95f);

            GameObject winObj = new GameObject("DefeatDialog");
            winObj.transform.SetParent(m_DefeatModal.transform, false);
            RectTransform winRt = winObj.AddComponent<RectTransform>();
            winRt.sizeDelta = new Vector2(680, 650);
            Image winBg = winObj.AddComponent<Image>();
            winBg.sprite = SpriteFactory.GetPanelSprite(new Color(0.08f, 0.14f, 0.28f, 0.98f), new Color(0.65f, 0.25f, 0.25f, 1f), 2.5f, 20);
            winBg.type = Image.Type.Sliced;

            GameObject titleObj = CreateText(winObj.transform, new Vector2(0, 225), new Vector2(600, 70), "OUT OF MOVES!", 56, FontStyle.Bold, new Color(0.96f, 0.35f, 0.35f), TextAnchor.MiddleCenter);
            m_DefeatTitleText = titleObj.GetComponent<Text>();

            GameObject subObj = CreateText(winObj.transform, new Vector2(0, 165), new Vector2(600, 36), "Don't give up! Try again.", 22, FontStyle.Normal, new Color(0.85f, 0.88f, 0.95f), TextAnchor.MiddleCenter);
            m_DefeatSubtitleText = subObj.GetComponent<Text>();

            // +5 Extra Moves Button (Free with limited chances)
            m_DefeatExtraMovesBtn = CreateButton(winObj.transform, new Vector2(0, 85), new Vector2(450, 72), new Color(0.92f, 0.65f, 0.12f), "+5 MOVES (FREE)", 28);
            m_DefeatExtraMovesText = m_DefeatExtraMovesBtn.GetComponentInChildren<Text>();
            m_DefeatExtraMovesBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.AddExtraMoves(5);
                }
            });

            // Retry Button
            m_DefeatRetryBtn = CreateButton(winObj.transform, new Vector2(0, -2), new Vector2(450, 68), new Color(0.85f, 0.40f, 0.15f), "RETRY LEVEL", 30);
            m_DefeatRetryBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_DefeatModal.SetActive(false);
                if (LevelManager.Instance != null) LevelManager.Instance.RestartCurrentLevel();
            });

            // Level Select Grid Button
            m_DefeatLvlSelectBtn = CreateButton(winObj.transform, new Vector2(0, -82), new Vector2(450, 64), new Color(0.18f, 0.38f, 0.72f), "LEVEL SELECT", 26);
            m_DefeatLvlSelectBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_DefeatModal.SetActive(false);
                if (LevelManager.Instance != null) OpenLevelSelect(LevelManager.Instance.CurrentMode);
            });

            // Main Menu Button
            m_DefeatModeBtn = CreateButton(winObj.transform, new Vector2(0, -160), new Vector2(450, 58), new Color(0.25f, 0.28f, 0.40f), "MAIN MENU", 24);
            m_DefeatModeBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_DefeatModal.SetActive(false);
                OpenModeSelect();
            });

            m_DefeatModal.SetActive(false);
        }

        // ==========================================
        // BALL DESIGNS WARDROBE MODAL
        // ==========================================
        public void OpenSkinWardrobe()
        {
            if (m_SkinWardrobeModal == null) BuildSkinWardrobeModal();
            RefreshSkinWardrobeGrid();
            if (m_SkinWardrobeModal != null)
            {
                m_SkinWardrobeModal.SetActive(true);
                m_SkinWardrobeModal.transform.SetAsLastSibling();
            }
        }

        private void BuildSkinWardrobeModal()
        {
            m_SkinWardrobeModal = new GameObject("SkinWardrobeModal");
            m_SkinWardrobeModal.transform.SetParent(m_RootCanvas, false);
            RectTransform rt = m_SkinWardrobeModal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;

            Image bg = m_SkinWardrobeModal.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.11f, 0.97f);

            GameObject winObj = new GameObject("WardrobeDialog");
            winObj.transform.SetParent(m_SkinWardrobeModal.transform, false);
            RectTransform winRt = winObj.AddComponent<RectTransform>();
            winRt.sizeDelta = new Vector2(740, 840);
            Image winBg = winObj.AddComponent<Image>();
            winBg.sprite = SpriteFactory.GetPanelSprite(new Color(0.07f, 0.12f, 0.22f, 0.98f), new Color(0.28f, 0.48f, 0.85f, 1f), 2.5f, 20);
            winBg.type = Image.Type.Sliced;

            // Title
            CreateText(winObj.transform, new Vector2(0, 355), new Vector2(600, 50), "BALL DESIGNS", 38, FontStyle.Bold, new Color(1.0f, 0.85f, 0.25f), TextAnchor.MiddleCenter);

            // Subtitle
            CreateText(winObj.transform, new Vector2(0, 310), new Vector2(620, 32), "Complete Adventure levels to unlock new stunning ball designs!", 19, FontStyle.Normal, new Color(0.70f, 0.82f, 1f, 0.9f), TextAnchor.MiddleCenter);

            // Close Button (X)
            GameObject closeBtn = CreateButton(winObj.transform, new Vector2(315, 355), new Vector2(56, 56), new Color(0.85f, 0.25f, 0.25f), "X", 28);
            closeBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_SkinWardrobeModal.SetActive(false);
            });

            // Grid Container for 10 skins (2 columns x 5 rows)
            GameObject gridObj = new GameObject("SkinsGridContainer");
            gridObj.transform.SetParent(winObj.transform, false);
            RectTransform gRt = gridObj.AddComponent<RectTransform>();
            gRt.anchoredPosition = new Vector2(0, -30);
            gRt.sizeDelta = new Vector2(680, 600);
            m_SkinGridContainer = gridObj.transform;

            m_SkinWardrobeModal.SetActive(false);
        }

        private void RefreshSkinWardrobeGrid()
        {
            if (m_SkinGridContainer == null) return;

            for (int i = m_SkinGridContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_SkinGridContainer.GetChild(i).gameObject);
            }

            List<SkinInfo> allSkins = SkinManager.GetAllSkins();
            BallSkinType equipped = SkinManager.CurrentSkin;

            float startX = -170f;
            float stepX = 340f;
            float startY = 220f;
            float stepY = -110f;

            for (int i = 0; i < allSkins.Count; i++)
            {
                SkinInfo skin = allSkins[i];
                int col = i % 2;
                int row = i / 2;
                Vector2 pos = new Vector2(startX + col * stepX, startY + row * stepY);

                bool isUnlocked = SkinManager.IsSkinUnlocked(skin.Type);
                bool isEquipped = (equipped == skin.Type);

                GameObject card = new GameObject($"SkinCard_{skin.Type}");
                card.transform.SetParent(m_SkinGridContainer, false);
                RectTransform cRt = card.AddComponent<RectTransform>();
                cRt.anchoredPosition = pos;
                cRt.sizeDelta = new Vector2(325, 96);

                Image cBg = card.AddComponent<Image>();
                Color bgColor = isEquipped
                    ? new Color(0.12f, 0.25f, 0.45f, 0.98f)
                    : (isUnlocked ? new Color(0.08f, 0.14f, 0.26f, 0.95f) : new Color(0.05f, 0.08f, 0.14f, 0.85f));
                Color borderColor = isEquipped
                    ? new Color(0.30f, 0.88f, 0.50f, 1f)
                    : (isUnlocked ? new Color(0.24f, 0.40f, 0.68f, 0.8f) : new Color(0.15f, 0.20f, 0.32f, 0.5f));
                cBg.sprite = SpriteFactory.GetRoundedButtonSprite(bgColor, borderColor, 0.06f);

                // Ball Preview Icon (Left)
                GameObject iconObj = new GameObject("BallIcon");
                iconObj.transform.SetParent(card.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchoredPosition = new Vector2(-115, 0);
                iconRt.sizeDelta = new Vector2(64, 64);
                Image iconImg = iconObj.AddComponent<Image>();
                Color previewCol = isUnlocked ? new Color(0.18f, 0.50f, 0.95f) : new Color(0.40f, 0.45f, 0.55f, 0.45f);
                iconImg.sprite = SpriteFactory.GetBlockSprite(previewCol, skin.Type);
                iconImg.raycastTarget = false;

                // Skin Title & Unlock Status (Center)
                string titleText = skin.Name;
                CreateText(card.transform, new Vector2(8, 18), new Vector2(170, 32), titleText, 20, FontStyle.Bold, isUnlocked ? Color.white : new Color(0.6f, 0.65f, 0.75f), TextAnchor.MiddleLeft);

                string statusText = isUnlocked ? "Unlocked" : $"Beat Level {skin.UnlockAdventureLevel}";
                Color statusCol = isUnlocked ? new Color(0.35f, 0.88f, 0.45f) : new Color(0.85f, 0.45f, 0.45f);
                CreateText(card.transform, new Vector2(8, -16), new Vector2(170, 28), statusText, 17, FontStyle.Normal, statusCol, TextAnchor.MiddleLeft);

                // Right Action (Equip Button or Equipped Badge)
                if (isEquipped)
                {
                    GameObject badge = new GameObject("EquippedBadge");
                    badge.transform.SetParent(card.transform, false);
                    RectTransform bRt = badge.AddComponent<RectTransform>();
                    bRt.anchoredPosition = new Vector2(105, 0);
                    bRt.sizeDelta = new Vector2(90, 42);
                    Image bBg = badge.AddComponent<Image>();
                    bBg.sprite = SpriteFactory.GetPillBadgeSprite(new Color(0.12f, 0.65f, 0.30f));
                    CreateText(badge.transform, Vector2.zero, new Vector2(88, 40), "EQUIPPED", 14, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                }
                else if (isUnlocked)
                {
                    GameObject equipBtn = CreateButton(card.transform, new Vector2(105, 0), new Vector2(90, 44), new Color(0.18f, 0.45f, 0.85f), "EQUIP", 16);
                    BallSkinType capturedType = skin.Type;
                    equipBtn.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        SkinManager.EquipSkin(capturedType);
                        RefreshSkinWardrobeGrid();
                    });
                }
                else
                {
                    GameObject lockBadge = new GameObject("LockBadge");
                    lockBadge.transform.SetParent(card.transform, false);
                    RectTransform lbRt = lockBadge.AddComponent<RectTransform>();
                    lbRt.anchoredPosition = new Vector2(105, 0);
                    lbRt.sizeDelta = new Vector2(84, 38);
                    Image lbBg = lockBadge.AddComponent<Image>();
                    lbBg.sprite = SpriteFactory.GetPillBadgeSprite(new Color(0.18f, 0.22f, 0.32f, 0.8f));
                    CreateText(lockBadge.transform, Vector2.zero, new Vector2(82, 36), "LOCKED", 14, FontStyle.Bold, new Color(0.6f, 0.65f, 0.75f), TextAnchor.MiddleCenter);
                }
            }
        }

        private void BuildClassicGameOverModal()
        {
            m_GameOverPanel = new GameObject("GameOverPanel");
            m_GameOverPanel.transform.SetParent(m_RootCanvas, false);
            RectTransform panelRt = m_GameOverPanel.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one; panelRt.sizeDelta = Vector2.zero;

            Image panelImg = m_GameOverPanel.AddComponent<Image>();
            panelImg.color = new Color(0.04f, 0.06f, 0.12f, 0.95f);

            GameObject winObj = new GameObject("GameOverDialog");
            winObj.transform.SetParent(m_GameOverPanel.transform, false);
            RectTransform winRt = winObj.AddComponent<RectTransform>();
            winRt.sizeDelta = new Vector2(680, 680);
            Image winBg = winObj.AddComponent<Image>();
            winBg.sprite = SpriteFactory.GetPanelSprite(new Color(0.08f, 0.14f, 0.28f, 0.98f), new Color(0.65f, 0.25f, 0.25f, 1f), 2.5f, 20);
            winBg.type = Image.Type.Sliced;

            GameObject titleObj = CreateText(winObj.transform, new Vector2(0, 255), new Vector2(600, 70), "GAME OVER", 58, FontStyle.Bold, new Color(0.96f, 0.35f, 0.35f), TextAnchor.MiddleCenter);

            // New Best Score banner
            m_GameOverNewBestObj = CreateText(winObj.transform, new Vector2(0, 200), new Vector2(500, 36), "NEW RECORD!", 24, FontStyle.Bold, new Color(1.0f, 0.84f, 0.22f), TextAnchor.MiddleCenter);
            m_GameOverNewBestObj.SetActive(false);

            // Final Score
            GameObject scoreLabel = CreateText(winObj.transform, new Vector2(0, 145), new Vector2(400, 32), "FINAL SCORE", 20, FontStyle.Bold, new Color(0.70f, 0.82f, 1f), TextAnchor.MiddleCenter);
            GameObject scoreVal = CreateText(winObj.transform, new Vector2(0, 90), new Vector2(500, 75), "0", 64, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            m_GameOverScoreText = scoreVal.GetComponent<Text>();

            // Best Score
            GameObject bestVal = CreateText(winObj.transform, new Vector2(0, 25), new Vector2(500, 40), "BEST: 0", 26, FontStyle.Bold, new Color(1.0f, 0.82f, 0.15f), TextAnchor.MiddleCenter);
            m_GameOverBestText = bestVal.GetComponent<Text>();

            // Sparkling Gold Revive Button (Free with limited chances)
            m_GameOverReviveBtn = CreateButton(winObj.transform, new Vector2(0, -45), new Vector2(450, 72), new Color(0.92f, 0.65f, 0.12f), "REVIVE (3 CHANCES LEFT)", 28);
            m_GameOverReviveText = m_GameOverReviveBtn.GetComponentInChildren<Text>();
            m_GameOverReviveBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                {
                    if (GameManager.Instance.ReviveGame())
                    {
                        m_GameOverPanel.SetActive(false);
                    }
                    else
                    {
                        HapticManager.TriggerLight();
                        FloatingText.Create(Vector3.zero, "No Revives Left!", new Color(1f, 0.35f, 0.35f), 6f);
                    }
                }
            });

            // Big Emerald Green "PLAY AGAIN" Button
            m_GameOverRestartBtn = CreateButton(winObj.transform, new Vector2(0, -128), new Vector2(450, 68), new Color(0.12f, 0.72f, 0.35f), "PLAY AGAIN", 30);
            m_GameOverRestartBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_GameOverPanel.SetActive(false);
                if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            });

            // Home Button
            m_GameOverHomeBtn = CreateIconButtonWithText(winObj.transform, new Vector2(0, -206), new Vector2(450, 62), new Color(0.18f, 0.42f, 0.82f), GemIconFactory.GetHomeSprite(), "HOME", 26);
            m_GameOverHomeBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_GameOverPanel.SetActive(false);
                OpenModeSelect();
            });

            m_GameOverPanel.SetActive(false);
        }

        // ==========================================
        // 🛠️ HELPER CREATORS
        // ==========================================
        private GameObject CreateText(Transform parent, Vector2 anchoredPos, Vector2 size, string text, int fontSize, FontStyle style, Color color, TextAnchor align, Font font = null)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Text txt = obj.AddComponent<Text>();
            txt.font = (font != null) ? font : m_DefaultFont;
            txt.text = text;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = align;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return obj;
        }

        private GameObject CreateButton(Transform parent, Vector2 anchoredPos, Vector2 size, Color bgColor, string label, int fontSize)
        {
            GameObject obj = new GameObject("Button");
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = SpriteFactory.GetRoundedButtonSprite(bgColor, new Color(1f, 1f, 1f, 0.28f), 0.05f);
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = true;

            Button btn = obj.AddComponent<Button>();

            if (!string.IsNullOrEmpty(label))
            {
                CreateText(obj.transform, Vector2.zero, size, label, fontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            }
            return obj;
        }

        private GameObject CreateCircularIconButton(Transform parent, Vector2 anchoredPos, float diameter, Sprite icon, Color bgColor)
        {
            GameObject obj = new GameObject("IconButton");
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(diameter, diameter);

            Image img = obj.AddComponent<Image>();
            img.sprite = SpriteFactory.GetCircleButtonSprite(bgColor, new Color(1f, 1f, 1f, 0.28f), 0.08f);
            img.color = Color.white;
            img.raycastTarget = true;
            obj.AddComponent<Button>();

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(obj.transform, false);
            RectTransform iRt = iconObj.AddComponent<RectTransform>();
            iRt.anchoredPosition = Vector2.zero;
            iRt.sizeDelta = new Vector2(diameter * 0.62f, diameter * 0.62f);
            Image iImg = iconObj.AddComponent<Image>();
            iImg.sprite = icon;
            iImg.raycastTarget = false;

            return obj;
        }

        private GameObject CreateIconButtonWithText(Transform parent, Vector2 anchoredPos, Vector2 size, Color bgColor, Sprite icon, string label, int fontSize)
        {
            GameObject obj = CreateButton(parent, anchoredPos, size, bgColor, "", fontSize);

            if (icon != null)
            {
                GameObject iconObj = new GameObject("BtnIcon");
                iconObj.transform.SetParent(obj.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchoredPosition = new Vector2(-size.x * 0.28f, 0);
                iconRt.sizeDelta = new Vector2(size.y * 0.58f, size.y * 0.58f);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.raycastTarget = false;
            }

            CreateText(obj.transform, new Vector2(icon != null ? 22 : 0, 0), new Vector2(size.x - 90, size.y), label, fontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            return obj;
        }

        private System.Collections.IEnumerator AnimateButtonPress(RectTransform targetRt, System.Action onComplete)
        {
            if (targetRt == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            HapticManager.TriggerLight();
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();

            Vector3 baseScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.07f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float scale = Mathf.Lerp(1.0f, 0.94f, progress);
                targetRt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            elapsed = 0f;
            duration = 0.10f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float scale = Mathf.Lerp(0.94f, 1.04f, progress);
                targetRt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            targetRt.localScale = baseScale;
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Smooth organic breathing animation for Home Screen mode cards and SPHERE BLAST 3D Logo.
    /// Exactly matches the playful living feel of Block Blast.
    /// </summary>
    public class HomeButtonBreather : MonoBehaviour
    {
        public RectTransform classicBtnRt;
        public RectTransform classicShadowRt;
        public RectTransform adventureBtnRt;
        public RectTransform adventureShadowRt;
        public RectTransform logoRt;
        public RectTransform crownStarGlintRt;

        private void Update()
        {
            float t = Time.unscaledTime;

            // 1. Classic & Adventure Cards Organic Breathing
            if (classicBtnRt != null)
            {
                float clScale = 1.0f + 0.016f * Mathf.Sin(t * 2.4f);
                classicBtnRt.localScale = new Vector3(clScale, clScale, 1f);
                if (classicShadowRt != null)
                {
                    classicShadowRt.localScale = new Vector3(clScale, clScale, 1f);
                }
            }

            if (adventureBtnRt != null)
            {
                float advScale = 1.0f + 0.020f * Mathf.Sin(t * 2.4f + 1.4f);
                adventureBtnRt.localScale = new Vector3(advScale, advScale, 1f);
                if (adventureShadowRt != null)
                {
                    adventureShadowRt.localScale = new Vector3(advScale, advScale, 1f);
                }
            }

            // 2. SPHERE BLAST 3D Candy Logo Gentle Breathing Pulse & Living Sway
            if (logoRt != null)
            {
                float logoPulse = 1.0f + 0.016f * Mathf.Sin(t * 2.4f);
                float logoTilt = Mathf.Sin(t * 1.8f) * 1.0f;
                logoRt.localScale = new Vector3(logoPulse, logoPulse, 1f);
                logoRt.localRotation = Quaternion.Euler(0, 0, logoTilt);
            }

            // 3. Twinkling Diamond Star on Crown Peak
            if (crownStarGlintRt != null)
            {
                crownStarGlintRt.localRotation = Quaternion.Euler(0, 0, t * 50f);
                float glintPulse = 0.85f + 0.35f * Mathf.Sin(t * 3.5f);
                crownStarGlintRt.localScale = new Vector3(glintPulse, glintPulse, 1f);
            }
        }
    }
}
