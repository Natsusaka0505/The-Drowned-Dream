using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>開場 / 結局文字演出（F-STORY）。[待確認] 呈現形式，原型為黑底逐句文字。</summary>
    public class StoryPanel : MonoBehaviour
    {
        /// <summary>每句淡入秒數。</summary>
        [SerializeField] private float _fadeInTime = 0.6f;

        /// <summary>面板根物件。</summary>
        private GameObject _panel;
        /// <summary>內文文字。</summary>
        private Text _text;
        /// <summary>「按任意鍵繼續」提示。</summary>
        private Text _hint;
        /// <summary>目前播放的文字列。</summary>
        private string[] _lines;
        /// <summary>目前句子索引。</summary>
        private int _index;
        /// <summary>目前句子已顯示秒數。</summary>
        private float _lineTimer;
        /// <summary>播完後的回呼。</summary>
        private Action _onComplete;

        /// <summary>建立面板（預設隱藏）。</summary>
        private void Awake()
        {
            var bg = UIFactory.Stretch("StoryPanel", transform);
            UIFactory.Image(bg, Color.black);
            _panel = bg.gameObject;

            var body = UIFactory.Rect("Text", bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 300f));
            _text = UIFactory.Text(body, "", 40, TextAnchor.MiddleCenter, new Color(0.85f, 0.9f, 1f));

            var hint = UIFactory.Rect("Hint", bg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(800f, 40f));
            _hint = UIFactory.Text(hint, "按任意鍵繼續", 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.4f));

            _panel.SetActive(false);
        }

        /// <summary>開始播放一組文字，播完呼叫 onComplete。</summary>
        public void Play(string[] lines, Action onComplete)
        {
            _lines = lines;
            _onComplete = onComplete;
            _index = 0;
            _panel.transform.SetAsLastSibling();
            _panel.SetActive(true);
            ShowLine();
        }

        /// <summary>淡入目前句子，按鍵切到下一句。</summary>
        private void Update()
        {
            if (!_panel.activeSelf) return;

            _lineTimer += Time.unscaledDeltaTime;
            var c = _text.color;
            c.a = Mathf.Clamp01(_lineTimer / _fadeInTime);
            _text.color = c;
            _hint.enabled = _lineTimer >= _fadeInTime;

            if (_lineTimer < 0.25f || !AnyPressed()) return;

            _index++;
            if (_index < _lines.Length)
            {
                ShowLine();
                return;
            }

            _panel.SetActive(false);
            var done = _onComplete;
            _onComplete = null;
            done?.Invoke();
        }

        /// <summary>顯示目前句子並重置計時。</summary>
        private void ShowLine()
        {
            _text.text = _lines.Length > 0 ? _lines[_index] : "";
            _lineTimer = 0f;
        }

        /// <summary>本幀是否按下任意鍵 / 滑鼠左鍵 / 手把 South。</summary>
        private static bool AnyPressed()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
            return false;
        }
    }
}
