using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 武器：魚叉（docs/core、F-WPN-03/04）。以拋物線飛行；
    /// 接觸 enemy → 命中後掉落；接觸場地 → 插住並成為可拾取物。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Harpoon : MonoBehaviour
    {
        /// <summary>地形 Layer（接觸場地）。</summary>
        [SerializeField] private LayerMask _groundMask;
        /// <summary>敵人 Layer（接觸 enemy）。</summary>
        [SerializeField] private LayerMask _enemyMask;

        #region Status

        /// <summary>是否接觸到場地（插住後可拾取）。</summary>
        public bool IsTouchingGround { get; private set; }
        /// <summary>拋物線下墜的重力加速度。</summary>
        public double Gravity { get; private set; }
        /// <summary>飛行速度。</summary>
        public double FlightSpeed { get; private set; }

        #endregion

        /// <summary>發射者（撿回時歸還）。</summary>
        private PlayerAttack _owner;
        /// <summary>魚槍參數。</summary>
        private HarpoonConfig _config;
        /// <summary>拾取用 Trigger（插住後才啟用）。</summary>
        private Collider2D _pickupTrigger;
        /// <summary>目前速度向量。</summary>
        private Vector2 _velocity;
        /// <summary>發射點高度（判斷掉入深淵）。</summary>
        private float _launchY;
        /// <summary>是否已命中過敵人（命中後只下墜不再傷害）。</summary>
        private bool _hitEnemy;

        /// <summary>設定拾取 Trigger 為停用。</summary>
        private void Awake()
        {
            _pickupTrigger = GetComponent<Collider2D>();
            _pickupTrigger.isTrigger = true;
            _pickupTrigger.enabled = false;
        }

        /// <summary>由 PlayerAttack 呼叫，朝 direction（1 右 / -1 左）水平發射。</summary>
        public void Launch(PlayerAttack owner, HarpoonConfig config, int direction)
        {
            _owner = owner;
            _config = config;
            FlightSpeed = config.FlightSpeed;
            Gravity = config.Gravity;
            _velocity = new Vector2(direction * (float)FlightSpeed, 0f);
            _launchY = transform.position.y;
            UpdateRotation();
        }

        /// <summary>拋物線移動並檢查接觸。</summary>
        private void Update()
        {
            if (_owner == null || IsTouchingGround) return;
            float dt = Time.deltaTime;

            _velocity.y = Mathf.Max(_velocity.y - (float)Gravity * dt, -_config.MaxFallSpeed);
            Vector2 pos = transform.position;
            Vector2 step = _velocity * dt;

            if (!CheckContact(pos, step))
            {
                transform.position = pos + step;
                UpdateRotation();
            }

            if (_launchY - transform.position.y >= _config.AbyssFallDistance) _owner.PickUp(this);
        }

        #region Action

        /// <summary>沿移動路徑檢查接觸 enemy / 場地，回傳是否已處理位置。</summary>
        private bool CheckContact(Vector2 pos, Vector2 step)
        {
            var mask = _hitEnemy ? _groundMask : (LayerMask)(_groundMask | _enemyMask);
            var hits = Physics2D.RaycastAll(pos, step.normalized, step.magnitude, mask);
            foreach (var hit in hits)
            {
                if (hit.collider.isTrigger)
                {
                    var target = hit.collider.GetComponentInParent<IDamageable>();
                    if (target == null || !target.IsAlive) continue;
                    OnTouchEnemy(target, hit.point);
                    return true;
                }
                OnTouchGround(hit.point, step.normalized);
                return true;
            }
            return false;
        }

        /// <summary>接觸到 enemy：造成一次命中，之後停止水平移動往下掉。</summary>
        private void OnTouchEnemy(IDamageable target, Vector2 point)
        {
            target.TakeHit();
            _hitEnemy = true;
            transform.position = point;
            _velocity = new Vector2(0f, Mathf.Min(_velocity.y, 0f));
        }

        /// <summary>接觸到場地：插在接觸點，開放拾取。</summary>
        private void OnTouchGround(Vector2 point, Vector2 direction)
        {
            IsTouchingGround = true;
            transform.position = point - direction * 0.05f;
            _pickupTrigger.enabled = true;
        }

        #endregion

        /// <summary>依速度方向旋轉外觀。</summary>
        private void UpdateRotation()
        {
            if (_velocity.sqrMagnitude < 0.0001f) return;
            float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            // 往左飛時上下翻轉外觀，倒鉤才不會朝下（Visual 子物件）
            var visual = transform.Find("Visual");
            if (visual != null)
            {
                var scale = visual.localScale;
                scale.y = Mathf.Abs(scale.y) * (_velocity.x < 0f ? -1f : 1f);
                visual.localScale = scale;
            }
        }

        /// <summary>玩家碰到插住的魚叉即撿回。</summary>
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsTouchingGround || _owner == null) return;
            if (other.attachedRigidbody == null || other.attachedRigidbody.gameObject != _owner.gameObject) return;
            _owner.PickUp(this);
        }
    }
}
