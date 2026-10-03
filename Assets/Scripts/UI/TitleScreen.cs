using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>遊戲封面：封面底圖 + Start / Quit 按鈕（F-STORY）。按 Start 進入開場，Quit 結束遊戲。</summary>
    public class TitleScreen : MonoBehaviour
    {
        /// <summary>封面底圖（16:9）。</summary>
        [SerializeField] private Sprite _cover;
        /// <summary>Start 按鈕圖。</summary>
        [SerializeField] private Sprite _startSprite;
        /// <summary>Quit 按鈕圖。</summary>
        [SerializeField] private Sprite _quitSprite;

        [Header("按鈕版面（以 1920×1080 為準）")]
        /// <summary>按鈕欄中心的水平位置（0 = 封面左、1 = 封面右）。</summary>
        [Range(0f, 1f)]
        [SerializeField] private float _buttonColumnX = 0.46f;
        /// <summary>按鈕欄中心的高度（0 = 封面底、1 = 封面頂）。</summary>
        [Range(0f, 1f)]
        [SerializeField] private float _buttonColumnY = 0.47f;
        /// <summary>單顆按鈕寬度（高度依圖片比例）。</summary>
        [SerializeField] private float _buttonWidth = 280f;
        /// <summary>兩顆按鈕的上下間距。</summary>
        [SerializeField] private float _buttonSpacing = 45f;
        /// <summary>滑鼠移上 / 選取時的放大倍率。</summary>
        [SerializeField] private float _hoverScale = 1.08f;

        /// <summary>封面根物件。</summary>
        private GameObject _panel;
        /// <summary>Start 按鈕（開啟時預設選取，鍵盤 / 手把可直接確認）。</summary>
        private Button _startButton;
        /// <summary>按下 Start 後的回呼。</summary>
        private Action _onStart;

        /// <summary>建立封面（預設隱藏）。</summary>
        private void Awake()
        {
            var bg = UIFactory.Stretch("TitleScreen", transform);
            var bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.color = Color.black;
            _panel = bg.gameObject;

            var cover = UIFactory.Stretch("Cover", bg);
            var coverImage = UIFactory.Image(cover, Color.white);
            coverImage.sprite = _cover;
            if (_cover != null)
            {
                var fitter = cover.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; // 完整顯示封面，比例不同時以黑邊補齊
                fitter.aspectRatio = _cover.rect.width / _cover.rect.height;
            }

            // 上下排列：Start 在上、Quit 在下；按鈕掛在封面底圖下，非 16:9 螢幕時也跟著底圖對齊
            float height = _buttonWidth * ButtonAspect(_startSprite);
            float offset = (height + _buttonSpacing) / 2f;
            _startButton = MakeButton("Start", cover, _startSprite, offset, StartGame);
            MakeButton("Quit", cover, _quitSprite, -offset, Quit);

            _panel.SetActive(false);
        }

        /// <summary>顯示封面，按下 Start 後呼叫 onStart。</summary>
        public void Show(Action onStart)
        {
            _onStart = onStart;
            _panel.transform.SetAsLastSibling();
            _panel.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_startButton.gameObject);
        }

        /// <summary>按鈕圖的高寬比（沒有圖時用預設值）。</summary>
        private static float ButtonAspect(Sprite sprite) => sprite != null ? sprite.rect.height / sprite.rect.width : 0.38f;

        /// <summary>建立圖片按鈕（y = 相對按鈕欄中心的垂直位移）。</summary>
        private Button MakeButton(string name, RectTransform parent, Sprite sprite, float y, Action onClick)
        {
            var anchor = new Vector2(_buttonColumnX, _buttonColumnY);
            var rt = UIFactory.Rect(name, parent, anchor, anchor, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(_buttonWidth, _buttonWidth * ButtonAspect(sprite)));
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick());

            var hover = rt.gameObject.AddComponent<TitleButtonHover>();
            hover.Init(_hoverScale);
            return button;
        }

        /// <summary>隱藏封面並進入開場。</summary>
        private void StartGame()
        {
            if (!_panel.activeSelf) return;
            _panel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            var done = _onStart;
            _onStart = null;
            done?.Invoke();
        }

        /// <summary>結束遊戲（Editor 中停止 Play）。</summary>
        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
