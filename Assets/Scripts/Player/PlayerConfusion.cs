using System;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 低 SAN 方向錯亂（F-SAN-11/12）。
    /// [待確認] 判定時機：每隔 ConfusionCheckInterval 擲一次機率，命中 → 預告 → 左右反轉一段時間。
    /// </summary>
    public class PlayerConfusion : MonoBehaviour
    {
        /// <summary>玩家數值（SAN 比例）。</summary>
        private PlayerStatus _status;
        /// <summary>距離下次判定的秒數。</summary>
        private float _checkTimer;
        /// <summary>預告剩餘秒數。</summary>
        private float _warningTimer;
        /// <summary>反轉剩餘秒數。</summary>
        private float _confusedTimer;

        /// <summary>是否正在預告。</summary>
        public bool IsWarning => _warningTimer > 0f;
        /// <summary>是否正在左右反轉。</summary>
        public bool IsConfused => _confusedTimer > 0f;
        /// <summary>水平輸入倍率（反轉時為 -1）。</summary>
        public float HorizontalMultiplier => IsConfused ? -1f : 1f;

        /// <summary>開始預告。</summary>
        public event Action WarningStarted;
        /// <summary>開始反轉。</summary>
        public event Action ConfusionStarted;
        /// <summary>反轉結束。</summary>
        public event Action ConfusionEnded;

        /// <summary>SAN 參數。</summary>
        private SanityConfig Config => _status.SanityConfig;

        /// <summary>快取 Status。</summary>
        private void Awake()
        {
            _status = GetComponent<PlayerStatus>();
        }

        /// <summary>初始化判定計時。</summary>
        private void Start()
        {
            _checkTimer = Config.ConfusionCheckInterval;
        }

        /// <summary>依狀態推進預告 / 反轉 / 判定。</summary>
        private void Update()
        {
            if (!GameFlow.IsPlaying) return;
            float dt = Time.deltaTime;

            if (IsConfused)
            {
                _confusedTimer -= dt;
                if (_confusedTimer <= 0f) ConfusionEnded?.Invoke();
                return;
            }

            if (IsWarning)
            {
                _warningTimer -= dt;
                if (_warningTimer <= 0f)
                {
                    _confusedTimer = Config.ConfusionDuration;
                    ConfusionStarted?.Invoke();
                }
                return;
            }

            _checkTimer -= dt;
            if (_checkTimer > 0f) return;
            _checkTimer = Config.ConfusionCheckInterval;

            if (UnityEngine.Random.value < CurrentChance())
            {
                _warningTimer = Config.ConfusionWarningTime;
                WarningStarted?.Invoke();
            }
        }

        /// <summary>目前錯亂機率：SAN ≥ 起始比例為 0，之後線性增加至 SAN 0 時為最大機率。</summary>
        public float CurrentChance()
        {
            float ratio = (float)_status.SanityRatio;
            if (ratio >= Config.ConfusionStartRatio) return 0f;
            float t = 1f - ratio / Config.ConfusionStartRatio;
            return t * Config.ConfusionMaxChance;
        }

        /// <summary>清除錯亂狀態（復活用）。</summary>
        public void ResetConfusion()
        {
            bool wasConfused = IsConfused;
            _warningTimer = 0f;
            _confusedTimer = 0f;
            _checkTimer = Config.ConfusionCheckInterval;
            if (wasConfused) ConfusionEnded?.Invoke();
        }
    }
}
