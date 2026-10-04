using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 海兔（精靈本體，F-STORY-11）：平常趴在玩家頭上播「走動」畫格，跟著玩家面向翻轉、憋氣時一起變透明；
    /// 玩家從高處落下時被水流托起、懸空飄在頭頂上方（播「跳」畫格），落地後壓扁一下再趴回去；玩家受擊時嚇得跳一下。
    /// 第一次進 Boss 房時由 BossArea 呼叫 LeaveHead() 從頭頂飛起，之後飛去變身成 Boss。
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

        [Header("高處落下懸空")]
        /// <summary>落下距離超過此值（單位，從空中最高點算）才懸空；一般跳躍約 2.6，不會觸發。</summary>
        [SerializeField] private float _floatFallDistance = 3.5f;
        /// <summary>懸空時離頭頂的最大高度（單位）。</summary>
        [SerializeField] private float _floatLift = 0.7f;
        /// <summary>懸空時左右飄動幅度（單位）。</summary>
        [SerializeField] private float _floatSway = 0.12f;
        /// <summary>懸空時左右傾斜角度。</summary>
        [SerializeField] private float _floatTilt = 12f;
        /// <summary>飄起來的速度（每秒，0~1 的懸空程度）。</summary>
        [SerializeField] private float _floatRiseRate = 3f;
        /// <summary>落回頭上的速度（每秒，0~1 的懸空程度）。</summary>
        [SerializeField] private float _floatSettleRate = 5f;
        /// <summary>懸空落地後壓扁的秒數。</summary>
        [SerializeField] private float _landSquashSeconds = 0.25f;
        /// <summary>落地壓扁幅度（0.3 = 高度少 30%、寬度多 30%）。</summary>
        [SerializeField] private float _landSquash = 0.3f;

        [Header("受擊受驚")]
        /// <summary>玩家受擊時海兔往上彈的高度（單位）。</summary>
        [SerializeField] private float _startleHeight = 0.35f;
        /// <summary>受驚彈跳秒數。</summary>
        [SerializeField] private float _startleSeconds = 0.3f;

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
        /// <summary>玩家移動元件（讀面向、是否著地）。</summary>
        private PlayerMove _move;
        /// <summary>玩家數值（訂閱受擊）。</summary>
        private PlayerStatus _status;
        /// <summary>趴在頭上時的縮放（壓扁以此為基準）。</summary>
        private Vector3 _baseScale = Vector3.one;
        /// <summary>這次離地後的最高點 Y（算落下距離）。</summary>
        private float _airPeakY;
        /// <summary>上一幀玩家 Y（偵測傳送）。</summary>
        private float _lastPlayerY;
        /// <summary>懸空程度（0 = 趴著、1 = 飄到最高）。</summary>
        private float _float;
        /// <summary>這次空中是否進入過懸空（落地時決定要不要壓扁）。</summary>
        private bool _floatedThisAir;
        /// <summary>落地壓扁剩餘秒數。</summary>
        private float _squashTimer;
        /// <summary>受驚彈跳剩餘秒數。</summary>
        private float _startleTimer;

        /// <summary>原始圖高度（單位，縮放用）。</summary>
        public float SpriteHeight => _renderer != null && _renderer.sprite != null ? _renderer.sprite.bounds.size.y : 1f;

        /// <summary>註冊單例、顯示第一格並套用頭上大小。</summary>
        private void Awake()
        {
            Instance = this;
            _renderer = GetComponent<SpriteRenderer>();
            _move = GetComponentInParent<PlayerMove>();
            _status = GetComponentInParent<PlayerStatus>();
            SetFrames(_idleFrames);
            transform.localPosition = _headOffset;
            if (_renderer.sprite != null) transform.localScale = Vector3.one * (_headHeight / SpriteHeight);
            _baseScale = transform.localScale;
            if (_move != null) _airPeakY = _lastPlayerY = _move.transform.position.y;
        }

        /// <summary>訂閱玩家受擊。</summary>
        private void OnEnable()
        {
            if (_status != null) _status.Damaged += OnPlayerDamaged;
        }

        /// <summary>取消訂閱。</summary>
        private void OnDisable()
        {
            if (_status != null) _status.Damaged -= OnPlayerDamaged;
        }

        /// <summary>清除單例。</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>玩家受擊：嚇得往上彈一下。</summary>
        private void OnPlayerDamaged()
        {
            if (_onHead) _startleTimer = _startleSeconds;
        }

        /// <summary>播放畫格；在頭上時跟隨面向、透明度與起伏。</summary>
        private void Update()
        {
            StepFrames();
            if (!_onHead) return;
            UpdateFloat(Time.deltaTime);

            // 位置：起伏 + 懸空高度與左右飄 + 受驚彈跳
            float bob = Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
            float sway = Mathf.Sin(Time.time * 5f) * _floatSway * _float;
            float startle = 0f;
            if (_startleTimer > 0f)
            {
                _startleTimer -= Time.deltaTime;
                startle = Mathf.Sin(Mathf.Clamp01(1f - _startleTimer / Mathf.Max(0.01f, _startleSeconds)) * Mathf.PI) * _startleHeight;
            }
            transform.localPosition = _headOffset + new Vector2(sway, bob + _float * _floatLift + startle);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 4f) * _floatTilt * _float);

            // 縮放：落地壓扁（寬變大、高變小）後彈回
            float squash = 0f;
            if (_squashTimer > 0f)
            {
                _squashTimer -= Time.deltaTime;
                squash = Mathf.Sin(Mathf.Clamp01(_squashTimer / Mathf.Max(0.01f, _landSquashSeconds)) * Mathf.PI) * _landSquash;
            }
            transform.localScale = new Vector3(_baseScale.x * (1f + squash), _baseScale.y * (1f - squash), _baseScale.z);

            if (_move != null) _renderer.flipX = (_move.Facing >= 0) == _flipWhenRight;
            if (_mirrorAlpha != null)
            {
                var c = _renderer.color;
                c.a = _mirrorAlpha.color.a;
                _renderer.color = c;
            }
        }

        /// <summary>
        /// 高處落下懸空：記錄空中最高點，落下距離超過門檻就慢慢飄起（播「跳」畫格），
        /// 著地後慢慢落回頭上（懸空過才壓扁一下、換回「走動」畫格）。
        /// </summary>
        private void UpdateFloat(float dt)
        {
            if (_move == null) return;
            float y = _move.transform.position.y;
            if (Mathf.Abs(y - _lastPlayerY) > 3f) _airPeakY = y; // 傳送（復活）不算落下
            _lastPlayerY = y;

            bool falling = false;
            if (_move.IsGrounded) _airPeakY = y;
            else
            {
                _airPeakY = Mathf.Max(_airPeakY, y);
                falling = _airPeakY - y > _floatFallDistance;
            }

            if (falling && !_floatedThisAir)
            {
                _floatedThisAir = true;
                SetFrames(_jumpFrames);
            }
            _float = Mathf.MoveTowards(_float, falling ? 1f : 0f, (falling ? _floatRiseRate : _floatSettleRate) * dt);

            if (_floatedThisAir && _move.IsGrounded && _float <= 0f)
            {
                _floatedThisAir = false;
                _squashTimer = _landSquashSeconds;
                SetFrames(_idleFrames);
            }
        }

        /// <summary>離開玩家頭頂：脫離玩家、改播跳躍畫格、畫在最前面，回傳 Transform 交給演出控制。</summary>
        public Transform LeaveHead(int sortingOrder)
        {
            _onHead = false;
            transform.localRotation = Quaternion.identity;
            transform.localScale = _baseScale;
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
