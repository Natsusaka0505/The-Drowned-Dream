using UnityEngine;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>原型 UI 產生工具。正式美術進來後可改為 prefab。</summary>
    public static class UIFactory
    {
        /// <summary>預設字型快取。</summary>
        private static Font s_font;

        /// <summary>Unity 內建字型（中文由系統字型 fallback）。</summary>
        public static Font DefaultFont
        {
            get
            {
                if (s_font == null) s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return s_font;
            }
        }

        /// <summary>建立指定錨點 / 位置 / 大小的 RectTransform。</summary>
        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>建立撐滿父物件的 RectTransform。</summary>
        public static RectTransform Stretch(string name, Transform parent)
        {
            return Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        /// <summary>加上純色 Image（不接收點擊）。</summary>
        public static Image Image(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>加上帶陰影的文字。</summary>
        public static Text Text(RectTransform rt, string content, int fontSize, TextAnchor anchor, Color color)
        {
            var text = rt.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }
    }

    /// <summary>簡易數值條：以 anchorMax.x 控制填滿比例，不需要 sprite。</summary>
    public class UIBar
    {
        /// <summary>填滿區塊。</summary>
        private readonly RectTransform _fill;
        /// <summary>「失去的最大值」區塊（可為空）。</summary>
        private readonly RectTransform _cap;
        /// <summary>數值文字。</summary>
        private readonly Text _label;

        /// <summary>建立數值條（左上角錨點）。</summary>
        public UIBar(Transform parent, string name, Vector2 pos, Vector2 size, Color color, bool withCap = false)
        {
            var root = UIFactory.Rect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);
            UIFactory.Image(root, new Color(0f, 0f, 0f, 0.6f));

            if (withCap)
            {
                _cap = UIFactory.Stretch("Lost", root);
                UIFactory.Image(_cap, new Color(0.25f, 0.05f, 0.1f, 0.9f));
                _cap.anchorMin = new Vector2(1f, 0f);
            }

            _fill = UIFactory.Stretch("Fill", root);
            _fill.offsetMin = new Vector2(2f, 2f);
            _fill.offsetMax = new Vector2(-2f, -2f);
            UIFactory.Image(_fill, color);

            var labelRt = UIFactory.Stretch("Label", root);
            labelRt.offsetMin = new Vector2(8f, 0f);
            _label = UIFactory.Text(labelRt, "", 20, TextAnchor.MiddleLeft, Color.white);
        }

        /// <summary>設定填滿比例與文字。</summary>
        public void Set(float ratio, string label)
        {
            _fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            _label.text = label;
        }

        /// <summary>設定最大值上限比例（右側以暗色表示失去的部分）。</summary>
        public void SetCap(float capRatio)
        {
            if (_cap != null) _cap.anchorMin = new Vector2(Mathf.Clamp01(capRatio), 0f);
        }
    }
}
