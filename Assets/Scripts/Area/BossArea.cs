using System.Collections;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// area（docs/core）：Boss 房。放在 Boss 房內，執行時自動找出所在的 Room。
    /// 第一次進房 → 鏡頭特寫 Boss 數秒再切回玩家；道具不齊 → Boss 啟動；離房 → Boss 停止（F-BOSS-02/04/05/06）。
    /// </summary>
    public class BossArea : MonoBehaviour
    {
        /// <summary>房內的 Boss。</summary>
        [SerializeField] private BossController _boss;
        /// <summary>第一次進房的鏡頭特寫秒數。</summary>
        [SerializeField] private float _closeUpSeconds = 2.5f;
        /// <summary>[待確認] Boss 是否追出房間（原型：否）。</summary>
        [SerializeField] private bool _bossFollowsOutside;

        #region Status

        /// <summary>是否第一次進到 Boss 房間。</summary>
        public bool IsFirstEntry { get; private set; } = true;
        /// <summary>玩家目前是否在 Boss 房內。</summary>
        public bool PlayerInside { get; private set; }

        #endregion

        /// <summary>所在區塊。</summary>
        private Room _room;

        /// <summary>找出所在區塊並訂閱進出事件（Room 在 OnEnable 註冊，因此放在 Start）。</summary>
        private void Start()
        {
            _room = Room.FindAt(transform.position);
            if (_room == null)
            {
                Debug.LogError("[DrownedDream] BossArea 不在任何 Room 範圍內", this);
                return;
            }
            _room.PlayerEntered += OnPlayerEntered;
            _room.PlayerExited += OnPlayerExited;
        }

        /// <summary>取消訂閱。</summary>
        private void OnDestroy()
        {
            if (_room == null) return;
            _room.PlayerEntered -= OnPlayerEntered;
            _room.PlayerExited -= OnPlayerExited;
        }

        #region Action

        /// <summary>偵測到玩家進入 Boss 房。</summary>
        private void OnPlayerEntered()
        {
            PlayerInside = true;
            var player = Player.Instance;
            if (player == null || _boss == null || _boss.IsSealed) return;

            if (IsFirstEntry)
            {
                IsFirstEntry = false;
                StartCoroutine(FirstEntryRoutine(player));
                return;
            }
            AnnounceAndActivate(player);
        }

        /// <summary>偵測到玩家離開 Boss 房。</summary>
        private void OnPlayerExited()
        {
            PlayerInside = false;
            if (_boss != null && !_bossFollowsOutside) _boss.Deactivate();
        }

        /// <summary>第一次進房：鎖輸入 → 鏡頭特寫 Boss → 切回玩家 → 啟動。</summary>
        private IEnumerator FirstEntryRoutine(Player player)
        {
            player.Input.SetLocked(true);
            var cam = GameCamera.Instance;
            if (cam != null) cam.SwitchToBossRoom(_boss.gameObject);
            GameEvents.RaiseBossRevealed();
            GameEvents.ShowMessage("……牠在這裡。", _closeUpSeconds);
            yield return new WaitForSeconds(_closeUpSeconds);

            if (cam != null) cam.SwitchToPlayer();
            player.Input.SetLocked(false);
            if (PlayerInside) AnnounceAndActivate(player);
        }

        /// <summary>提示封印進度；道具不齊則啟動 Boss。</summary>
        private void AnnounceAndActivate(Player player)
        {
            var status = player.Status;
            if (status.HasAllSeals)
            {
                GameEvents.ShowMessage($"封印道具已齊（{status.SealCount}/{status.RequiredSeals}）——前往祭壇按 E 封印", 3f);
                return;
            }
            GameEvents.ShowMessage($"封印道具不足（{status.SealCount}/{status.RequiredSeals}）……牠醒了！", 3f);
            _boss.Activate();
        }

        #endregion
    }
}
