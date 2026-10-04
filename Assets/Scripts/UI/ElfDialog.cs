using UnityEngine;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>
    /// 精靈提示（左下角）：精靈頭像 + 對話框。進 Boss 房前所有提示文字都由精靈說（由 HUD 轉交）。
    /// 出現時淡入、精靈輕輕上下飄，時間到淡出。
    /// </summary>
    public class ElfDialog
    {
        /// <summary>整組根物件（淡入淡出用）。</summary>
        private readonly CanvasGroup _group;
        /// <summary>精靈頭像（上下飄動）。</summary>
        private readonly RectTransform _elf;
        /// <summary>對話文字。</summary>
        private readonly Text _text;
        /// <summary>精靈頭像原始位置。</summary>
        private readonly Vector2 _elfBasePos;
        /// <summary>剩餘顯示秒數。</summary>
        private float _timer;

        /// <summary>淡出秒數。</summary>
        private const float FadeTime = 0.3f;

        /// <summary>在 parent 左下角建立精靈對話框（預設隱藏）。</summary>
        public ElfDialog(Transform parent, Sprite elfSprite)
        {
            var root = UIFactory.Rect("ElfDialog", parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(24f, 56f), new Vector2(760f, 150f));
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            // 對話框：深色底 + 淡色邊框
            var bubble = UIFactory.Rect("Bubble", root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(130f, 20f), new Vector2(620f, 110f));
            UIFactory.Image(bubble, new Color(0.75f, 0.92f, 0.95f, 0.9f));
            var inner = UIFactory.Stretch("Inner", bubble);
            inner.offsetMin = new Vector2(3f, 3f);
            inner.offsetMax = new Vector2(-3f, -3f);
            UIFactory.Image(inner, new Color(0.04f, 0.08f, 0.14f, 0.92f));
            var textRt = UIFactory.Stretch("Text", inner);
            textRt.offsetMin = new Vector2(22f, 10f);
            textRt.offsetMax = new Vector2(-16f, -10f);
            _text = UIFactory.Text(textRt, "", 24, TextAnchor.MiddleLeft, new Color(0.92f, 0.97f, 1f));

            // 精靈頭像（疊在對話框左側）
            _elf = UIFactory.Rect("Elf", root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0f, 0f), new Vector2(150f, 150f));
            var img = _elf.gameObject.AddComponent<Image>();
            img.sprite = elfSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.enabled = elfSprite != null;
            _elfBasePos = _elf.anchoredPosition;
        }

        /// <summary>顯示一句話（覆蓋目前這句）。</summary>
        public void Show(string text, float duration)
        {
            _text.text = text;
            _timer = Mathf.Max(duration, 1.2f) + FadeTime;
            _group.alpha = 1f;
        }

        /// <summary>立刻隱藏（進 Boss 房改用一般提示時）。</summary>
        public void Hide()
        {
            _timer = 0f;
            _group.alpha = 0f;
        }

        /// <summary>每幀更新：倒數、淡出、精靈上下飄（不受 timeScale 影響）。</summary>
        public void Tick(float dt)
        {
            if (_timer <= 0f) return;
            _timer -= dt;
            _group.alpha = Mathf.Clamp01(_timer / FadeTime);
            _elf.anchoredPosition = _elfBasePos + new Vector2(0f, Mathf.Sin(Time.unscaledTime * 3f) * 6f);
        }
    }
}
