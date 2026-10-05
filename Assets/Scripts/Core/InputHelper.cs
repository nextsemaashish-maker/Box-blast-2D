using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BoxBlast
{
    /// <summary>
    /// Robust cross-compatible input helper.
    /// Safely handles New Input System and Old Input Manager without throwing InvalidOperationException.
    /// Prioritizes mobile touch events over fallback mouse pointers, with Android back button support.
    /// </summary>
    public static class InputHelper
    {
        private static Vector2 s_LastKnownPointerPosition = Vector2.zero;

        public static bool IsBackButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                return true;
            }
#endif
            return false;
        }

        public static bool IsMobileTouchActive()
        {
            return Application.isMobilePlatform || IsTouchActive();
        }

        public static bool IsTouchActive()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                return Touchscreen.current.primaryTouch.press.isPressed ||
                       Touchscreen.current.primaryTouch.press.wasPressedThisFrame ||
                       Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            }
            return false;
#else
            return Input.touchCount > 0;
#endif
        }

        public static Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var primary = Touchscreen.current.primaryTouch;
                if (primary.press.isPressed || primary.isInProgress || primary.press.wasPressedThisFrame || primary.press.wasReleasedThisFrame)
                {
                    Vector2 pos = primary.position.ReadValue();
                    if (pos.sqrMagnitude > 0.01f)
                    {
                        s_LastKnownPointerPosition = pos;
                        return s_LastKnownPointerPosition;
                    }
                }
            }
            if (Mouse.current != null)
            {
                Vector2 mPos = Mouse.current.position.ReadValue();
                if (mPos.sqrMagnitude > 0.01f)
                {
                    s_LastKnownPointerPosition = mPos;
                    return s_LastKnownPointerPosition;
                }
            }
            return s_LastKnownPointerPosition;
#else
            if (Input.touchCount > 0)
            {
                s_LastKnownPointerPosition = Input.GetTouch(0).position;
                return s_LastKnownPointerPosition;
            }
            if (Input.mousePosition != Vector3.zero)
            {
                s_LastKnownPointerPosition = Input.mousePosition;
            }
            return s_LastKnownPointerPosition;
#endif
        }

        public static bool IsPointerDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }
            return false;
#else
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                return true;
            }
            return Input.GetMouseButtonDown(0);
#endif
        }

        public static bool IsPointerPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var primary = Touchscreen.current.primaryTouch;
                if (primary.press.isPressed || primary.isInProgress)
                {
                    return true;
                }
            }
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                return true;
            }
            return false;
#else
            if (Input.touchCount > 0)
            {
                var phase = Input.GetTouch(0).phase;
                return phase == TouchPhase.Moved || phase == TouchPhase.Stationary || phase == TouchPhase.Began;
            }
            return Input.GetMouseButton(0);
#endif
        }

        public static bool IsPointerUp()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                return true;
            }
            return false;
#else
            if (Input.touchCount > 0)
            {
                var phase = Input.GetTouch(0).phase;
                return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
            }
            return Input.GetMouseButtonUp(0);
#endif
        }

        public static bool IsPointerOverUI()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return true;

            if (IsTouchActive())
            {
#if !ENABLE_INPUT_SYSTEM
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.touches[i].fingerId))
                    {
                        return true;
                    }
                }
#endif
            }
            return false;
        }
    }
}
