using UnityEngine;

namespace DrownedDream
{
    /// <summary>玩家入口：集中 Status 與各 Action 的參考，供其他系統查詢。</summary>
    [RequireComponent(typeof(PlayerStatus), typeof(PlayerInputReader), typeof(PlayerMove))]
    [RequireComponent(typeof(PlayerAttack), typeof(PlayerBreath), typeof(PlayerPickup))]
    [RequireComponent(typeof(PlayerEnding), typeof(PlayerRespawn), typeof(PlayerConfusion))]
    public class Player : MonoBehaviour
    {
        /// <summary>場景中唯一的玩家。</summary>
        public static Player Instance { get; private set; }

        /// <summary>玩家數值（Status）。</summary>
        public PlayerStatus Status { get; private set; }
        /// <summary>輸入讀取。</summary>
        public PlayerInputReader Input { get; private set; }
        /// <summary>Action：左右移動 + 跳躍。</summary>
        public PlayerMove Move { get; private set; }
        /// <summary>Action：投擲 / 撿魚叉。</summary>
        public PlayerAttack Attack { get; private set; }
        /// <summary>Action：憋氣潛行。</summary>
        public PlayerBreath Breath { get; private set; }
        /// <summary>Action：撿道具。</summary>
        public PlayerPickup Pickup { get; private set; }
        /// <summary>Action：是否觸發結局。</summary>
        public PlayerEnding Ending { get; private set; }
        /// <summary>死亡復活。</summary>
        public PlayerRespawn Respawn { get; private set; }
        /// <summary>低 SAN 方向錯亂。</summary>
        public PlayerConfusion Confusion { get; private set; }

        /// <summary>敵人是否看得到玩家（憋氣中或死亡時看不到，F-BRE-04）。</summary>
        public bool IsVisibleToEnemies => !Status.IsHoldingBreath && Status.IsAlive;

        /// <summary>註冊單例並快取各元件。</summary>
        private void Awake()
        {
            Instance = this;
            Status = GetComponent<PlayerStatus>();
            Input = GetComponent<PlayerInputReader>();
            Move = GetComponent<PlayerMove>();
            Attack = GetComponent<PlayerAttack>();
            Breath = GetComponent<PlayerBreath>();
            Pickup = GetComponent<PlayerPickup>();
            Ending = GetComponent<PlayerEnding>();
            Respawn = GetComponent<PlayerRespawn>();
            Confusion = GetComponent<PlayerConfusion>();
        }

        /// <summary>清除單例。</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
