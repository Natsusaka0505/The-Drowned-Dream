using UnityEngine;

namespace DrownedDream
{
    /// <summary>逐格播放 Sprite（例如由 GIF 拆出的 Boss 動畫），每格可設定不同秒數，循環播放。</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        /// <summary>依序播放的畫格。</summary>
        [SerializeField] private Sprite[] _frames;
        /// <summary>每格秒數（長度不足時用 _defaultDuration）。</summary>
        [SerializeField] private float[] _durations;
        /// <summary>沒有指定秒數的畫格使用的秒數。</summary>
        [SerializeField] private float _defaultDuration = 0.1f;
        /// <summary>播放速度倍率。</summary>
        [SerializeField] private float _speed = 1f;

        /// <summary>顯示用 Renderer。</summary>
        private SpriteRenderer _renderer;
        /// <summary>目前畫格索引。</summary>
        private int _index;
        /// <summary>目前畫格已顯示秒數。</summary>
        private float _timer;

        /// <summary>快取 Renderer 並顯示第一格。</summary>
        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_frames != null && _frames.Length > 0) _renderer.sprite = _frames[0];
        }

        /// <summary>依每格秒數切換畫格（遊戲暫停時一起停）。</summary>
        private void Update()
        {
            if (_frames == null || _frames.Length <= 1) return;
            _timer += Time.deltaTime * _speed;
            float duration = FrameDuration(_index);
            while (_timer >= duration)
            {
                _timer -= duration;
                _index = (_index + 1) % _frames.Length;
                duration = FrameDuration(_index);
            }
            _renderer.sprite = _frames[_index];
        }

        /// <summary>取得某格的秒數（至少 0.01 秒，避免無限迴圈）。</summary>
        private float FrameDuration(int i)
        {
            float d = _durations != null && i < _durations.Length ? _durations[i] : _defaultDuration;
            return Mathf.Max(0.01f, d);
        }
    }
}
