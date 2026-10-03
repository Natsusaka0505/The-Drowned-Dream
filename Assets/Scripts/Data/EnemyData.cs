using UnityEngine;

namespace DrownedDream
{
    /// <summary>敵人行為類型。</summary>
    public enum EnemyBehaviour
    {
        /// <summary>巡游魚怪：左右巡邏 → 偵測後左右追擊。</summary>
        Patrol,
        /// <summary>觸手：固定位置，玩家進入攻擊範圍時攻擊。</summary>
        Stationary,
        /// <summary>深淵之眼：不移動不攻擊，只有大範圍 SAN 影響。</summary>
        Passive,
    }

    /// <summary>敵人定義（F-ENM-07）。EnemyStatus 初始化時讀取。</summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Enemy", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        /// <summary>顯示名稱</summary>
        [SerializeField] private string _displayName = "Enemy";
        /// <summary>顯示名稱（唯讀）</summary>
        public string DisplayName => _displayName;
        /// <summary>行為類型</summary>
        [SerializeField] private EnemyBehaviour _behaviour = EnemyBehaviour.Patrol;
        /// <summary>行為類型（唯讀）</summary>
        public EnemyBehaviour Behaviour => _behaviour;
        /// <summary>被攻擊幾次死亡</summary>
        [SerializeField] private int _maxHits = 2;
        /// <summary>被攻擊幾次死亡（唯讀）</summary>
        public int MaxHits => _maxHits;

        [Header("移動")]
        /// <summary>巡邏移動速度</summary>
        [SerializeField] private float _moveSpeed = 2f;
        /// <summary>巡邏移動速度（唯讀）</summary>
        public float MoveSpeed => _moveSpeed;
        /// <summary>偵測到玩家後的追擊速度</summary>
        [SerializeField] private float _chaseSpeed = 4.5f;
        /// <summary>偵測到玩家後的追擊速度（唯讀）</summary>
        public float ChaseSpeed => _chaseSpeed;
        /// <summary>以出生點為中心的左右巡邏距離</summary>
        [SerializeField] private float _patrolDistance = 4f;
        /// <summary>以出生點為中心的左右巡邏距離（唯讀）</summary>
        public float PatrolDistance => _patrolDistance;

        [Header("範圍")]
        /// <summary>偵測範圍</summary>
        [SerializeField] private float _detectRange = 6f;
        /// <summary>偵測範圍（唯讀）</summary>
        public float DetectRange => _detectRange;
        /// <summary>追擊中超過偵測範圍 × 此倍率才放棄</summary>
        [SerializeField] private float _loseRangeMultiplier = 1.5f;
        /// <summary>追擊中超過偵測範圍 × 此倍率才放棄（唯讀）</summary>
        public float LoseRangeMultiplier => _loseRangeMultiplier;
        /// <summary>攻擊範圍（觸手為伸展距離）</summary>
        [SerializeField] private float _attackRange = 0.8f;
        /// <summary>攻擊範圍（觸手為伸展距離）（唯讀）</summary>
        public float AttackRange => _attackRange;
        /// <summary>恐懼範圍（玩家在內會掉 SAN）</summary>
        [SerializeField] private float _fearRange = 5f;
        /// <summary>恐懼範圍（玩家在內會掉 SAN）（唯讀）</summary>
        public float FearRange => _fearRange;

        [Header("攻擊")]
        /// <summary>攻擊傷害（扣 HP）</summary>
        [SerializeField] private float _attackDamage = 20f;
        /// <summary>攻擊傷害（扣 HP）（唯讀）</summary>
        public float AttackDamage => _attackDamage;
        /// <summary>觸手：攻擊前蓄力秒數</summary>
        [SerializeField] private float _attackWindup = 0.6f;
        /// <summary>觸手：攻擊前蓄力秒數（唯讀）</summary>
        public float AttackWindup => _attackWindup;
        /// <summary>觸手：攻擊判定持續秒數</summary>
        [SerializeField] private float _attackActiveTime = 0.4f;
        /// <summary>觸手：攻擊判定持續秒數（唯讀）</summary>
        public float AttackActiveTime => _attackActiveTime;
        /// <summary>觸手：攻擊後冷卻秒數</summary>
        [SerializeField] private float _attackCooldown = 1.5f;
        /// <summary>觸手：攻擊後冷卻秒數（唯讀）</summary>
        public float AttackCooldown => _attackCooldown;

        [Header("SAN")]
        /// <summary>恐懼範圍內每秒扣 SAN</summary>
        [SerializeField] private float _sanityDrainPerSecond = 5f;
        /// <summary>恐懼範圍內每秒扣 SAN（唯讀）</summary>
        public float SanityDrainPerSecond => _sanityDrainPerSecond;
        /// <summary>擊殺時恢復玩家 SAN</summary>
        [SerializeField] private float _sanityRestoreOnKill = 15f;
        /// <summary>擊殺時恢復玩家 SAN（唯讀）</summary>
        public float SanityRestoreOnKill => _sanityRestoreOnKill;

        [Header("掉落")]
        /// <summary>死亡掉落的道具 Prefab（可空）</summary>
        [SerializeField] private GameObject _dropPrefab;
        /// <summary>死亡掉落的道具 Prefab（可空）（唯讀）</summary>
        public GameObject DropPrefab => _dropPrefab;
        /// <summary>掉落機率</summary>
        [Range(0f, 1f)] [SerializeField] private float _dropChance = 1f;
        /// <summary>掉落機率（唯讀）</summary>
        public float DropChance => _dropChance;

        [Header("外觀（原型用色塊）")]
        /// <summary>原型色塊顏色</summary>
        [SerializeField] private Color _color = Color.red;
        /// <summary>原型色塊顏色（唯讀）</summary>
        public Color Color => _color;
        /// <summary>原型色塊 / 碰撞大小</summary>
        [SerializeField] private Vector2 _size = Vector2.one;
        /// <summary>原型色塊 / 碰撞大小（唯讀）</summary>
        public Vector2 Size => _size;
    }
}
