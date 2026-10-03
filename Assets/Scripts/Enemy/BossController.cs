using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// Boss（F-BOSS）。由 BossRoom 啟動 / 停止；啟動時朝玩家發射彈幕。
    /// [待確認] 攻擊模式、魚槍能否傷害 Boss（預設不行）。
    /// </summary>
    [RequireComponent(typeof(FearSource))]
    public class BossController : MonoBehaviour, IDamageable
    {
        /// <summary>外觀 Renderer。</summary>
        [SerializeField] private SpriteRenderer _renderer;
        /// <summary>彈幕外觀。</summary>
        [SerializeField] private Sprite _projectileSprite;

        [Header("攻擊（可調）")]
        /// <summary>每波彈幕間隔秒數。</summary>
        [SerializeField] private float _fireInterval = 1.6f;
        /// <summary>每波彈幕數量。</summary>
        [SerializeField] private int _projectilesPerVolley = 3;
        /// <summary>彈幕擴散角度。</summary>
        [SerializeField] private float _spreadAngle = 20f;
        /// <summary>彈幕速度。</summary>
        [SerializeField] private float _projectileSpeed = 5f;
        /// <summary>彈幕傷害（扣 HP）。</summary>
        [SerializeField] private float _projectileDamage = 15f;
        /// <summary>彈幕存活秒數。</summary>
        [SerializeField] private float _projectileLifetime = 6f;
        /// <summary>會擋下彈幕的 Layer。</summary>
        [SerializeField] private LayerMask _projectileBlockMask;
        /// <summary>憋氣隱形時 Boss 是否也看不到玩家。</summary>
        [SerializeField] private bool _respectStealth = true;

        [Header("[待確認] 魚槍傷害")]
        /// <summary>魚槍能否傷害 Boss。</summary>
        [SerializeField] private bool _canBeDamaged;
        /// <summary>被攻擊幾次死亡（可受傷時才有意義）。</summary>
        [SerializeField] private int _maxHits = 30;

        /// <summary>距離下一波的秒數。</summary>
        private float _fireTimer;
        /// <summary>被攻擊次數。</summary>
        private int _hitCount;
        /// <summary>原始顏色。</summary>
        private Color _baseColor;

        /// <summary>是否啟動中（攻擊玩家）。</summary>
        public bool IsActive { get; private set; }
        /// <summary>是否已被封印。</summary>
        public bool IsSealed { get; private set; }
        /// <summary>是否還活著（封印後視為死亡）。</summary>
        public bool IsAlive => !IsSealed && _hitCount < _maxHits;

        /// <summary>記錄原始顏色。</summary>
        private void Awake()
        {
            if (_renderer != null) _baseColor = _renderer.color;
        }

        /// <summary>啟動 Boss（玩家進房且道具不齊）。</summary>
        public void Activate()
        {
            if (IsSealed || IsActive) return;
            IsActive = true;
            _fireTimer = _fireInterval;
        }

        /// <summary>停止 Boss（玩家離房）。</summary>
        public void Deactivate() => IsActive = false;

        /// <summary>封印 Boss。</summary>
        public void Seal()
        {
            IsSealed = true;
            IsActive = false;
            GetComponent<FearSource>().enabled = false;
            if (_renderer != null) _renderer.color = new Color(0.3f, 0.3f, 0.35f, 0.6f);
        }

        /// <summary>啟動時閃爍並定時發射彈幕。</summary>
        private void Update()
        {
            if (_renderer != null && !IsSealed)
            {
                float pulse = IsActive ? 0.5f + 0.5f * Mathf.Sin(Time.time * 6f) : 0f;
                _renderer.color = Color.Lerp(_baseColor, Color.red, pulse * 0.5f);
            }

            if (!IsActive || !GameFlow.IsPlaying) return;
            var player = Player.Instance;
            if (player == null || !player.Status.IsAlive) return;
            if (_respectStealth && !player.IsVisibleToEnemies) return;

            _fireTimer -= Time.deltaTime;
            if (_fireTimer > 0f) return;
            _fireTimer = _fireInterval;
            FireVolley(player.transform.position);
        }

        /// <summary>朝目標發射一波扇形彈幕。</summary>
        private void FireVolley(Vector2 target)
        {
            Vector2 origin = transform.position;
            Vector2 baseDir = (target - origin).normalized;
            int count = Mathf.Max(1, _projectilesPerVolley);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : (i / (float)(count - 1) - 0.5f);
                var dir = (Vector2)(Quaternion.Euler(0f, 0f, t * _spreadAngle) * baseDir);
                BossProjectile.Spawn(_projectileSprite, origin, dir * _projectileSpeed, _projectileDamage, _projectileLifetime, _projectileBlockMask);
            }
        }

        /// <summary>被魚叉命中（預設無效）。</summary>
        public bool TakeHit()
        {
            if (!_canBeDamaged || !IsAlive) return false;
            _hitCount++;
            return true;
        }
    }
}
