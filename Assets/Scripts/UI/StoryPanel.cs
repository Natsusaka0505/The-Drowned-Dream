using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>故事的一頁：一張插圖（可空 = 黑底）+ 依序顯示的文字。</summary>
    [Serializable]
    public class StoryPage
    {
        /// <summary>插圖（空 = 黑底、文字置中）。</summary>
        [SerializeField] private Sprite _image;
        /// <summary>這張圖底下依序顯示的文字（按鍵換下一句）。</summary>
        [TextArea(2, 4)]
        [SerializeField] private string[] _lines;

        /// <summary>插圖（唯讀）。</summary>
        public Sprite Image => _image;
        /// <summary>文字（唯讀）。</summary>
        public string[] Lines => _lines;

        /// <summary>Unity 反序列化用的無參數建構子。</summary>
        public StoryPage() { }

        /// <summary>建立預設頁（插圖由場景產生器 / 更新工具綁定）。</summary>
        public StoryPage(params string[] lines) { _lines = lines; }
    }

    /// <summary>
    /// 開場 / 結局演出（F-STORY）：黑底逐句文字；結局可附插圖（淡入、緩慢推近，文字在下方字幕條），
    /// 結局時右下角顯示 Quit 按鈕。
    /// </summary>
    public class StoryPanel : MonoBehaviour
    {
        /// <summary>每句淡入秒數。</summary>
        [SerializeField] private float _fadeInTime = 0.6f;
        /// <summary>換插圖時的淡入秒數。</summary>
        [SerializeField] private float _imageFadeTime = 1.2f;
        /// <summary>插圖每秒推近比例（0 = 不動）。</summary>
        [SerializeField] private float _imageZoomPerSecond = 0.006f;
        /// <summary>插圖最大推近倍率。</summary>
        [SerializeField] private float _imageMaxZoom = 1.08f;
        /// <summary>字幕條不透明度（有插圖時墊在文字後面）。</summary>
        [Range(0f, 1f)] [SerializeField] private float _captionBarAlpha = 0.55f;
        /// <summary>Quit 按鈕圖（空 = 文字按鈕）。</summary>
        [SerializeField] private Sprite _quitSprite;
        /// <summary>Quit 按鈕寬度。</summary>
        [SerializeField] private float _quitWidth = 220f;
        /// <summary>Quit 按鈕離右下角的距離。</summary>
        [SerializeField] private Vector2 _quitMargin = new Vector2(50f, 40f);

        /// <summary>面板根物件。</summary>
        private GameObject _panel;
        /// <summary>插圖。</summary>
        private Image _image;
        /// <summary>插圖比例（完整顯示、補黑邊）。</summary>
        private AspectRatioFitter _imageFitter;
        /// <summary>字幕條背景。</summary>
        private Image _captionBar;
        /// <summary>內文文字。</summary>
        private Text _text;
        /// <summary>內文文字的位置（有圖在下方、沒圖置中）。</summary>
        private RectTransform _textRect;
        /// <summary>「按任意鍵繼續」提示。</summary>
        private Text _hint;
        /// <summary>Quit 按鈕。</summary>
        private GameObject _quitButton;
        /// <summary>攤平後的每一句（插圖, 文字）。</summary>
        private readonly List<(Sprite image, string text)> _entries = new List<(Sprite, string)>();
        /// <summary>目前句子索引。</summary>
        private int _index;
        /// <summary>目前句子已顯示秒數。</summary>
        private float _lineTimer;
        /// <summary>目前插圖已顯示秒數（換圖才重置）。</summary>
        private float _imageTimer;
        /// <summary>播完後的回呼。</summary>
        private Action _onComplete;

        /// <summary>建立面板（預設隱藏）。</summary>
        private void Awake()
        {
            var bg = UIFactory.Stretch("StoryPanel", transform);
            UIFactory.Image(bg, Color.black);
            _panel = bg.gameObject;

            var imageRt = UIFactory.Stretch("Illustration", bg);
            _image = UIFactory.Image(imageRt, Color.white);
            _imageFitter = imageRt.gameObject.AddComponent<AspectRatioFitter>();
            _imageFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; // 完整顯示插圖，比例不同時以黑邊補齊

            var bar = UIFactory.Rect("CaptionBar", bg, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 230f));
            _captionBar = UIFactory.Image(bar, new Color(0f, 0f, 0f, _captionBarAlpha));

            _textRect = UIFactory.Rect("Text", bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 300f));
            _text = UIFactory.Text(_textRect, "", 40, TextAnchor.MiddleCenter, new Color(0.85f, 0.9f, 1f));

            var hint = UIFactory.Rect("Hint", bg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(800f, 40f));
            _hint = UIFactory.Text(hint, "按任意鍵繼續", 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.4f));

            _quitButton = MakeQuitButton(bg);
            _panel.SetActive(false);
        }

        /// <summary>開始播放一組純文字（黑底），播完呼叫 onComplete。</summary>
        public void Play(string[] lines, Action onComplete) => Play(new[] { new StoryPage(lines) }, onComplete, false);

        /// <summary>開始播放多頁故事（插圖 + 文字），播完呼叫 onComplete；showQuit = 右下角顯示 Quit 按鈕。</summary>
        public void Play(StoryPage[] pages, Action onComplete, bool showQuit)
        {
            _entries.Clear();
            foreach (var page in pages)
            {
                if (page == null || page.Lines == null) continue;
                foreach (var line in page.Lines) _entries.Add((page.Image, line));
                if (page.Lines.Length == 0 && page.Image != null) _entries.Add((page.Image, ""));
            }
            _onComplete = onComplete;
            _index = 0;
            _panel.transform.SetAsLastSibling();
            _panel.SetActive(true);
            _quitButton.SetActive(showQuit);
            if (_entries.Count == 0)
            {
                Finish();
                return;
            }
            ShowLine(true);
        }

        /// <summary>淡入目前句子與插圖、緩慢推近，按鍵切到下一句。</summary>
        private void Update()
        {
            if (!_panel.activeSelf) return;

            float dt = Time.unscaledDeltaTime;
            _lineTimer += dt;
            _imageTimer += dt;
            var c = _text.color;
            c.a = Mathf.Clamp01(_lineTimer / _fadeInTime);
            _text.color = c;
            _hint.enabled = _lineTimer >= _fadeInTime;
            if (_image.enabled)
            {
                _image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(_imageTimer / Mathf.Max(0.01f, _imageFadeTime)));
                float zoom = Mathf.Min(_imageMaxZoom, 1f + _imageTimer * _imageZoomPerSecond);
                _image.rectTransform.localScale = new Vector3(zoom, zoom, 1f);
            }

            if (_lineTimer < 0.25f || !AnyPressed()) return;

            _index++;
            if (_index < _entries.Count)
            {
                ShowLine(_entries[_index].image != _entries[_index - 1].image);
                return;
            }
            Finish();
        }

        /// <summary>顯示目前句子並重置計時；imageChanged = 換插圖（重新淡入、推近）。</summary>
        private void ShowLine(bool imageChanged)
        {
            var (sprite, text) = _entries[_index];
            _text.text = text;
            _lineTimer = 0f;
            if (!imageChanged) return;

            _imageTimer = 0f;
            bool hasImage = sprite != null;
            _image.enabled = hasImage;
            _captionBar.enabled = hasImage;
            if (hasImage)
            {
                _image.sprite = sprite;
                _imageFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            }
            // 有插圖：文字放在下方字幕條；沒有：畫面正中
            bool bottom = hasImage;
            var anchor = bottom ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
            _textRect.anchorMin = anchor;
            _textRect.anchorMax = anchor;
            _textRect.pivot = anchor;
            _textRect.anchoredPosition = bottom ? new Vector2(0f, 70f) : Vector2.zero;
            _textRect.sizeDelta = bottom ? new Vector2(1400f, 130f) : new Vector2(1400f, 300f);
        }

        /// <summary>關閉面板並呼叫回呼。</summary>
        private void Finish()
        {
            _panel.SetActive(false);
            var done = _onComplete;
            _onComplete = null;
            done?.Invoke();
        }

        /// <summary>建立右下角 Quit 按鈕（有圖用圖，沒圖用文字）。</summary>
        private GameObject MakeQuitButton(RectTransform parent)
        {
            float aspect = _quitSprite != null ? _quitSprite.rect.height / _quitSprite.rect.width : 0.38f;
            var corner = new Vector2(1f, 0f);
            var rt = UIFactory.Rect("Quit", parent, corner, corner, corner, new Vector2(-_quitMargin.x, _quitMargin.y), new Vector2(_quitWidth, _quitWidth * aspect));
            var image = rt.gameObject.AddComponent<Image>();
            if (_quitSprite != null)
            {
                image.sprite = _quitSprite;
                image.preserveAspect = true;
            }
            else
            {
                image.color = new Color(0f, 0f, 0f, 0.5f);
                UIFactory.Text(UIFactory.Stretch("Label", rt), "Quit", 36, TextAnchor.MiddleCenter, Color.white);
            }

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(GameFlow.QuitGame);
            rt.gameObject.AddComponent<TitleButtonHover>().Init(1.08f);
            rt.gameObject.SetActive(false);
            return rt.gameObject;
        }

        /// <summary>本幀是否按下任意鍵 / 滑鼠左鍵（點在按鈕上不算）/ 手把 South。</summary>
        private static bool AnyPressed()
        {
            if (QuitConfirmDialog.BlocksInput) return false; // 離開確認視窗開啟中 / 剛關閉
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return false; // ESC 保留給離開確認
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
            return false;
        }
    }
}
