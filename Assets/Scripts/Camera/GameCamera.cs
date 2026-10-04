using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// camera（docs/core）：平常跟隨玩家（限制在整張地圖內，不露出地圖外）；第一次進 Boss 房時平移到 Boss 特寫，再平移回玩家。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GameCamera : MonoBehaviour
    {
        /// <summary>場景中的主攝影機。</summary>
        public static GameCamera Instance { get; private set; }

        #region Status

        /// <summary>目前跟隨的目標（GameObject）。</summary>
        [SerializeField] private GameObject _target;

        #endregion

        /// <summary>跟隨玩家時的畫面大小（orthographicSize）。</summary>
        [SerializeField] private float _followSize = 7f;
        /// <summary>Boss 特寫時的畫面大小。</summary>
        [SerializeField] private float _closeUpSize = 10f; // Boss 放大 2 倍 + 站上浮岩，要拉遠才拍得完整
        /// <summary>平滑跟隨時間。</summary>
        [SerializeField] private float _smoothTime = 0.15f;
        /// <summary>切換目標（玩家 ↔ Boss）時的平移平滑時間（越大越慢）。</summary>
        [SerializeField] private float _switchSmoothTime = 0.5f;
        /// <summary>切換平移距離小於此值時視為抵達，恢復一般跟隨速度。</summary>
        [SerializeField] private float _switchArriveDistance = 0.3f;
        /// <summary>畫面大小切換速度。</summary>
        [SerializeField] private float _zoomSpeed = 3f;
        /// <summary>跟隨玩家時的偏移。</summary>
        [SerializeField] private Vector2 _offset = new Vector2(0f, 1f);

        /// <summary>攝影機元件。</summary>
        private Camera _camera;
        /// <summary>玩家（切回用）。</summary>
        private GameObject _player;
        /// <summary>SmoothDamp 用速度。</summary>
        private Vector3 _velocity;
        /// <summary>上一幀加上的抖動量（下一幀先扣回）。</summary>
        private Vector3 _shakeOffset;
        /// <summary>目標畫面大小。</summary>
        private float _targetSize;
        /// <summary>是否為 Boss 特寫模式（不限制在地圖內、不加偏移）。</summary>
        private bool _closeUp;
        /// <summary>是否正在切換目標的平移中（用較慢的平滑時間）。</summary>
        private bool _switching;

        /// <summary>目前跟隨的目標。</summary>
        public GameObject Target => _target;
        /// <summary>額外抖動幅度（低 SAN 效果用）。</summary>
        public float ShakeAmount { get; set; }

        /// <summary>短暫震動的幅度（Boss 咆哮等，會隨時間衰減）。</summary>
        private float _impulseAmount;
        /// <summary>短暫震動剩餘秒數。</summary>
        private float _impulseTimer;
        /// <summary>短暫震動總秒數。</summary>
        private float _impulseTime;

        /// <summary>觸發一次短暫畫面震動（幅度隨時間衰減到 0）。</summary>
        public void Shake(float amount, float duration)
        {
            _impulseAmount = amount;
            _impulseTime = Mathf.Max(0.01f, duration);
            _impulseTimer = _impulseTime;
        }

        /// <summary>註冊單例並快取攝影機。</summary>
        private void Awake()
        {
            Instance = this;
            _camera = GetComponent<Camera>();
            _targetSize = _followSize;
            _camera.orthographicSize = _followSize;
        }

        /// <summary>清除單例。</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>預設跟隨玩家並直接對準。</summary>
        private void Start()
        {
            if (Player.Instance != null) _player = Player.Instance.gameObject;
            if (_target == null) _target = _player;
            if (_target != null) transform.position = Clamp(Desired());
        }

        #region Action

        /// <summary>切換到玩家身上。</summary>
        public void SwitchToPlayer()
        {
            _target = _player;
            _closeUp = false;
            _switching = true;
            _targetSize = _followSize;
        }

        /// <summary>切換到 Boss 房間（特寫指定目標）。</summary>
        public void SwitchToBossRoom(GameObject boss)
        {
            _target = boss;
            _closeUp = true;
            _switching = true;
            _targetSize = _closeUpSize;
        }

        #endregion

        /// <summary>平滑跟隨、縮放並套用抖動。</summary>
        private void LateUpdate()
        {
            if (_target == null) return;
            float dt = Time.unscaledDeltaTime;
            _camera.orthographicSize = Mathf.MoveTowards(_camera.orthographicSize, _targetSize, _zoomSpeed * dt);

            transform.position -= _shakeOffset;
            var desired = _closeUp ? Desired() : Clamp(Desired());
            if (_switching && Vector2.Distance(transform.position, desired) < _switchArriveDistance) _switching = false;
            float smooth = _switching ? _switchSmoothTime : _smoothTime;
            var pos = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smooth, Mathf.Infinity, dt);
            float impulse = 0f;
            if (_impulseTimer > 0f)
            {
                _impulseTimer -= dt;
                impulse = _impulseAmount * Mathf.Clamp01(_impulseTimer / _impulseTime);
            }
            float shake = ShakeAmount + impulse;
            _shakeOffset = shake > 0f ? (Vector3)(Random.insideUnitCircle * shake) : Vector3.zero;
            transform.position = pos + _shakeOffset;
        }

        /// <summary>理想位置（目標 + 偏移；特寫時不加偏移）。</summary>
        private Vector3 Desired()
        {
            var p = _target.transform.position + (_closeUp ? Vector3.zero : (Vector3)_offset);
            p.z = transform.position.z;
            return p;
        }

        /// <summary>限制在整張地圖內（不露出地圖外）；地圖比畫面小時置中。</summary>
        private Vector3 Clamp(Vector3 pos)
        {
            if (!Room.TryGetWorldBounds(out var b)) return pos;

            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;

            pos.x = b.size.x <= halfW * 2f ? b.center.x : Mathf.Clamp(pos.x, b.min.x + halfW, b.max.x - halfW);
            pos.y = b.size.y <= halfH * 2f ? b.center.y : Mathf.Clamp(pos.y, b.min.y + halfH, b.max.y - halfH);
            return pos;
        }
    }
}
