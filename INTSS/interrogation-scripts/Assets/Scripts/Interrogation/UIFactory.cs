using UnityEngine;
using UnityEngine.UI;

namespace F3.Interrogation
{
    // =====================================================================
    // UIFactory.cs
    // ---------------------------------------------------------------------
    // Small helpers that build UGUI objects (panels, text, circles) from code.
    // The whole interrogation UI is created at runtime with these, which means
    // you do NOT have to build a Canvas, prefabs or an EventSystem by hand.
    // =====================================================================
    public static class UIFactory
    {
        static Sprite circleSprite;

        /// <summary>
        /// Unity's built-in font for the (legacy) UnityEngine.UI.Text component.
        /// In Unity 6 the built-in font is called "LegacyRuntime.ttf"
        /// (older Unity versions called it "Arial.ttf").
        /// Reference: https://docs.unity3d.com/ScriptReference/Resources.GetBuiltinResource.html
        /// </summary>
        public static Font DefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return font;
        }

        public static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>A coloured rectangle (or a sprite if you pass one). Not clickable on purpose.</summary>
        public static Image MakeImage(string name, Transform parent, Color color, Sprite sprite = null)
        {
            RectTransform rt = MakeRect(name, parent);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false; // we read input ourselves, UI never needs to block clicks
            return image;
        }

        public static Text MakeText(string name, Transform parent, Font font, int size, Color color,
                                    TextAnchor anchor, FontStyle style = FontStyle.Normal)
        {
            RectTransform rt = MakeRect(name, parent);
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Fill the parent, with optional inner margins in pixels.</summary>
        public static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Position from the parent's top-left corner. y is usually negative (down).</summary>
        public static void PlaceTopLeft(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
        }

        /// <summary>Position relative to the parent's centre.</summary>
        public static void PlaceCentered(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
        }

        /// <summary>A white anti-aliased circle sprite, generated once. Tint it with Image.color.</summary>
        public static Sprite Circle()
        {
            if (circleSprite != null) return circleSprite;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[size * size];
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance); // ~1 pixel soft edge
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return circleSprite;
        }
    }
}
