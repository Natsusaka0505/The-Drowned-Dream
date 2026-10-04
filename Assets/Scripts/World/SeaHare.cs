using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 海兔（精靈本體，F-STORY-11）：平常趴在玩家頭上播「走動」畫格，跟著玩家面向翻轉、憋氣時一起變透明；
    /// 第一次進 Boss 房時由 BossArea 呼叫 LeaveHead() 跳離頭頂，改播「跳」畫格，之後飛去變身成 Boss。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SeaHare : MonoBehaviour
    {
        /// <summary>場景中的海兔（沒有時為 null，BossArea 改用舊的精靈圖）。</summary>
        public static SeaHare Instance { get; private set; }

        /// <summary>趴在頭上時的畫格（走動）。</summary>
        [SerializeField] private Sprite[] _idleFrames;
        /// <summary>跳離頭頂 / 飛行時的畫格（跳）。</summary>
        [SerializeField] private Sprite[] _jumpFrames;
        /// <summary>每格秒數。</summary>
        [SerializeField] private float _frameSeconds = 0.12f;
        /// <summary>趴在頭上時的高度（單位）。</summary>
        [SerializeField] private float _headHeight = 0.6f;
        /// <summary>相對玩家根物件的位置（腳底對頭頂）。</summary>
        [SerializeField] private Vector2 _headOffset = new Vector2(0f, 0.62f);
        /// <summary>面向右時是否翻轉圖片（原圖臉朝右 = false）。</summary>
        [SerializeField] private bool _flipWhenRight;
        /// <summary>上下輕微起伏幅度。</summary>
        [SerializeField] private float _bobHeight = 0.03f;
        /// <summary>上下起伏速度（弧度 / 秒）。</summary>
        [SerializeField] private float _bobSpeed = 4f;
        /// <summary>跟隨透明度的玩家圖（憋氣變透明、受擊閃爍時一起變；空 = 不跟）。</summary>
        [SerializeField] private SpriteRenderer _mirrorAlpha;

        /// <summary>顯示用 Renderer。</summary>
        private SpriteRenderer _renderer;
        /// <summary>目前播放的畫格組。</summary>
        private Sprite[] _frames;
        /// <summary>目前畫格索引。</summary>
        private int _index;
        /// <summary>目前畫格已顯示秒數。</summary>
        private float _timer;
        /// <summary>是否還趴在頭上。</summary>
        private bool _onHead = true;
        /// <summary>玩家移動元件（讀面向）。</summary>
        private PlayerMove _move;

        /// <summary>原始圖高度（單位，縮放用）。</summary>
        public float SpriteHeight => _renderer != null && _renderer.sprite != null ? _renderer.sprite.bounds.size.y : 1f;

        /// <summary>註冊單例、顯示第一格並套用頭上大小。</summary>
        private void Awake()
        {
            Instance = this;
            _renderer = GetComponent<SpriteRenderer>();
            _move = GetComponentInParent<PlayerMove>();
            SetFrames(_idleFrames);
            transform.localPosition = _headOffset;
            if (_renderer.sprite != null) transform.localScale = Vector3.one * (_headHeight / SpriteHeight);
        }

        /// <summary>清除單例。</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>播放畫格；在頭上時跟隨面向、透明度與起伏。</summary>
        private void Update()
        {
            StepFrames();
            if (!_onHead) return;
            transform.localPosition = _headOffset + Vector2.up * (Mathf.Sin(Time.time * _bobSpeed) * _bobHeight);
            if (_move != null) _renderer.flipX = (_move.Facing >= 0) == _flipWhenRight;
            if (_mirrorAlpha != null)
            {
                var c = _renderer.color;
                c.a = _mirrorAlpha.color.a;
                _renderer.color = c;
            }
        }

        /// <summary>跳離玩家頭頂：脫離玩家、改播跳躍畫格、畫在最前面，回傳 Transform 交給演出控制。</summary>
        public Transform LeaveHead(int sortingOrder)
        {
            _onHead = false;
            transform.SetParent(null, true);
            _renderer.color = Color.white;
            _renderer.sortingOrder = sortingOrder;
            SetFrames(_jumpFrames);
            return transform;
        }

        /// <summary>切換畫格組（空的保留目前圖）。</summary>
        private void SetFrames(Sprite[] frames)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) return;
            _frames = frames;
            _index = 0;
            _timer = 0f;
            _renderer.sprite = _frames[0];
        }

        /// <summary>依秒數切換畫格（循環）。</summary>
        private void StepFrames()
        {
            if (_frames == null || _frames.Length <= 1) return;
            _timer += Time.deltaTime;
            float d = Mathf.Max(0.01f, _frameSeconds);
            while (_timer >= d)
            {
                _timer -= d;
                _index = (_index + 1) % _frames.Length;
            }
            _renderer.sprite = _frames[_index];
        }
    }
}
