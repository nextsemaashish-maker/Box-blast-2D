using System.Collections;
using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// Floats and fades upward showing combo points and streak messages.
    /// Built using native TextMesh for 100% standalone reliability.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        private TextMesh m_TextMesh;

        public static void Create(Vector3 worldPos, string text, Color color, float size = 5f)
        {
            GameObject obj = new GameObject("FloatingText");
            obj.transform.position = worldPos;

            FloatingText ft = obj.AddComponent<FloatingText>();
            ft.Initialize(text, color, size, false, Color.clear, 1.4f);
        }

        public static void CreateBadge(Vector3 worldPos, string text, Color textColor, Color badgeColor, float size = 3.4f)
        {
            GameObject obj = new GameObject("FloatingBadge");
            obj.transform.position = worldPos;

            FloatingText ft = obj.AddComponent<FloatingText>();
            ft.Initialize(text, textColor, size, true, badgeColor, 0.75f);
        }

        public void Initialize(string text, Color color, float size, bool withBadge = false, Color badgeColor = default, float floatHeight = 1.4f)
        {
            m_TextMesh = gameObject.AddComponent<TextMesh>();
            MeshRenderer mr = GetComponent<MeshRenderer>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);

            if (font != null)
            {
                m_TextMesh.font = font;
                mr.material = font.material;
            }

            m_TextMesh.text = text;
            m_TextMesh.characterSize = size * 0.05f;
            m_TextMesh.fontSize = 44;
            m_TextMesh.color = color;
            m_TextMesh.anchor = TextAnchor.MiddleCenter;
            m_TextMesh.alignment = TextAlignment.Center;
            mr.sortingOrder = 32;

            if (withBadge)
            {
                GameObject bgObj = new GameObject("BadgeBg");
                bgObj.transform.SetParent(transform, false);
                bgObj.transform.localPosition = new Vector3(0f, 0f, 0.05f);

                SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.GetRoundedButtonSprite(badgeColor, new Color(1.0f, 0.85f, 0.25f, 0.85f), 0.08f);
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.sortingOrder = 31;

                Bounds bounds = mr.bounds;
                float bgW = Mathf.Max(bounds.size.x + 0.40f, 1.6f);
                float bgH = Mathf.Max(bounds.size.y + 0.20f, 0.55f);
                sr.size = new Vector2(bgW, bgH);
            }

            StartCoroutine(AnimateFloat(floatHeight));
        }

        private IEnumerator AnimateFloat(float floatHeight)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + new Vector3(0f, floatHeight, 0f);
            Color startColor = m_TextMesh.color;
            SpriteRenderer bgSr = GetComponentInChildren<SpriteRenderer>();
            Color startBgColor = (bgSr != null) ? bgSr.color : Color.clear;
            Vector3 baseScale = Vector3.one;

            float elapsed = 0f;
            float duration = 0.95f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Position: ease out upward float
                transform.position = Vector3.Lerp(startPos, endPos, Mathf.Sin(t * Mathf.PI * 0.5f));

                // Scale: gentle, sleek pop
                float scalePop = 1.0f;
                if (t < 0.20f)
                {
                    scalePop = Mathf.Lerp(0.5f, 1.15f, t / 0.20f);
                }
                else if (t < 0.38f)
                {
                    scalePop = Mathf.Lerp(1.15f, 1.0f, (t - 0.20f) / 0.18f);
                }
                transform.localScale = baseScale * scalePop;

                // Alpha fade out in second half
                float alpha = (t > 0.45f) ? Mathf.Lerp(1.0f, 0f, (t - 0.45f) / 0.55f) : 1.0f;
                Color c = startColor;
                c.a = startColor.a * alpha;
                m_TextMesh.color = c;

                if (bgSr != null)
                {
                    Color bc = startBgColor;
                    bc.a = startBgColor.a * alpha;
                    bgSr.color = bc;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
