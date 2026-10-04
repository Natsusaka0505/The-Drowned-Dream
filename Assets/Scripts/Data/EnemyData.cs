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

        [Header("巡游魚怪：蓄力衝刺")]
        /// <summary>衝刺觸發距離（玩家在此距離內才衝刺）</summary>
        [SerializeField] private float _dashRange = 5f;
        /// <summary>衝刺觸發距離（玩家在此距離內才衝刺）（唯讀）</summary>
        public float DashRange => _dashRange;
        /// <summary>衝刺前蓄力秒數（停下閃紅）</summary>
        [SerializeField] private float _dashWindup = 0.5f;
        /// <summary>衝刺前蓄力秒數（停下閃紅）（唯讀）</summary>
        public float DashWindup => _dashWindup;
        /// <summary>衝刺速度</summary>
        [SerializeField] private float _dashSpeed = 12f;
        /// <summary>衝刺速度（唯讀）</summary>
        public float DashSpeed => _dashSpeed;
        /// <summary>衝刺持續秒數</summary>
        [SerializeField] private float _dashTime = 0.35f;
        /// <summary>衝刺持續秒數（唯讀）</summary>
        public float DashTime => _dashTime;
        /// <summary>衝刺後冷卻秒數</summary>
        [SerializeField] private float _dashCooldown = 2.5f;
        /// <summary>衝刺後冷卻秒數（唯讀）</summary>
        public float DashCooldown => _dashCooldown;

        [Header("巡游魚怪：泡泡彈")]
        /// <summary>追擊中吐泡泡彈的間隔秒數（0 = 不吐）</summary>
        [SerializeField] private float _bubbleInterval = 3f;
        /// <summary>追擊中吐泡泡彈的間隔秒數（0 = 不吐）（唯讀）</summary>
        public float BubbleInterval => _bubbleInterval;
        /// <summary>泡泡彈速度</summary>
        [SerializeField] private float _bubbleSpeed = 3f;
        /// <summary>泡泡彈速度（唯讀）</summary>
        public float BubbleSpeed => _bubbleSpeed;
        /// <summary>泡泡彈直徑</summary>
        [SerializeField] private float _bubbleSize = 0.6f;
        /// <summary>泡泡彈直徑（唯讀）</summary>
        public float BubbleSize => _bubbleSize;
        /// <summary>泡泡彈存活秒數</summary>
        [SerializeField] private float _bubbleLifetime = 4f;
        /// <summary>泡泡彈存活秒數（唯讀）</summary>
        public float BubbleLifetime => _bubbleLifetime;
        /// <summary>泡泡彈動畫畫格（依序循環；空著用單色圓形）。</summary>
        [SerializeField] private Sprite[] _bubbleFrames;
        /// <summary>泡泡彈動畫畫格。</summary>
        public Sprite[] BubbleFrames => _bubbleFrames;
        /// <summary>泡泡彈每格秒數。</summary>
        [SerializeField] private float _bubbleFrameDuration = 0.16f;
        /// <summary>泡泡彈每格秒數。</summary>
        public float BubbleFrameDuration => _bubbleFrameDuration;
        /// <summary>泡泡彈動畫外觀放大倍率（判定大小不變）。</summary>
        [SerializeField] private float _bubbleArtScale = 1.6f;
        /// <summary>泡泡彈動畫外觀放大倍率。</summary>
        public float BubbleArtScale => _bubbleArtScale;

        [Header("觸手：地面突刺")]
        /// <summary>地刺間隔秒數（玩家在偵測範圍內、橫戳範圍外時使用；0 = 不用）</summary>
        [SerializeField] private float _spikeCooldown = 3f;
        /// <summary>地刺間隔秒數（玩家在偵測範圍內、橫戳範圍外時使用；0 = 不用）（唯讀）</summary>
        public float SpikeCooldown => _spikeCooldown;
        /// <summary>地刺預告秒數</summary>
        [SerializeField] private float _spikeWindup = 0.7f;
        /// <summary>地刺預告秒數（唯讀）</summary>
        public float SpikeWindup => _spikeWindup;
        /// <summary>地刺判定秒數</summary>
        [SerializeField] private float _spikeActive = 0.3f;
        /// <summary>地刺判定秒數（唯讀）</summary>
        public float SpikeActive => _spikeActive;
        /// <summary>地刺大小（寬, 高）</summary>
        [SerializeField] private Vector2 _spikeSize = new Vector2(1.2f, 2.2f);
        /// <summary>地刺大小（寬, 高）（唯讀）</summary>
        public Vector2 SpikeSize => _spikeSize;

        [Header("深淵之眼：凝視光束")]
        /// <summary>光束間隔秒數（0 = 不發射）</summary>
        [SerializeField] private float _beamCooldown = 3.5f;
        /// <summary>光束間隔秒數（0 = 不發射）（唯讀）</summary>
        public float BeamCooldown => _beamCooldown;
        /// <summary>瞄準（預告）秒數</summary>
        [SerializeField] private float _beamAim = 1f;
        /// <summary>瞄準（預告）秒數（唯讀）</summary>
        public float BeamAim => _beamAim;
        /// <summary>光束判定秒數</summary>
        [SerializeField] private float _beamActive = 0.35f;
        /// <summary>光束判定秒數（唯讀）</summary>
        public float BeamActive => _beamActive;
        /// <summary>光束最長距離（碰到地形會截斷）</summary>
        [SerializeField] private float _beamLength = 14f;
        /// <summary>光束最長距離（碰到地形會截斷）（唯讀）</summary>
        public float BeamLength => _beamLength;
        /// <summary>光束寬度</summary>
        [SerializeField] private float _beamWidth = 0.6f;
        /// <summary>光束寬度（唯讀）</summary>
        public float BeamWidth => _beamWidth;
        /// <summary>光束命中額外扣的 SAN</summary>
        [SerializeField] private float _beamSanityDamage = 15f;
        /// <summary>光束命中額外扣的 SAN（唯讀）</summary>
        public float BeamSanityDamage => _beamSanityDamage;
        /// <summary>光束特效（FX Lightning II 的 fx_lightning_01；空 = 只顯示色塊）</summary>
        [SerializeField] private GameObject _beamFx;
        /// <summary>光束特效（唯讀）</summary>
        public GameObject BeamFx => _beamFx;
        /// <summary>光束特效寬度（閃電圖只佔畫格約 1/4 寬，所以比判定寬）</summary>
        [SerializeField] private float _beamFxWidth = 2.4f;
        /// <summary>光束特效寬度（唯讀）</summary>
        public float BeamFxWidth => _beamFxWidth;
        /// <summary>光束特效存在秒數</summary>
        [SerializeField] private float _beamFxTime = 0.45f;
        /// <summary>光束特效存在秒數（唯讀）</summary>
        public float BeamFxTime => _beamFxTime;

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

        [Header("漂浮")]
        /// <summary>離地高度（碰撞框底部比地面高多少；水母等漂浮怪用）</summary>
        [SerializeField] private float _hoverHeight;
        /// <summary>離地高度（唯讀）</summary>
        public float HoverHeight => _hoverHeight;
        /// <summary>外觀上下漂浮幅度（只動外觀）</summary>
        [SerializeField] private float _bobHeight = 0.08f;
        /// <summary>外觀上下漂浮幅度（唯讀）</summary>
        public float BobHeight => _bobHeight;
        /// <summary>外觀上下漂浮速度（弧度 / 秒）</summary>
        [SerializeField] private float _bobSpeed = 2.5f;
        /// <summary>外觀上下漂浮速度（唯讀）</summary>
        public float BobSpeed => _bobSpeed;

        [Header("外觀（逐格動畫；沒有畫格時用原型色塊）")]
        /// <summary>外觀畫格（依序循環播放）</summary>
        [SerializeField] private Sprite[] _animFrames;
        /// <summary>外觀畫格（唯讀）</summary>
        public Sprite[] AnimFrames => _animFrames;
        /// <summary>每格秒數（長度不足時用 0.1 秒）</summary>
        [SerializeField] private float[] _animDurations;
        /// <summary>每格秒數（唯讀）</summary>
        public float[] AnimDurations => _animDurations;
        /// <summary>底層靜態圖（例如眼白；有設定時畫格疊在最上層，例如眼皮）</summary>
        [SerializeField] private Sprite _baseSprite;
        /// <summary>底層靜態圖（唯讀）</summary>
        public Sprite BaseSprite => _baseSprite;
        /// <summary>追視玩家的圖（例如瞳孔，夾在底層與畫格之間）</summary>
        [SerializeField] private Sprite _lookSprite;
        /// <summary>追視玩家的圖（唯讀）</summary>
        public Sprite LookSprite => _lookSprite;
        /// <summary>追視圖最大偏移（底層圖的局部單位）</summary>
        [SerializeField] private float _lookRadius = 0.22f;
        /// <summary>追視圖最大偏移（唯讀）</summary>
        public float LookRadius => _lookRadius;
        /// <summary>攻擊用觸鬚圖（觸手橫戳時朝玩家伸出；空 = 拉長身體）</summary>
        [SerializeField] private Sprite _tendrilSprite;
        /// <summary>攻擊用觸鬚圖（唯讀）</summary>
        public Sprite TendrilSprite => _tendrilSprite;

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
