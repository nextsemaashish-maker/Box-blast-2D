#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BoxBlast.Editor
{
    public static class BoxBlastSetupEditor
    {
        [MenuItem("Box Blast/Setup Current Scene", false, 1)]
        public static void SetupCurrentScene()
        {
            // 1. Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
                Undo.RegisterCreatedObjectUndo(camObj, "Create Camera");
            }

            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.09f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            // 2. Event System
            EventSystem es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<InputSystemUIInputModule>();
#else
                esObj.AddComponent<StandaloneInputModule>();
#endif
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }

            // 3. GameBootstrapper
            GameBootstrapper bootstrapper = Object.FindFirstObjectByType<GameBootstrapper>();
            if (bootstrapper == null)
            {
                GameObject bootObj = new GameObject("GameBootstrapper");
                bootstrapper = bootObj.AddComponent<GameBootstrapper>();
                Undo.RegisterCreatedObjectUndo(bootObj, "Create GameBootstrapper");
            }

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            EditorUtility.DisplayDialog(
                "Box Blast 2D Setup",
                "Scene configured successfully!\n\nAll components, Camera settings, and GameBootstrapper are now active.\nPress 'Play' in Unity to test your Box Blast 2D game!",
                "Awesome!"
            );
        }

        [MenuItem("Box Blast/Build Android APK", false, 2)]
        public static void BuildAndroidAPK()
        {
            string buildFolder = System.IO.Path.Combine(Application.dataPath, "..", "Builds");
            if (!System.IO.Directory.Exists(buildFolder))
            {
                System.IO.Directory.CreateDirectory(buildFolder);
            }

            string apkPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(buildFolder, "BoxBlast2D.apk"));

            // 1. Ensure active build target is Android
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[BoxBlast] Switching active build target to Android...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // 2. Optimization and package settings for Android build
            UnityEditor.Build.NamedBuildTarget androidTarget = UnityEditor.Build.NamedBuildTarget.Android;
            PlayerSettings.companyName = "NextSem";
            PlayerSettings.productName = "Box Blast 2D";
            PlayerSettings.SetApplicationIdentifier(androidTarget, "com.nextsem.boxblast2d");
            PlayerSettings.SetManagedStrippingLevel(androidTarget, ManagedStrippingLevel.Minimal);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[BoxBlast] Starting Android APK build to: {apkPath}");
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[BoxBlast] Build Succeeded! Output: {apkPath} ({report.summary.totalSize} bytes)");
                EditorUtility.RevealInFinder(apkPath);
                EditorUtility.DisplayDialog(
                    "APK Ready!",
                    $"APK successfully created at:\n\n{apkPath}\n\nYou can now copy this APK to your phone and install it!",
                    "Open Folder"
                );
            }
            else
            {
                string firstError = "";
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        {
                            firstError = msg.content;
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(firstError)) break;
                }

                Debug.LogError($"[BoxBlast] Build Failed: {report.summary.result}. {firstError}");

                string userMessage = $"Build Status: {report.summary.result}\n\n";
                if (!string.IsNullOrEmpty(firstError))
                {
                    userMessage += $"Details: {firstError}\n\n";
                }

                if (firstError.Contains("Application Control") || firstError.Contains("0x800711C7"))
                {
                    userMessage += "Tip: Windows 11 Smart App Control has blocked an internal Unity file. Please temporarily turn off Smart App Control in Windows Settings > Privacy & security > Windows Security > App & browser control.";
                }
                else
                {
                    userMessage += "Please check the Unity Console window (Ctrl+Shift+C) for the exact error logs.";
                }

                EditorUtility.DisplayDialog("Build Notice", userMessage, "OK");
            }
        }
    }
}
#endif
