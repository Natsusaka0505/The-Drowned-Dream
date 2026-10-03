using System.Collections;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>死亡 → 演出 → 於最近存檔點復活（F-DTH）。</summary>
    public class PlayerRespawn : MonoBehaviour
    {
        /// <summary>玩家入口（存取 Status 與各 Action）。</summary>
        private Player _player;
        /// <summary>目前復活點。</summary>
        private Vector2 _respawnPoint;

        /// <summary>預設以出生點為復活點。</summary>
        private void Awake()
        {
            _player = GetComponent<Player>();
            _respawnPoint = transform.position;
        }

        /// <summary>訂閱死亡事件。</summary>
        private void OnEnable() => GetComponent<PlayerStatus>().Died += OnDied;

        /// <summary>取消訂閱死亡事件。</summary>
        private void OnDisable() => GetComponent<PlayerStatus>().Died -= OnDied;

        /// <summary>更新復活點（存檔點呼叫）。</summary>
        public void SetRespawnPoint(Vector2 point) => _respawnPoint = point;

        /// <summary>死亡時開始復活流程。</summary>
        private void OnDied()
        {
            StartCoroutine(RespawnRoutine());
        }

        /// <summary>鎖輸入 → 等待 → 傳送並重置數值。</summary>
        private IEnumerator RespawnRoutine()
        {
            var vitals = _player.Status.Vitals;
            _player.Input.SetLocked(true);
            _player.Breath.ForceEnd();
            GameEvents.ShowMessage("意識沉入深淵……", vitals.RespawnDelay);
            yield return new WaitForSeconds(vitals.RespawnDelay);

            _player.Move.Teleport(_respawnPoint);
            if (vitals.ReturnHarpoonsOnDeath) _player.Attack.ClearActive();
            _player.Status.ApplyRespawn();
            _player.Breath.ResetBreath();
            _player.Confusion.ResetConfusion();

            _player.Input.SetLocked(false);
            GameEvents.RaisePlayerRespawned();
            if (vitals.SanityMode == RespawnSanityMode.ReduceMax)
            {
                GameEvents.ShowMessage($"你醒了過來……但心智更加脆弱（SAN 最大值 {_player.Status.SanityMax:0}）", 3f);
            }
        }
    }
}
