using System;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 敵人 Status（docs/core）：被攻擊次數、移動速度、偵測 / 攻擊範圍、移動狀態、是否偵測到玩家。
    /// 被魚叉命中達上限即死亡：恢復玩家 SAN、依機率掉落道具。
    /// </summary>
    [RequireComponent(typeof(FearSource))]
    public class EnemyStatus : MonoBehaviour, IDamageable
    {
        /// <summary>敵人資料（初始值來源）。</summary>
        [SerializeField] private EnemyData _data;

        #region Status（script 分類：enemy status）

        /// <summary>被攻擊次數。</summary>
        public int HitCount { get; private set; }
        /// <summary>移動速度。</summary>
        public double MoveSpeed { get; private set; }
        /// <summary>偵測範圍。</summary>
        public double DetectRange { get; private set; }
        /// <summary>攻擊範圍。</summary>
        public double AttackRange { get; private set; }
        /// <summary>移動狀態（是否正在移動）。</summary>
        public bool IsMoving { get; private set; }
        /// <summary>是否偵測到玩家。</summary>
        public bool PlayerDetected { get; private set; }

        #endregion

        /// <summary>被攻擊幾次死亡。</summary>
        public int MaxHits => _data.MaxHits;
        /// <summary>是否還活著。</summary>
        public bool IsAlive => HitCount < MaxHits;
        /// <summary>敵人資料。</summary>
        public EnemyData Data => _data;

        /// <summary>被命中（參數：目前被攻擊次數）。</summary>
        public event Action<int> Hit;
        /// <summary>死亡。</summary>
        public event Action Died;

        /// <summary>依資料初始化數值與恐懼範圍。</summary>
        private void Awake()
        {
            MoveSpeed = _data.MoveSpeed;
            DetectRange = _data.DetectRange;
            AttackRange = _data.AttackRange;
            var fear = GetComponent<FearSource>();
            fear.Radius = _data.FearRange;
            fear.DrainPerSecond = _data.SanityDrainPerSecond;
        }

        /// <summary>設定移動狀態（由 EnemyAI 呼叫）。</summary>
        public void SetMoving(bool moving) => IsMoving = moving;

        /// <summary>設定是否偵測到玩家（由 EnemyAI 呼叫）。</summary>
        public void SetPlayerDetected(bool detected) => PlayerDetected = detected;

        /// <summary>設定移動速度（巡邏 / 追擊切換）。</summary>
        public void SetMoveSpeed(double speed) => MoveSpeed = Math.Max(0d, speed);

        /// <summary>被魚叉命中一次，達上限時死亡。</summary>
        public bool TakeHit()
        {
            if (!IsAlive) return false;
            HitCount++;
            Hit?.Invoke(HitCount);
            if (!IsAlive) Die();
            return true;
        }

        /// <summary>死亡：關閉恐懼範圍、恢復玩家 SAN、掉落道具、移除自己。</summary>
        private void Die()
        {
            GetComponent<FearSource>().enabled = false;
            var player = Player.Instance;
            if (player != null) player.Status.RestoreSanity(_data.SanityRestoreOnKill);

            if (_data.DropPrefab != null && UnityEngine.Random.value <= _data.DropChance)
            {
                Instantiate(_data.DropPrefab, transform.position, Quaternion.identity);
            }
            Died?.Invoke();
            GameEvents.ShowMessage($"擊殺 {_data.DisplayName}", 1.2f);
            Destroy(gameObject, 0.1f);
        }
    }
}
