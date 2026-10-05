$path = "c:\Users\nextsem\Box-blast-2D\Assets\Scripts\UI\UIManager.cs"
$content = [System.IO.File]::ReadAllText($path)

# 1. Update BuildVictoryModal
$oldVictoryStart = "private void BuildVictoryModal()"
$oldDefeatStart = "private void BuildDefeatModal()"
$vStartIdx = $content.IndexOf($oldVictoryStart)
$dStartIdx = $content.IndexOf($oldDefeatStart)

if ($vStartIdx -ge 0 -and $dStartIdx -gt $vStartIdx) {
    $newVictoryCode = @"
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

            GameObject titleObj = CreateText(winObj.transform, new Vector2(0, 275), new Vector2(600, 60), "VICTORY! 🎉", 54, FontStyle.Bold, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter);
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

            GameObject subObj = CreateText(winObj.transform, new Vector2(0, 165), new Vector2(600, 32), "Next Level Unlocked! 🔓", 21, FontStyle.Normal, new Color(0.8f, 0.9f, 1f), TextAnchor.MiddleCenter);
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
            CreateText(m_VictorySkinRewardObj.transform, new Vector2(0, 16), new Vector2(300, 26), "🎉 NEW BALL SKIN UNLOCKED!", 18, FontStyle.Bold, new Color(1f, 0.85f, 0.25f), TextAnchor.MiddleCenter);
            GameObject nameTxtObj = CreateText(m_VictorySkinRewardObj.transform, new Vector2(0, -14), new Vector2(300, 26), "STAR CORE BALL", 18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            m_VictorySkinNameText = nameTxtObj.GetComponent<Text>();

            // Equip Button (Right)
            GameObject equipBtn = CreateButton(m_VictorySkinRewardObj.transform, new Vector2(205, 0), new Vector2(135, 52), new Color(0.12f, 0.68f, 0.35f), "EQUIP ✨", 18);
            m_VictorySkinEquipBtn = equipBtn.GetComponent<Button>();
            m_VictorySkinEquipBtnText = equipBtn.GetComponentInChildren<Text>();

            m_VictorySkinRewardObj.SetActive(false);

            // Next Level Button
            GameObject nextBtn = CreateButton(winObj.transform, new Vector2(0, -5), new Vector2(460, 74), new Color(0.12f, 0.72f, 0.32f), "NEXT LEVEL ▶", 30);
            m_VictoryNextBtnObj = nextBtn;
            nextBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_VictoryModal.SetActive(false);
                if (LevelManager.Instance != null) LevelManager.Instance.NextLevel();
            });

            // Level Select Grid Button
            GameObject lvlSelectBtn = CreateButton(winObj.transform, new Vector2(0, -88), new Vector2(460, 64), new Color(0.18f, 0.38f, 0.72f), "LEVEL SELECT 🗺️", 25);
            lvlSelectBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_VictoryModal.SetActive(false);
                if (LevelManager.Instance != null) OpenLevelSelect(LevelManager.Instance.CurrentMode);
            });

            // Ball Designs Wardrobe Button
            GameObject wardrobeBtn = CreateButton(winObj.transform, new Vector2(0, -162), new Vector2(460, 60), new Color(0.55f, 0.25f, 0.85f), "🔮 BALL DESIGNS WARDROBE", 23);
            wardrobeBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_VictoryModal.SetActive(false);
                OpenSkinWardrobe();
            });

            // Mode Select Button
            GameObject modeBtn = CreateButton(winObj.transform, new Vector2(0, -232), new Vector2(460, 56), new Color(0.25f, 0.28f, 0.40f), "MAIN MENU 🏠", 22);
            modeBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                m_VictoryModal.SetActive(false);
                OpenModeSelect();
            });

            m_VictoryModal.SetActive(false);
        }

        
"@
    $content = $content.Substring(0, $vStartIdx) + $newVictoryCode + $content.Substring($dStartIdx)
    Write-Output "BuildVictoryModal replaced."
} else {
    Write-Output "Could not locate BuildVictoryModal"
}

# 2. Add Wardrobe methods right after BuildDefeatModal
$wardrobeCode = @"


        // ==========================================
        // 🔮 BALL DESIGNS WARDROBE MODAL
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
            CreateText(winObj.transform, new Vector2(0, 355), new Vector2(600, 50), "🔮 BALL DESIGNS WARDROBE", 38, FontStyle.Bold, new Color(1.0f, 0.85f, 0.25f), TextAnchor.MiddleCenter);

            // Subtitle
            CreateText(winObj.transform, new Vector2(0, 310), new Vector2(620, 32), "Complete Adventure levels to unlock new stunning ball designs!", 19, FontStyle.Normal, new Color(0.70f, 0.82f, 1f, 0.9f), TextAnchor.MiddleCenter);

            // Close Button (✕)
            GameObject closeBtn = CreateButton(winObj.transform, new Vector2(315, 355), new Vector2(56, 56), new Color(0.85f, 0.25f, 0.25f), "✕", 28);
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
                string titleText = $"{skin.Emoji} {skin.Name}";
                CreateText(card.transform, new Vector2(8, 18), new Vector2(170, 32), titleText, 20, FontStyle.Bold, isUnlocked ? Color.white : new Color(0.6f, 0.65f, 0.75f), TextAnchor.MiddleLeft);

                string statusText = isUnlocked ? "Unlocked" : $"🔒 Beat Level {skin.UnlockAdventureLevel}";
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
"@

$dEndIdx = $content.IndexOf("m_DefeatModal.SetActive(false);`n        }")
if ($dEndIdx -lt 0) {
    $dEndIdx = $content.IndexOf("m_DefeatModal.SetActive(false);`r`n        }")
}
if ($dEndIdx -ge 0) {
    $insertPos = $dEndIdx + "m_DefeatModal.SetActive(false);`n        }".Length
    $content = $content.Substring(0, $insertPos) + $wardrobeCode + $content.Substring($insertPos)
    Write-Output "Wardrobe methods added."
} else {
    Write-Output "Could not locate end of BuildDefeatModal"
}

[System.IO.File]::WriteAllText($path, $content)
Write-Output "Successfully updated UIManager.cs with Victory Skin Rewards and Wardrobe Modal!"
