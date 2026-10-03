using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// Action：角色逐格動畫（美術左 / 右兩組圖，不用翻轉）。
    /// 站立 = 走路第 1 格；走路循環；起跳播到最高點那格後停住，落地播最後幾格；從平台掉下來用空中那格。
    /// 憋氣半透明、受傷閃爍由 PlayerBreath / PlayerStatus 控制同一個 Renderer。
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        /// <summary>動畫狀態。</summary>
        private enum AnimState
        {
            /// <summary>站立。</summary>
            Idle,
            /// <summary>走路。</summary>
            Walk,
            /// <summary>起跳（播到空中那格停住）。</summary>
            Jump,
            /// <summary>空中（停在空中那格）。</summary>
            Air,
            /// <summary>落地（播最後幾格）。</summary>
            Land,
        }

        /// <summary>顯示用 Renderer。</summary>
        [SerializeField] private SpriteRenderer _renderer;
        /// <summary>面向右的走路畫格。</summary>
        [SerializeField] private Sprite[] _walkRight;
        /// <summary>面向左的走路畫格。</summary>
        [SerializeField] private Sprite[] _walkLeft;
        /// <summary>面向右的跳躍畫格。</summary>
        [SerializeField] private Sprite[] _jumpRight;
        /// <summary>面向左的跳躍畫格。</summary>
        [SerializeField] private Sprite[] _jumpLeft;
        /// <summary>走路每秒格數。</summary>
        [SerializeField] private float _walkFps = 10f;
        /// <summary>跳躍每秒格數。</summary>
        [SerializeField] private float _jumpFps = 12f;
        /// <summary>跳躍畫格中「空中」那格的索引（起跳播到這格停住）。</summary>
        [SerializeField] private int _airFrame = 4;
        /// <summary>水平速度超過此值視為在走路。</summary>
        [SerializeField] private float _walkSpeedThreshold = 0.2f;

        /// <summary>移動（面向、是否著地、跳躍 / 落地事件）。</summary>
        private PlayerMove _move;
        /// <summary>剛體（水平速度）。</summary>
        private Rigidbody2D _body;
        /// <summary>目前狀態。</summary>
        private AnimState _state;
        /// <summary>目前狀態已進行秒數。</summary>
        private float _timer;

        /// <summary>快取元件。</summary>
        private void Awake()
        {
            _move = GetComponent<PlayerMove>();
            _body = GetComponent<Rigidbody2D>();
        }

        /// <summary>訂閱跳躍 / 落地事件。</summary>
        private void OnEnable()
        {
            _move.Jumped += OnJumped;
            _move.Landed += OnLanded;
        }

        /// <summary>取消訂閱。</summary>
        private void OnDisable()
        {
            _move.Jumped -= OnJumped;
            _move.Landed -= OnLanded;
        }

        /// <summary>起跳：從頭播跳躍畫格。</summary>
        private void OnJumped() => SetState(AnimState.Jump);

        /// <summary>落地：播落地畫格。</summary>
        private void OnLanded(float fallSpeed) => SetState(AnimState.Land);

        /// <summary>依狀態選畫格。</summary>
        private void Update()
        {
            if (_renderer == null) return;
            _timer += Time.deltaTime;
            bool right = _move.Facing >= 0;
            var walk = right ? _walkRight : _walkLeft;
            var jump = right ? _jumpRight : _jumpLeft;
            bool moving = Mathf.Abs(_body.linearVelocity.x) > _walkSpeedThreshold;

            // 沒有起跳就離地（從平台掉下）→ 空中
            if (!_move.IsGrounded && (_state == AnimState.Idle || _state == AnimState.Walk)) SetState(AnimState.Air);

            switch (_state)
            {
                case AnimState.Idle:
                case AnimState.Walk:
                    SetState(moving ? AnimState.Walk : AnimState.Idle, keepTimer: true);
                    Show(walk, _state == AnimState.Walk ? (int)(_timer * _walkFps) % Len(walk) : 0);
                    break;

                case AnimState.Jump:
                    int f = (int)(_timer * _jumpFps);
                    if (f >= AirFrame(jump)) SetState(AnimState.Air);
                    Show(jump, Mathf.Min(f, AirFrame(jump)));
                    break;

                case AnimState.Air:
                    Show(jump, AirFrame(jump));
                    if (_move.IsGrounded) SetState(AnimState.Land);
                    break;

                case AnimState.Land:
                    // 空中那格之後的畫格依序播完，回到站立 / 走路
                    int land = AirFrame(jump) + 1 + (int)(_timer * _jumpFps);
                    if (land >= Len(jump)) SetState(moving ? AnimState.Walk : AnimState.Idle);
                    else Show(jump, land);
                    break;
            }
        }

        /// <summary>切換狀態（相同狀態且 keepTimer 時不重設計時）。</summary>
        private void SetState(AnimState state, bool keepTimer = false)
        {
            if (state == _state && keepTimer) return;
            _state = state;
            _timer = 0f;
        }

        /// <summary>空中那格的索引（夾在畫格範圍內）。</summary>
        private int AirFrame(Sprite[] frames) => Mathf.Clamp(_airFrame, 0, Len(frames) - 1);

        /// <summary>畫格數（至少 1，避免除以 0）。</summary>
        private static int Len(Sprite[] frames) => frames != null && frames.Length > 0 ? frames.Length : 1;

        /// <summary>顯示某一格（沒有畫格時不動）。</summary>
        private void Show(Sprite[] frames, int index)
        {
            if (frames == null || frames.Length == 0) return;
            var sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            if (sprite != null) _renderer.sprite = sprite;
        }
    }
}
