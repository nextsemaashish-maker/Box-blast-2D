using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BoxBlast
{
    /// <summary>
    /// Automatic scene initializer. When the scene starts, it ensures all required 
    /// managers, camera settings, and event systems exist and are fully wired up.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrapper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            if (FindAnyObjectByType<GameBootstrapper>() == null)
            {
                GameObject bootObj = new GameObject("GameBootstrapper");
                bootObj.AddComponent<GameBootstrapper>();
            }
        }

        private void Awake()
        {
            SetupEnvironment();
        }

        public static void SetupEnvironment()
        {
            // 1. Camera Setup
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = (Screen.width > Screen.height) ? 5.4f : 6.6f;
            cam.backgroundColor = new Color(0.22f, 0.29f, 0.65f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.allowMSAA = true;
            cam.allowHDR = true;

            QualitySettings.antiAliasing = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

            // Mobile display optimization: prevent screen sleep while thinking
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            QualitySettings.vSyncCount = 0; // Disable vSync lock so mobile targetFrameRate is respected

            // Target high refresh rate displays (90Hz, 120Hz ProMotion) for silky smooth touch response
#if UNITY_2021_2_OR_NEWER
            int deviceRefresh = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            Application.targetFrameRate = Mathf.Max(60, Mathf.Min(120, deviceRefresh > 0 ? deviceRefresh : 120));
#else
            int deviceRefresh = Screen.currentResolution.refreshRate;
            Application.targetFrameRate = Mathf.Max(60, Mathf.Min(120, deviceRefresh > 0 ? deviceRefresh : 120));
#endif

            if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            // 1b. Ambient Background (Atmospheric Radial Glow + Floating Stardust)
            if (FindAnyObjectByType<AmbientBackground>() == null)
            {
                GameObject ambObj = new GameObject("AmbientBackground");
                ambObj.AddComponent<AmbientBackground>();
            }

            // 1c. Particle FX Manager (Zero-allocation procedural bursts and shockwaves)
            if (FindAnyObjectByType<ParticleFXManager>() == null)
            {
                GameObject fxObj = new GameObject("ParticleFXManager");
                fxObj.AddComponent<ParticleFXManager>();
            }

            // 1d. Line Blast Animator (Dynamic laser beam sweep and wave burst effects)
            if (FindAnyObjectByType<LineBlastAnimator>() == null)
            {
                GameObject animObj = new GameObject("LineBlastAnimator");
                animObj.AddComponent<LineBlastAnimator>();
            }

            // 2. Event System (Required for UI clicks)
            EventSystem es = FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                InputSystemUIInputModule uiMod = esObj.AddComponent<InputSystemUIInputModule>();
                uiMod.AssignDefaultActions();
#else
                esObj.AddComponent<StandaloneInputModule>();
#endif
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                InputSystemUIInputModule uiMod = es.GetComponent<InputSystemUIInputModule>();
                if (uiMod == null)
                {
                    StandaloneInputModule oldMod = es.GetComponent<StandaloneInputModule>();
                    if (oldMod != null) DestroyImmediate(oldMod);
                    uiMod = es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                uiMod.AssignDefaultActions();
#endif
            }

            // 3. Sound Manager
            if (FindAnyObjectByType<SoundManager>() == null)
            {
                GameObject soundObj = new GameObject("SoundManager");
                soundObj.AddComponent<SoundManager>();
            }

            // 4. Grid Manager
            if (FindAnyObjectByType<GridManager>() == null)
            {
                GameObject gridObj = new GameObject("GridManager");
                gridObj.AddComponent<GridManager>();
            }

            // 5. Shape Spawner
            if (FindAnyObjectByType<ShapeSpawner>() == null)
            {
                GameObject spawnerObj = new GameObject("ShapeSpawner");
                spawnerObj.AddComponent<ShapeSpawner>();
            }

            // 6. Game Manager
            if (FindAnyObjectByType<GameManager>() == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            // 7. Level Manager
            if (FindAnyObjectByType<LevelManager>() == null)
            {
                GameObject lmObj = new GameObject("LevelManager");
                lmObj.AddComponent<LevelManager>();
            }

            // 8. UI Manager (Constructed after all game logic managers are ready)
            if (FindAnyObjectByType<UIManager>() == null)
            {
                GameObject uiObj = new GameObject("UIManager");
                uiObj.AddComponent<UIManager>();
            }

            // 9. Launch Flow: Show Home Page (Mode Selection) on game start
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenModeSelect();
            }
        }
    }
}
