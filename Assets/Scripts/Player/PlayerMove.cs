using UnityEngine;

namespace DrownedDream
{
    /// <summary>Action：左右移動 + 跳躍（F-MAP-07）。速度讀 PlayerStatus.MoveSpeed；水平輸入經過 PlayerConfusion。</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerMove : MonoBehaviour
    {
        /// <summary>視為地面的 Layer。</summary>
        [SerializeField] private LayerMask _groundMask;
        /// <summary>左右翻轉的視覺節點。</summary>
        [SerializeField] private Transform _visual;

        /// <summary>物理剛體。</summary>
        private Rigidbody2D _body;
        /// <summary>身體碰撞框（地面偵測用）。</summary>
        private BoxCollider2D _collider;
        /// <summary>玩家數值。</summary>
        private PlayerStatus _status;
        /// <summary>輸入來源。</summary>
        private PlayerInputReader _input;
        /// <summary>方向錯亂。</summary>
        private PlayerConfusion _confusion;

        /// <summary>本幀水平輸入（已套用錯亂）。</summary>
        private float _moveX;
        /// <summary>土狼時間剩餘秒數。</summary>
        private float _coyoteTimer;
        /// <summary>跳躍緩衝剩餘秒數。</summary>
        private float _jumpBufferTimer;
        /// <summary>是否要在下個物理步截斷上升速度（短跳）。</summary>
        private bool _jumpCutRequested;

        /// <summary>面向：1 右、-1 左。</summary>
        public int Facing { get; private set; } = 1;
        /// <summary>是否站在地面。</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>移動參數（從 Status 取得）。</summary>
        private MovementConfig Config => _status.Movement;

        /// <summary>快取元件並套用剛體設定。</summary>
        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();
            _status = GetComponent<PlayerStatus>();
            _input = GetComponent<PlayerInputReader>();
            _confusion = GetComponent<PlayerConfusion>();
            _body.gravityScale = Config.GravityScale;
            _body.freezeRotation = true;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        /// <summary>讀取輸入、更新面向與跳躍緩衝。</summary>
        private void Update()
        {
            _moveX = _input.MoveX * _confusion.HorizontalMultiplier;

            if (Mathf.Abs(_moveX) > 0.1f)
            {
                Facing = _moveX > 0f ? 1 : -1;
                if (_visual != null)
                {
                    var scale = _visual.localScale;
                    scale.x = Mathf.Abs(scale.x) * Facing;
                    _visual.localScale = scale;
                }
            }

            if (_input.JumpPressed) _jumpBufferTimer = Config.JumpBufferTime;
            if (_input.JumpReleased) _jumpCutRequested = true;
        }

        /// <summary>套用水平加減速、跳躍與下落速度上限。</summary>
        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            IsGrounded = CheckGrounded();
            _coyoteTimer = IsGrounded ? Config.CoyoteTime : _coyoteTimer - dt;
            _jumpBufferTimer -= dt;

            var velocity = _body.linearVelocity;

            float target = _moveX * (float)_status.MoveSpeed;
            float rate = Mathf.Abs(target) > 0.01f ? Config.Acceleration : Config.Deceleration;
            if (!IsGrounded) rate *= Config.AirControl;
            velocity.x = Mathf.MoveTowards(velocity.x, target, rate * dt);

            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                velocity.y = Config.JumpVelocity;
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                _jumpCutRequested = false;
            }
            else if (_jumpCutRequested)
            {
                if (velocity.y > 0f) velocity.y *= Config.JumpCutMultiplier;
                _jumpCutRequested = false;
            }

            if (velocity.y < -Config.MaxFallSpeed) velocity.y = -Config.MaxFallSpeed;
            _body.linearVelocity = velocity;
        }

        /// <summary>傳送到指定位置並清除速度（復活用）。</summary>
        public void Teleport(Vector2 position)
        {
            _body.position = position;
            transform.position = position;
            _body.linearVelocity = Vector2.zero;
        }

        /// <summary>腳底 BoxCast 檢查是否站在地面。</summary>
        private bool CheckGrounded()
        {
            if (_body.linearVelocity.y > 0.01f) return false;
            var bounds = _collider.bounds;
            var size = new Vector2(bounds.size.x * 0.9f, 0.05f);
            var origin = new Vector2(bounds.center.x, bounds.min.y);
            var hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, Config.GroundCheckDistance, _groundMask);
            return hit.collider != null && !hit.collider.isTrigger;
        }
    }
}
