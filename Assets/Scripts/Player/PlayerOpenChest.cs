using UnityEngine;

namespace DrownedDream
{
    /// <summary>Action：開寶箱（F-INV-15）。靠近未打開的寶箱時提示，按互動鍵打開。</summary>
    public class PlayerOpenChest : MonoBehaviour
    {
        /// <summary>靠近寶箱時的提示文字。</summary>
        [SerializeField] private string _prompt = "按 E 打開寶箱";

        /// <summary>玩家入口（傳給寶箱，動畫播完把道具交給玩家）。</summary>
        private Player _player;
        /// <summary>玩家數值（死亡時不能開）。</summary>
        private PlayerStatus _status;
        /// <summary>輸入來源。</summary>
        private PlayerInputReader _input;
        /// <summary>上一次提示過的寶箱（離開再靠近才重新提示）。</summary>
        private TreasureChest _prompted;

        /// <summary>快取元件。</summary>
        private void Awake()
        {
            _player = GetComponent<Player>();
            _status = GetComponent<PlayerStatus>();
            _input = GetComponent<PlayerInputReader>();
        }

        /// <summary>找範圍內的寶箱，提示並在按互動鍵時打開。</summary>
        private void Update()
        {
            var chest = _status.IsAlive ? TreasureChest.FindInRange(transform.position) : null;
            if (chest == null)
            {
                _prompted = null;
                return;
            }

            if (chest != _prompted)
            {
                _prompted = chest;
                GameEvents.ShowMessage(_prompt, 1.5f);
            }

            if (_input.InteractPressed) chest.Open(_player);
        }
    }
}
