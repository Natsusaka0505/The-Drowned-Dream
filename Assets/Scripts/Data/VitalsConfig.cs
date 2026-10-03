using UnityEngine;

namespace DrownedDream
{
    /// <summary>復活時最大 SAN 的處理方案（F-DTH-02）。</summary>
    public enum RespawnSanityMode
    {
        /// <summary>方案 A：每次復活最大 SAN 下降，有下限。</summary>
        ReduceMax,
        /// <summary>方案 B：復活時 SAN 回滿，最大值不變。</summary>
        RefillOnly,
    }

    /// <summary>HP / 氧氣 / 死亡復活參數（F-OXY、F-DTH）。</summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Vitals Config", fileName = "VitalsConfig")]
    public class VitalsConfig : ScriptableObject
    {
        [Header("HP")]
        /// <summary>最大 HP</summary>
        [SerializeField] private float _maxHp = 100f;
        /// <summary>最大 HP（唯讀）</summary>
        public float MaxHp => _maxHp;
        /// <summary>受傷後無敵秒數</summary>
        [SerializeField] private float _invincibleTime = 1f;
        /// <summary>受傷後無敵秒數（唯讀）</summary>
        public float InvincibleTime => _invincibleTime;

        [Header("氧氣")]
        /// <summary>氧氣上限（洞窟內沒有任何補氧方式，只有復活時回滿）</summary>
        [SerializeField] private float _maxOxygen = 100f;
        /// <summary>氧氣上限（洞窟內沒有任何補氧方式，只有復活時回滿）（唯讀）</summary>
        public float MaxOxygen => _maxOxygen;
        /// <summary>呼吸狀態每秒消耗氧氣</summary>
        [SerializeField] private float _oxygenDrainPerSecond = 1f;
        /// <summary>呼吸狀態每秒消耗氧氣（唯讀）</summary>
        public float OxygenDrainPerSecond => _oxygenDrainPerSecond;
        /// <summary>氧氣歸零時每秒扣 HP</summary>
        [SerializeField] private float _hpDrainWhenNoOxygen = 10f;
        /// <summary>氧氣歸零時每秒扣 HP（唯讀）</summary>
        public float HpDrainWhenNoOxygen => _hpDrainWhenNoOxygen;

        [Header("死亡 / 復活（F-DTH）")]
        /// <summary>死亡到復活的演出秒數</summary>
        [SerializeField] private float _respawnDelay = 1.5f;
        /// <summary>死亡到復活的演出秒數（唯讀）</summary>
        public float RespawnDelay => _respawnDelay;
        /// <summary>[待確認] 復活時最大 SAN 的處理方案</summary>
        [SerializeField] private RespawnSanityMode _sanityMode = RespawnSanityMode.ReduceMax;
        /// <summary>[待確認] 復活時最大 SAN 的處理方案（唯讀）</summary>
        public RespawnSanityMode SanityMode => _sanityMode;
        /// <summary>方案 A：每次復活最大 SAN 下降量</summary>
        [SerializeField] private float _maxSanityPenalty = 10f;
        /// <summary>方案 A：每次復活最大 SAN 下降量（唯讀）</summary>
        public float MaxSanityPenalty => _maxSanityPenalty;
        /// <summary>方案 A：最大 SAN 下限</summary>
        [SerializeField] private float _minMaxSanity = 30f;
        /// <summary>方案 A：最大 SAN 下限（唯讀）</summary>
        public float MinMaxSanity => _minMaxSanity;
        /// <summary>[待確認] 死亡時是否歸還場上所有魚叉</summary>
        [SerializeField] private bool _returnHarpoonsOnDeath = true;
        /// <summary>[待確認] 死亡時是否歸還場上所有魚叉（唯讀）</summary>
        public bool ReturnHarpoonsOnDeath => _returnHarpoonsOnDeath;
    }
}
