using System;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>憋氣狀態。</summary>
    public enum BreathState
    {
        /// <summary>可以憋氣。</summary>
        Ready,
        /// <summary>憋氣中（隱形）。</summary>
        Holding,
        /// <summary>冷卻中。</summary>
        Cooldown,
    }

    /// <summary>
    /// Action：憋氣潛行（F-BRE）。按鍵進入，再按提早結束；時間到自動結束。
    /// CD 依實際憋氣時間比例計算。結果寫回 PlayerStatus.IsHoldingBreath / BreathCooldown。
    /// </summary>
    public class PlayerBreath : MonoBehaviour
    {
        /// <summary>憋氣參數。</summary>
        [SerializeField] private BreathConfig _config;
        /// <summary>憋氣時變半透明的 Renderer。</summary>
        [SerializeField] private SpriteRenderer[] _fadeRenderers;

        /// <summary>玩家數值。</summary>
        private PlayerStatus _status;
        /// <summary>輸入來源。</summary>
        private PlayerInputReader _input;
        /// <summary>本次已憋氣秒數。</summary>
        private float _holdTimer;
        /// <summary>CD 剩餘秒數。</summary>
        private float _cooldownTimer;
        /// <summary>本次 CD 總秒數（UI 顯示比例用）。</summary>
        private float _cooldownTotal;

        /// <summary>目前狀態。</summary>
        public BreathState State { get; private set; } = BreathState.Ready;
        /// <summary>憋氣剩餘秒數。</summary>
        public float HoldRemaining => State == BreathState.Holding ? Mathf.Max(0f, _config.MaxHoldTime - _holdTimer) : 0f;
        /// <summary>憋氣最大秒數。</summary>
        public float MaxHoldTime => _config.MaxHoldTime;
        /// <summary>本次 CD 總秒數。</summary>
        public float CooldownTotal => _cooldownTotal;

        /// <summary>狀態變更。</summary>
        public event Action<BreathState> StateChanged;

        /// <summary>快取元件。</summary>
        private void Awake()
        {
            _status = GetComponent<PlayerStatus>();
            _input = GetComponent<PlayerInputReader>();
        }

        /// <summary>依狀態處理輸入與計時，並同步到 Status。</summary>
        private void Update()
        {
            switch (State)
            {
                case BreathState.Ready:
                    if (_input.BreathPressed && _status.IsAlive) StartHold();
                    break;

                case BreathState.Holding:
                    _holdTimer += Time.deltaTime;
                    if (_input.BreathPressed || _holdTimer >= _config.MaxHoldTime || !_status.IsAlive) EndHold();
                    break;

                case BreathState.Cooldown:
                    _cooldownTimer -= Time.deltaTime;
                    if (_cooldownTimer <= 0f) SetState(BreathState.Ready);
                    break;
            }
            _status.SetBreath(State == BreathState.Holding, State == BreathState.Cooldown ? _cooldownTimer : 0f);
        }

        /// <summary>強制結束憋氣（攻擊 / 死亡時）。</summary>
        public void ForceEnd()
        {
            if (State == BreathState.Holding) EndHold();
        }

        /// <summary>重置為可憋氣（復活用）。</summary>
        public void ResetBreath()
        {
            _holdTimer = 0f;
            _cooldownTimer = 0f;
            SetState(BreathState.Ready);
        }

        /// <summary>開始憋氣。</summary>
        private void StartHold()
        {
            _holdTimer = 0f;
            SetState(BreathState.Holding);
        }

        /// <summary>結束憋氣並依比例計算 CD。</summary>
        private void EndHold()
        {
            float ratio = Mathf.Clamp01(_holdTimer / _config.MaxHoldTime);
            _cooldownTotal = _config.Cooldown * Mathf.Max(_config.MinCooldownRatio, ratio);
            _cooldownTimer = _cooldownTotal;
            SetState(BreathState.Cooldown);
        }

        /// <summary>切換狀態、同步 Status、更新透明度。</summary>
        private void SetState(BreathState state)
        {
            State = state;
            _status.SetBreath(state == BreathState.Holding, state == BreathState.Cooldown ? _cooldownTimer : 0f);
            float alpha = state == BreathState.Holding ? _config.HoldAlpha : 1f;
            foreach (var r in _fadeRenderers)
            {
                if (r == null) continue;
                var c = r.color;
                c.a = alpha;
                r.color = c;
            }
            StateChanged?.Invoke(state);
        }
    }
}
