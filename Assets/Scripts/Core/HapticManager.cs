using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// Controls mobile haptic vibrations (light, medium, heavy) for touch feedback
    /// when placing blocks, blasting lines, triggering combos, and using boosters.
    /// Includes user preference toggle stored in PlayerPrefs.
    /// </summary>
    public static class HapticManager
    {
        private const string PrefsHaptics = "BoxBlast_HapticsEnabled";

        public static bool IsHapticsEnabled { get; private set; } = true;

        static HapticManager()
        {
            IsHapticsEnabled = PlayerPrefs.GetInt(PrefsHaptics, 1) == 1;
        }

        public static void ToggleHaptics()
        {
            IsHapticsEnabled = !IsHapticsEnabled;
            PlayerPrefs.SetInt(PrefsHaptics, IsHapticsEnabled ? 1 : 0);
            PlayerPrefs.Save();

            if (IsHapticsEnabled)
            {
                TriggerLight();
            }
        }

        public static void TriggerLight()
        {
            if (!IsHapticsEnabled) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        public static void TriggerMedium()
        {
            if (!IsHapticsEnabled) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        public static void TriggerHeavy()
        {
            if (!IsHapticsEnabled) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
