using System.Collections;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// area（docs/core）：Boss 廳。以自己的 BoxCollider2D（Trigger）涵蓋整個 Boss 廳。
    /// 第一次進廳 → 鏡頭特寫 Boss 數秒再切回玩家；道具不齊 → Boss 啟動；離廳 → Boss 停止（F-BOSS-02/04/05/06/08）。
    /// 撿齊所有封印碎片 → 鏡頭切到 Boss 廳數秒提示回去封印（F-BOSS-09）。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BossArea : MonoBehaviour
    {
        /// <summary>廳內的 Boss。</summary>
        [SerializeField] private BossController _boss;
        /// <summary>第一次進廳的鏡頭特寫秒數。</summary>
        [SerializeField] private float _closeUpSeconds = 2.5f;
        /// <summary>撿齊碎片時鏡頭停在 Boss 廳的秒數。</summary>
        [SerializeField] private float _allSealsSeconds = 3f;
        /// <summary>[待確認] Boss 是否追出房間（原型：否）。</summary>
        [SerializeField] private bool _bossFollowsOutside;

        #region Status

        /// <summary>是否第一次進到 Boss 廳。</summary>
        public bool IsFirstEntry { get; private set; } = true;
        /// <summary>玩家目前是否在 Boss 廳內。</summary>
        public bool PlayerInside { get; private set; }

        #endregion

        /// <summary>已訂閱封印數變化的玩家數值。</summary>
        private PlayerStatus _status;
        /// <summary>撿齊碎片的鏡頭演出是否已播過。</summary>
        private bool _allSealsShown;
        /// <summary>是否正在播鏡頭演出（避免重疊）。</summary>
        private bool _cutscenePlaying;

        /// <summary>設定觸發框。</summary>
        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        /// <summary>訂閱封印數變化（Player 在 Awake 註冊，因此放在 Start）。</summary>
        private void Start()
        {
            if (Player.Instance == null) return;
            _status = Player.Instance.Status;
            _status.SealCountChanged += OnSealCountChanged;
        }

        /// <summary>取消訂閱。</summary>
        private void OnDestroy()
        {
            if (_status != null) _status.SealCountChanged -= OnSealCountChanged;
        }

        #region Action

        /// <summary>偵測到玩家進入 Boss 廳。</summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other) || PlayerInside) return;
            PlayerInside = true;
            var player = Player.Instance;
            if (player == null || _boss == null || _boss.IsSealed) return;

            if (IsFirstEntry)
            {
                IsFirstEntry = false;
                StartCoroutine(ShowBossRoutine(player, _closeUpSeconds, "……牠在這裡。", activateAfter: true));
                return;
            }
            AnnounceAndActivate(player);
        }

        /// <summary>偵測到玩家離開 Boss 廳。</summary>
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other) || !PlayerInside) return;
            PlayerInside = false;
            if (_boss != null && !_bossFollowsOutside) _boss.Deactivate();
        }

        /// <summary>封印數變化：第一次撿齊時播放 Boss 廳鏡頭。</summary>
        private void OnSealCountChanged(int count, int required)
        {
            if (_allSealsShown || count < required || _boss == null || _boss.IsSealed) return;
            _allSealsShown = true;
            if (PlayerInside) return; // 已經在廳內就不用再帶過去看
            StartCoroutine(ShowBossRoutine(Player.Instance, _allSealsSeconds, "封印碎片已齊……回到牠沉睡的地方。", activateAfter: false));
        }

        /// <summary>鏡頭演出：鎖輸入 → 鏡頭切到 Boss → 等待 → 切回玩家 → 解鎖（activateAfter 時接著判斷 Boss 是否啟動）。</summary>
        private IEnumerator ShowBossRoutine(Player player, float seconds, string message, bool activateAfter)
        {
            while (_cutscenePlaying) yield return null;
            _cutscenePlaying = true;

            if (player != null) player.Input.SetLocked(true);
            var cam = GameCamera.Instance;
            if (cam != null) cam.SwitchToBossRoom(_boss.gameObject);
            GameEvents.ShowMessage(message, seconds);
            yield return new WaitForSeconds(seconds);

            if (cam != null) cam.SwitchToPlayer();
            if (player != null) player.Input.SetLocked(false);
            _cutscenePlaying = false;
            if (activateAfter && PlayerInside && player != null) AnnounceAndActivate(player);
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

        /// <summary>碰撞對象是否為玩家。</summary>
        private static bool IsPlayer(Collider2D other) =>
            other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<Player>() != null;

        /// <summary>在場景中畫出 Boss 廳範圍。</summary>
        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            Gizmos.color = new Color(1f, 0.2f, 0.3f, 0.5f);
            Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
        }
    }
}
