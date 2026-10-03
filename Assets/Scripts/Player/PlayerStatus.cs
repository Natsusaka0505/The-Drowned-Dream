using System;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 玩家 Status：集中所有玩家數值（docs/core Status + Action）。
    /// 數值只能透過本類別的方法修改，變動時發事件；氧氣、SAN 的自然增減也在這裡計算。
    /// </summary>
    public class PlayerStatus : MonoBehaviour
    {
        /// <summary>HP / 氧氣 / 復活參數。</summary>
        [SerializeField] private VitalsConfig _vitals;
        /// <summary>SAN 參數。</summary>
        [SerializeField] private SanityConfig _sanityConfig;
        /// <summary>移動參數（初始移動速度）。</summary>
        [SerializeField] private MovementConfig _movement;
        /// <summary>魚槍參數（魚叉上限）。</summary>
        [SerializeField] private HarpoonConfig _harpoon;
        /// <summary>封印 Boss 需要的封印道具數量。</summary>
        [SerializeField] private int _requiredSeals = 3;
        /// <summary>受傷無敵時閃爍的 Renderer。</summary>
        [SerializeField] private SpriteRenderer[] _flashRenderers;

        #region Status（script 分類：player status）

        /// <summary>氧氣值。</summary>
        public double Oxygen { get; private set; }
        /// <summary>目前 SAN 值。</summary>
        public double Sanity { get; private set; }
        /// <summary>SAN 最大值（會因復活下降）。</summary>
        public double SanityMax { get; private set; }
        /// <summary>HP。</summary>
        public double Hp { get; private set; }
        /// <summary>憋氣狀態。</summary>
        public bool IsHoldingBreath { get; private set; }
        /// <summary>呼吸（憋氣）CD 剩餘秒數。</summary>
        public double BreathCooldown { get; private set; }
        /// <summary>移動速度。</summary>
        public double MoveSpeed { get; private set; }
        /// <summary>武器（魚叉）數量。</summary>
        public int HarpoonCount { get; private set; }
        /// <summary>封印道具數量。</summary>
        public int SealCount { get; private set; }

        #endregion

        /// <summary>受傷無敵剩餘秒數。</summary>
        private double _invincibleTimer;
        /// <summary>目前 SAN 分段（變動時才發事件）。</summary>
        private int _sanityStage;

        /// <summary>氧氣上限。</summary>
        public double OxygenMax => _vitals.MaxOxygen;
        /// <summary>HP 上限。</summary>
        public double HpMax => _vitals.MaxHp;
        /// <summary>基礎 SAN 最大值（分段百分比以此計算）。</summary>
        public double SanityBaseMax => _sanityConfig.MaxSanity;
        /// <summary>魚叉上限。</summary>
        public int HarpoonMax => _harpoon.MaxAmmo;
        /// <summary>封印需要的數量。</summary>
        public int RequiredSeals => _requiredSeals;
        /// <summary>封印道具是否已齊。</summary>
        public bool HasAllSeals => SealCount >= _requiredSeals;
        /// <summary>是否還活著（HP &gt; 0）。</summary>
        public bool IsAlive => Hp > 0d;
        /// <summary>是否處於受傷無敵。</summary>
        public bool IsInvincible => _invincibleTimer > 0d;
        /// <summary>目前 SAN / 基礎最大 SAN。</summary>
        public double SanityRatio => SanityBaseMax > 0d ? Sanity / SanityBaseMax : 0d;
        /// <summary>低 SAN 分段：0 = 正常，數字越大越瘋狂。</summary>
        public int SanityStage => _sanityStage;
        /// <summary>是否位於恐懼範圍內。</summary>
        public bool InFear { get; private set; }

        /// <summary>HP / 氧氣 / 復活參數。</summary>
        public VitalsConfig Vitals => _vitals;
        /// <summary>SAN 參數。</summary>
        public SanityConfig SanityConfig => _sanityConfig;
        /// <summary>移動參數。</summary>
        public MovementConfig Movement => _movement;
        /// <summary>魚槍參數。</summary>
        public HarpoonConfig Harpoon => _harpoon;

        /// <summary>HP 變動（目前, 上限）。</summary>
        public event Action<double, double> HpChanged;
        /// <summary>氧氣變動（目前, 上限）。</summary>
        public event Action<double, double> OxygenChanged;
        /// <summary>SAN 變動（目前, 最大值, 基礎最大值）。</summary>
        public event Action<double, double, double> SanityChanged;
        /// <summary>SAN 分段變動。</summary>
        public event Action<int> SanityStageChanged;
        /// <summary>魚叉數變動（目前, 上限）。</summary>
        public event Action<int, int> HarpoonCountChanged;
        /// <summary>封印道具數變動（目前, 需要）。</summary>
        public event Action<int, int> SealCountChanged;
        /// <summary>受到攻擊。</summary>
        public event Action Damaged;
        /// <summary>死亡（HP 歸零）。</summary>
        public event Action Died;

        /// <summary>依設定初始化所有數值。</summary>
        private void Awake()
        {
            Hp = HpMax;
            Oxygen = OxygenMax;
            SanityMax = SanityBaseMax;
            Sanity = SanityMax;
            MoveSpeed = _movement.MaxSpeed;
            HarpoonCount = HarpoonMax;
        }

        /// <summary>通知所有初始值。</summary>
        private void Start() => NotifyAll();

        /// <summary>被動計時：無敵、氧氣消耗 / 窒息、SAN 增減。</summary>
        private void Update()
        {
            double dt = Time.deltaTime;
            UpdateInvincible(dt);
            if (!IsAlive || !GameFlow.IsPlaying) return;
            UpdateOxygen(dt);
            UpdateSanity(dt);
        }

        #region 修改方法

        /// <summary>受到攻擊（有無敵時間），回傳是否真的受傷。</summary>
        public bool TakeHit(double damage)
        {
            if (!IsAlive || IsInvincible || damage <= 0d) return false;
            _invincibleTimer = _vitals.InvincibleTime;
            SetHp(Hp - damage);
            Damaged?.Invoke();
            return true;
        }

        /// <summary>回復 HP（不超過上限）。</summary>
        public void RestoreHp(double amount) => SetHp(Hp + amount);

        /// <summary>回復 SAN（不超過目前最大值）。</summary>
        public void RestoreSanity(double amount) => SetSanity(Sanity + amount);

        /// <summary>設定憋氣狀態與 CD（由 PlayerBreath 呼叫）。</summary>
        public void SetBreath(bool holding, double cooldown)
        {
            IsHoldingBreath = holding;
            BreathCooldown = Math.Max(0d, cooldown);
        }

        /// <summary>設定移動速度（之後可用於減速 / 加速效果）。</summary>
        public void SetMoveSpeed(double speed) => MoveSpeed = Math.Max(0d, speed);

        /// <summary>增減魚叉數（夾在 0 ~ 上限）。</summary>
        public void AddHarpoons(int delta)
        {
            HarpoonCount = Mathf.Clamp(HarpoonCount + delta, 0, HarpoonMax);
            HarpoonCountChanged?.Invoke(HarpoonCount, HarpoonMax);
        }

        /// <summary>增加一個封印道具。</summary>
        public void AddSeal()
        {
            SealCount++;
            SealCountChanged?.Invoke(SealCount, _requiredSeals);
        }

        /// <summary>復活時重置（F-DTH-02/03）：HP、氧氣回滿，SAN 依方案處理，魚叉依設定歸還。</summary>
        public void ApplyRespawn()
        {
            _invincibleTimer = 0d;
            SetFlashVisible(true);
            if (_vitals.SanityMode == RespawnSanityMode.ReduceMax)
            {
                SanityMax = Math.Max(_vitals.MinMaxSanity, SanityMax - _vitals.MaxSanityPenalty);
            }
            SetHp(HpMax);
            SetOxygen(OxygenMax);
            SetSanity(SanityMax);
            if (_vitals.ReturnHarpoonsOnDeath) AddHarpoons(HarpoonMax);
        }

        #endregion

        /// <summary>倒數無敵並閃爍。</summary>
        private void UpdateInvincible(double dt)
        {
            if (_invincibleTimer <= 0d) return;
            _invincibleTimer -= dt;
            SetFlashVisible(_invincibleTimer <= 0d || Mathf.Repeat((float)_invincibleTimer, 0.15f) > 0.075f);
        }

        /// <summary>只有憋氣時耗氧（F-OXY-02）；氧氣歸零後仍憋氣則改扣 HP（不觸發無敵）。</summary>
        private void UpdateOxygen(double dt)
        {
            if (!IsHoldingBreath) return;
            if (Oxygen <= 0d)
            {
                SetHp(Hp - _vitals.HpDrainWhenNoOxygen * dt);
                return;
            }
            SetOxygen(Oxygen - _vitals.OxygenDrainPerSecond * dt);
        }

        /// <summary>恐懼範圍內掉 SAN，離開後恢復。</summary>
        private void UpdateSanity(double dt)
        {
            double drain = 0d;
            if (_sanityConfig.DrainWhileInvisible || !IsHoldingBreath) drain = FearSource.TotalDrainAt(transform.position);
            InFear = drain > 0d;

            if (InFear) SetSanity(Sanity - drain * dt);
            else if (Sanity < SanityMax) SetSanity(Sanity + _sanityConfig.RecoverPerSecond * dt);
        }

        /// <summary>設定 HP，歸零時發出死亡事件。</summary>
        private void SetHp(double value)
        {
            bool wasAlive = IsAlive;
            Hp = Math.Clamp(value, 0d, HpMax);
            HpChanged?.Invoke(Hp, HpMax);
            if (wasAlive && !IsAlive)
            {
                Died?.Invoke();
                GameEvents.RaisePlayerDied();
            }
        }

        /// <summary>設定氧氣。</summary>
        private void SetOxygen(double value)
        {
            Oxygen = Math.Clamp(value, 0d, OxygenMax);
            OxygenChanged?.Invoke(Oxygen, OxygenMax);
        }

        /// <summary>設定 SAN 並更新分段。</summary>
        private void SetSanity(double value)
        {
            Sanity = Math.Clamp(value, 0d, SanityMax);
            SanityChanged?.Invoke(Sanity, SanityMax, SanityBaseMax);

            int stage = 0;
            var thresholds = _sanityConfig.StageThresholds;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (SanityRatio < thresholds[i]) stage = i + 1;
            }
            if (stage == _sanityStage) return;
            _sanityStage = stage;
            SanityStageChanged?.Invoke(stage);
        }

        /// <summary>切換受傷閃爍的顯示。</summary>
        private void SetFlashVisible(bool visible)
        {
            foreach (var r in _flashRenderers)
            {
                if (r != null) r.enabled = visible;
            }
        }

        /// <summary>發出所有數值事件（初始化 / UI 同步用）。</summary>
        public void NotifyAll()
        {
            HpChanged?.Invoke(Hp, HpMax);
            OxygenChanged?.Invoke(Oxygen, OxygenMax);
            SanityChanged?.Invoke(Sanity, SanityMax, SanityBaseMax);
            HarpoonCountChanged?.Invoke(HarpoonCount, HarpoonMax);
            SealCountChanged?.Invoke(SealCount, _requiredSeals);
        }
    }
}
