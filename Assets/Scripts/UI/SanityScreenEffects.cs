using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>
    /// 低 SAN 畫面效果（F-SAN-10/12）：依分段加深色調、改變環境光、鏡頭抖動；方向錯亂前閃紅預告。
    /// 原型效果，美術可替換為 Volume / Shader。
    /// </summary>
    public class SanityScreenEffects : MonoBehaviour
    {
        /// <summary>全域 2D 光（依分段變色）。</summary>
        [SerializeField] private Light2D _globalLight;
        /// <summary>攝影機（低 SAN 抖動）。</summary>
        [SerializeField] private GameCamera _camera;

        [Header("各分段（0~3）畫面色調")]
        /// <summary>各分段全螢幕色調。</summary>
        [SerializeField] private Color[] _stageTints =
        {
            new Color(0f, 0f, 0f, 0f),
            new Color(0.25f, 0.05f, 0.35f, 0.12f),
            new Color(0.3f, 0.02f, 0.25f, 0.25f),
            new Color(0.35f, 0f, 0.1f, 0.38f),
        };
        /// <summary>各分段環境光顏色。</summary>
        [SerializeField] private Color[] _stageLightColors =
        {
            new Color(0.55f, 0.75f, 1f),
            new Color(0.6f, 0.6f, 0.95f),
            new Color(0.7f, 0.5f, 0.8f),
            new Color(0.8f, 0.35f, 0.5f),
        };
        /// <summary>各分段鏡頭抖動幅度。</summary>
        [SerializeField] private float[] _stageShake = { 0f, 0f, 0.02f, 0.06f };
        /// <summary>方向錯亂預告閃爍顏色。</summary>
        [SerializeField] private Color _warningColor = new Color(1f, 0f, 0f, 0.35f);

        /// <summary>全螢幕色調圖層。</summary>
        private Image _tint;
        /// <summary>錯亂預告圖層。</summary>
        private Image _warning;
        /// <summary>玩家快取。</summary>
        private Player _player;
        /// <summary>目前分段。</summary>
        private int _stage;
        /// <summary>預告閃爍剩餘秒數。</summary>
        private float _warningTimer;

        /// <summary>建立兩個全螢幕圖層（放在 UI 最底層）。</summary>
        private void Awake()
        {
            var tintRt = UIFactory.Stretch("SanityTint", transform);
            tintRt.SetAsFirstSibling();
            _tint = UIFactory.Image(tintRt, Color.clear);

            var warnRt = UIFactory.Stretch("ConfusionWarning", transform);
            warnRt.SetSiblingIndex(1);
            _warning = UIFactory.Image(warnRt, Color.clear);
        }

        /// <summary>訂閱 SAN 分段與錯亂事件。</summary>
        private void Start()
        {
            _player = Player.Instance;
            if (_player == null) return;
            _player.Status.SanityStageChanged += s => _stage = Mathf.Clamp(s, 0, _stageTints.Length - 1);
            _player.Confusion.WarningStarted += OnWarning;
            _player.Confusion.ConfusionStarted += () => GameEvents.ShowMessage("方向錯亂！", 1.5f);
        }

        /// <summary>開始預告閃爍。</summary>
        private void OnWarning()
        {
            _warningTimer = _player.Status.SanityConfig.ConfusionWarningTime;
            GameEvents.ShowMessage("耳邊傳來低語……", 1f);
        }

        /// <summary>平滑更新色調、光色、抖動與預告閃爍。</summary>
        private void Update()
        {
            if (_player == null) return;

            var target = _stageTints[_stage];
            if (_stage >= 3) target.a *= 0.8f + 0.2f * Mathf.Sin(Time.time * 4f);
            _tint.color = Color.Lerp(_tint.color, target, Time.deltaTime * 2f);

            if (_globalLight != null && _stage < _stageLightColors.Length)
            {
                _globalLight.color = Color.Lerp(_globalLight.color, _stageLightColors[_stage], Time.deltaTime * 2f);
            }

            if (_camera != null)
            {
                float shake = _stage < _stageShake.Length ? _stageShake[_stage] : 0f;
                if (_player.Confusion.IsConfused) shake += 0.03f;
                _camera.ShakeAmount = shake;
            }

            if (_warningTimer > 0f)
            {
                _warningTimer -= Time.deltaTime;
                float blink = Mathf.PingPong(Time.time * 8f, 1f);
                _warning.color = new Color(_warningColor.r, _warningColor.g, _warningColor.b, _warningColor.a * blink);
            }
            else if (_player.Confusion.IsConfused)
            {
                _warning.color = new Color(_warningColor.r, _warningColor.g, _warningColor.b, _warningColor.a * 0.3f);
            }
            else
            {
                _warning.color = Color.clear;
            }
        }
    }
}
