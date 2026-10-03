using UnityEngine;

namespace DrownedDream
{
    /// <summary>Action：撿道具（F-INV-02）。碰到 PickupItem（回復道具、封印道具）即拾取。</summary>
    public class PlayerPickup : MonoBehaviour
    {
        /// <summary>玩家入口（傳給道具套用效果）。</summary>
        private Player _player;

        /// <summary>快取玩家。</summary>
        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        /// <summary>碰到道具時拾取。</summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            var item = other.GetComponent<PickupItem>();
            if (item != null) TryPickUp(item);
        }

        /// <summary>拾取道具：活著才撿，道具接受後銷毀。</summary>
        public bool TryPickUp(PickupItem item)
        {
            if (!_player.Status.IsAlive || item == null) return false;
            if (!item.Apply(_player)) return false;
            Destroy(item.gameObject);
            return true;
        }
    }
}
