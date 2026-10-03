using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// camera（docs/core）：平常跟隨玩家（限制在目前 Room 內）；第一次進 Boss 房時切到 Boss 特寫，再切回玩家。
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
        [SerializeField] private float _followSize = 6f;
        /// <summary>Boss 特寫時的畫面大小。</summary>
        [SerializeField] private float _closeUpSize = 4.5f;
        /// <summary>平滑跟隨時間。</summary>
        [SerializeField] private float _smoothTime = 0.15f;
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
        /// <summary>是否為 Boss 特寫模式（不限制在 Room 內、不加偏移）。</summary>
        private bool _closeUp;

        /// <summary>目前跟隨的目標。</summary>
        public GameObject Target => _target;
        /// <summary>額外抖動幅度（低 SAN 效果用）。</summary>
        public float ShakeAmount { get; set; }

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
            _targetSize = _followSize;
        }

        /// <summary>切換到 Boss 房間（特寫指定目標）。</summary>
        public void SwitchToBossRoom(GameObject boss)
        {
            _target = boss;
            _closeUp = true;
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
            var pos = Vector3.SmoothDamp(transform.position, desired, ref _velocity, _smoothTime, Mathf.Infinity, dt);
            _shakeOffset = ShakeAmount > 0f ? (Vector3)(Random.insideUnitCircle * ShakeAmount) : Vector3.zero;
            transform.position = pos + _shakeOffset;
        }

        /// <summary>理想位置（目標 + 偏移；特寫時不加偏移）。</summary>
        private Vector3 Desired()
        {
            var p = _target.transform.position + (_closeUp ? Vector3.zero : (Vector3)_offset);
            p.z = transform.position.z;
            return p;
        }

        /// <summary>限制在目前區塊內；區塊比畫面小時置中。</summary>
        private Vector3 Clamp(Vector3 pos)
        {
            var room = Room.Current;
            if (room == null) return pos;

            var b = room.Bounds;
            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;

            pos.x = b.size.x <= halfW * 2f ? b.center.x : Mathf.Clamp(pos.x, b.min.x + halfW, b.max.x - halfW);
            pos.y = b.size.y <= halfH * 2f ? b.center.y : Mathf.Clamp(pos.y, b.min.y + halfH, b.max.y - halfH);
            return pos;
        }
    }
}
