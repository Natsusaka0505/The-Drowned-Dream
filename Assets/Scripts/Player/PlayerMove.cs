using System;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>Action：左右移動 + 跳躍（F-MAP-07）。跳躍高度固定；按住跳躍鍵則落地後自動連跳。速度讀 PlayerStatus.MoveSpeed；輸入一律經過 PlayerConfusion（精神錯亂時 A/W/D 會被替換）。</summary>
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
        /// <summary>精神錯亂（提供錯亂後的輸入）。</summary>
        private PlayerConfusion _confusion;
        /// <summary>原始輸入（往下穿過單向平台用，不受錯亂影響）。</summary>
        private PlayerInputReader _input;
        /// <summary>正在穿過的單向平台（暫時忽略碰撞）。</summary>
        private Collider2D _dropPlatform;
        /// <summary>穿過平台剩餘秒數（時間到恢復碰撞）。</summary>
        private float _dropTimer;
        /// <summary>穿過單向平台時忽略碰撞的秒數。</summary>
        private const float DropThroughTime = 0.35f;

        /// <summary>本幀水平輸入（已套用錯亂）。</summary>
        private float _moveX;
        /// <summary>土狼時間剩餘秒數。</summary>
        private float _coyoteTimer;
        /// <summary>跳躍緩衝剩餘秒數。</summary>
        private float _jumpBufferTimer;
        /// <summary>跳躍鍵是否按住中（落地後自動連跳）。</summary>
        private bool _jumpHeld;
        /// <summary>這次在空中的最大下落速度（落地音效判斷用）。</summary>
        private float _airFallSpeed;
        /// <summary>擊退剩餘秒數（期間不套用左右輸入，讓擊退速度不被抵銷）。</summary>
        private float _knockbackTimer;

        /// <summary>起跳（音效用）。</summary>
        public event Action Jumped;
        /// <summary>落地（參數：落地前的最大下落速度；音效用）。</summary>
        public event Action<float> Landed;

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
            _confusion = GetComponent<PlayerConfusion>();
            _input = GetComponent<PlayerInputReader>();
            _body.gravityScale = Config.GravityScale;
            _body.freezeRotation = true;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        /// <summary>讀取輸入、更新面向與跳躍緩衝。</summary>
        private void Update()
        {
            _moveX = _confusion.MoveX;

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

            if (_confusion.JumpPressed) _jumpBufferTimer = Config.JumpBufferTime;
            _jumpHeld = _confusion.JumpHeld;
        }

        /// <summary>套用水平加減速、跳躍與下落速度上限。</summary>
        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            UpdateDropThrough(dt);
            bool wasGrounded = IsGrounded;
            IsGrounded = CheckGrounded();
            if (!IsGrounded) _airFallSpeed = Mathf.Max(_airFallSpeed, -_body.linearVelocity.y);
            else if (!wasGrounded)
            {
                Landed?.Invoke(_airFallSpeed);
                _airFallSpeed = 0f;
            }
            _coyoteTimer = IsGrounded ? Config.CoyoteTime : _coyoteTimer - dt;
            _jumpBufferTimer -= dt;

            var velocity = _body.linearVelocity;

            float target = _moveX * (float)_status.MoveSpeed;
            float rate = Mathf.Abs(target) > 0.01f ? Config.Acceleration : Config.Deceleration;
            if (!IsGrounded) rate *= Config.AirControl;
            if (_knockbackTimer > 0f) _knockbackTimer -= dt; // 擊退中：保留擊退的水平速度
            else velocity.x = Mathf.MoveTowards(velocity.x, target, rate * dt);

            // 固定高度：不論短按或長按都用同一個初速；按住不放則一落地就再跳
            bool pressedJump = _jumpBufferTimer > 0f && _coyoteTimer > 0f;
            bool heldJump = _jumpHeld && IsGrounded;
            if (pressedJump || heldJump)
            {
                velocity.y = Config.JumpVelocity;
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                Jumped?.Invoke();
            }

            // 下降時加重重力，讓落下比上升快（手感較俐落）
            _body.gravityScale = velocity.y < 0f ? Config.GravityScale * Config.FallGravityMultiplier : Config.GravityScale;
            if (velocity.y < -Config.MaxFallSpeed) velocity.y = -Config.MaxFallSpeed;
            _body.linearVelocity = velocity;
        }

        /// <summary>
        /// 往下穿過單向平台：站在有 PlatformEffector2D 的平台上按住往下 → 暫時忽略與該平台的碰撞，
        /// 時間到再恢復（實心的牆 / 地板不受影響）。
        /// </summary>
        private void UpdateDropThrough(float dt)
        {
            if (_dropPlatform != null)
            {
                _dropTimer -= dt;
                if (_dropTimer <= 0f)
                {
                    Physics2D.IgnoreCollision(_collider, _dropPlatform, false);
                    _dropPlatform = null;
                }
                return;
            }
            if (!IsGrounded || _input == null || !_input.DownHeld) return;

            var bounds = _collider.bounds;
            var hit = Physics2D.BoxCast(new Vector2(bounds.center.x, bounds.min.y), new Vector2(bounds.size.x * 0.9f, 0.05f), 0f,
                Vector2.down, Config.GroundCheckDistance + 0.05f, _groundMask);
            if (hit.collider == null || hit.collider.GetComponent<PlatformEffector2D>() == null) return;

            _dropPlatform = hit.collider;
            _dropTimer = DropThroughTime;
            Physics2D.IgnoreCollision(_collider, _dropPlatform, true);
            IsGrounded = false;
            _coyoteTimer = 0f;
        }

        /// <summary>擊退：直接設定速度，seconds 秒內不吃左右輸入（受擊反饋用）。</summary>
        public void Knockback(Vector2 velocity, float seconds)
        {
            _body.linearVelocity = velocity;
            _knockbackTimer = Mathf.Max(0f, seconds);
            IsGrounded = false;
            _coyoteTimer = 0f;
        }

        /// <summary>傳送到指定位置並清除速度（復活用）。</summary>
        public void Teleport(Vector2 position)
        {
            _body.position = position;
            transform.position = position;
            _body.linearVelocity = Vector2.zero;
            _knockbackTimer = 0f;
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
