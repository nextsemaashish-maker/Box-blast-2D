using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoxBlast
{
    public enum BallSkinType
    {
        Classic = 0,        // Default 3D Glossy Marble
        StarCore = 1,       // Glowing Golden Star Core (Level 1)
        DiamondCrystal = 2, // Faceted Prismatic Diamond (Level 2)
        NeonPulse = 3,      // Cyber Neon Orbital Ring (Level 3)
        GalaxySwirl = 4,    // Cosmic Spiral Galaxy Nebula (Level 4)
        SunBurst = 5,       // Fiery Solar Corona Flares (Level 5)
        HeartPearl = 6,     // Glowing Ruby Heart Emblem (Level 6)
        LightningVolt = 7,  // High-Voltage Lightning Plasma (Level 7)
        GoldenCrown = 8,    // Royal Imperial Golden Crown (Level 8)
        RainbowPrism = 9    // Iridescent Rainbow Chromatic Sheen (Level 10)
    }

    [System.Serializable]
    public class SkinInfo
    {
        public BallSkinType Type;
        public string Name;
        public string Description;
        public int UnlockAdventureLevel; // 0 = default, 1 = lvl 1, 2 = lvl 2...
        public string Emoji;
    }

    /// <summary>
    /// Manages ball skin progression, unlocking, equipping, and live board updates.
    /// Adventure mode awards a new unique ball skin upon level completion!
    /// </summary>
    public static class SkinManager
    {
        private const string PREF_EQUIPPED_SKIN = "EquippedBallSkin";
        private const string PREF_SKIN_UNLOCKED_PREFIX = "SkinUnlocked_";

        public static event Action<BallSkinType> OnSkinChanged;

        private static readonly List<SkinInfo> s_Skins = new List<SkinInfo>
        {
            new SkinInfo
            {
                Type = BallSkinType.Classic,
                Name = "Classic Marble",
                Description = "Sleek 3D glossy crystal sphere with specular highlights.",
                UnlockAdventureLevel = 0,
                Emoji = ""
            },
            new SkinInfo
            {
                Type = BallSkinType.StarCore,
                Name = "Star Core",
                Description = "A radiant golden star blazing inside the glass sphere.",
                UnlockAdventureLevel = 2,
                Emoji = ""
            },
            new SkinInfo
            {
                Type = BallSkinType.DiamondCrystal,
                Name = "Diamond Crystal",
                Description = "Faceted prismatic gemstone cuts with brilliant reflections.",
                UnlockAdventureLevel = 4,
                Emoji = ""
            },
            new SkinInfo
            {
                Type = BallSkinType.NeonPulse,
                Name = "Neon Pulse",
                Description = "High-tech cyber neon ring orbiting the glowing core.",
                UnlockAdventureLevel = 6,
                Emoji = ""
            },
            new SkinInfo
            {
                Type = BallSkinType.GalaxySwirl,
                Name = "Galaxy Swirl",
                Description = "A swirling cosmic spiral nebula with shimmering stardust.",
                UnlockAdventureLevel = 8,
                Emoji = ""
            },
            new SkinInfo
            {
                Type = BallSkinType.SunBurst,
                Name = "Sun Burst",
                Description = "Blazing solar flares radiating from a luminous core.",
                UnlockAdventureLevel = 10,
                Emoji = ""
            },
            new SkinInfo
            {
                Type = BallSkinType.HeartPearl,
                Name = "Heart Pearl",
                Description = "A warm, glowing ruby heart pulsing with love & magic.",
                UnlockAdventureLevel = 12,
                Emoji = ""
            },
            new SkinInfo
            {
                Type = BallSkinType.LightningVolt,
                Name = "Lightning Volt",
                Description = "Crackling electric thunderbolts discharging pure energy.",
                UnlockAdventureLevel = 14,
                Emoji = ""
            },
            new SkinInfo
            {
                Type = BallSkinType.GoldenCrown,
                Name = "Golden Crown",
                Description = "An imperial royal golden crown emblem for champions.",
                UnlockAdventureLevel = 17,
                Emoji = ""
            },
            new SkinInfo
            {
                Type = BallSkinType.RainbowPrism,
                Name = "Rainbow Prism",
                Description = "Holographic chromatic rainbow sheen with iridescent colors.",
                UnlockAdventureLevel = 20,
                Emoji = ""
            }
        };

        public static List<SkinInfo> GetAllSkins() => s_Skins;

        public static SkinInfo GetSkinInfo(BallSkinType type)
        {
            for (int i = 0; i < s_Skins.Count; i++)
            {
                if (s_Skins[i].Type == type) return s_Skins[i];
            }
            return s_Skins[0];
        }

        public static SkinInfo GetSkinForLevel(int adventureLevel)
        {
            for (int i = 0; i < s_Skins.Count; i++)
            {
                if (s_Skins[i].UnlockAdventureLevel == adventureLevel)
                {
                    return s_Skins[i];
                }
            }
            return null;
        }

        public static BallSkinType CurrentSkin
        {
            get
            {
                string saved = PlayerPrefs.GetString(PREF_EQUIPPED_SKIN, BallSkinType.Classic.ToString());
                if (Enum.TryParse(saved, out BallSkinType skin) && IsSkinUnlocked(skin))
                {
                    return skin;
                }
                return BallSkinType.Classic;
            }
            private set
            {
                PlayerPrefs.SetString(PREF_EQUIPPED_SKIN, value.ToString());
                PlayerPrefs.Save();
            }
        }

        public static bool IsSkinUnlocked(BallSkinType type)
        {
            if (type == BallSkinType.Classic) return true;
            SkinInfo info = GetSkinInfo(type);
            if (info != null && info.UnlockAdventureLevel > 0)
            {
                return LevelManager.GetLevelStars(GameModeType.Adventure, info.UnlockAdventureLevel) > 0;
            }
            return false;
        }

        public static void UnlockSkin(BallSkinType type)
        {
            PlayerPrefs.SetInt(PREF_SKIN_UNLOCKED_PREFIX + type, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Checks if completing the given Adventure level awards a new ball skin.
        /// Unlocks the skin and returns true with skin info if newly unlocked.
        /// </summary>
        public static bool CheckAndUnlockLevelSkin(int adventureLevel, out SkinInfo newlyUnlocked)
        {
            newlyUnlocked = null;
            for (int i = 0; i < s_Skins.Count; i++)
            {
                SkinInfo skin = s_Skins[i];
                if (skin.UnlockAdventureLevel == adventureLevel)
                {
                    bool wasAlreadyNotified = PlayerPrefs.GetInt(PREF_SKIN_UNLOCKED_PREFIX + skin.Type, 0) == 1;
                    if (!wasAlreadyNotified)
                    {
                        UnlockSkin(skin.Type);
                        newlyUnlocked = skin;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Equips a ball skin and immediately updates all active blocks on the grid and in the spawner dock.
        /// </summary>
        public static void EquipSkin(BallSkinType type)
        {
            if (!IsSkinUnlocked(type)) return;

            CurrentSkin = type;
            SpriteFactory.ClearSpriteCache();
            OnSkinChanged?.Invoke(type);

            // Live update all blocks currently on screen
            if (GridManager.Instance != null)
            {
                GridManager.Instance.RefreshAllCellSprites();
            }

            if (ShapeSpawner.Instance != null)
            {
                ShapeSpawner.Instance.RefreshActiveShapeSprites();
            }

            HapticManager.TriggerLight();
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPlace();
            }
        }
    }
}
