using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 地圖區塊（F-MAP-01/06）。地圖圖片切成 4×4，每格一個 Room；
    /// 以 BoxCollider2D（Trigger）定義邊界，攝影機限制在目前區塊內。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Room : MonoBehaviour
    {
        /// <summary>玩家目前所在的區塊（依進入順序，最後一個為目前區塊）。</summary>
        private static readonly List<Room> s_occupied = new List<Room>();
        /// <summary>場上所有區塊。</summary>
        private static readonly List<Room> s_all = new List<Room>();

        /// <summary>區塊顯示名稱。</summary>
        [SerializeField] private string _displayName = "Room";

        /// <summary>邊界碰撞框。</summary>
        private BoxCollider2D _bounds;

        /// <summary>玩家目前所在區塊。</summary>
        public static Room Current => s_occupied.Count > 0 ? s_occupied[s_occupied.Count - 1] : null;
        /// <summary>目前區塊變更。</summary>
        public static event Action<Room> CurrentChanged;

        /// <summary>區塊顯示名稱。</summary>
        public string DisplayName => _displayName;
        /// <summary>區塊世界座標邊界。</summary>
        public Bounds Bounds => _bounds.bounds;

        /// <summary>玩家進入此區塊。</summary>
        public event Action PlayerEntered;
        /// <summary>玩家離開此區塊。</summary>
        public event Action PlayerExited;

        /// <summary>設定邊界為 Trigger。</summary>
        private void Awake()
        {
            _bounds = GetComponent<BoxCollider2D>();
            _bounds.isTrigger = true;
        }

        /// <summary>加入全區塊清單。</summary>
        private void OnEnable()
        {
            s_all.Add(this);
        }

        /// <summary>停用時移出清單。</summary>
        private void OnDisable()
        {
            s_all.Remove(this);
            s_occupied.Remove(this);
        }

        /// <summary>找出包含某世界座標的區塊（找不到回傳 null）。</summary>
        public static Room FindAt(Vector2 position)
        {
            foreach (var room in s_all)
            {
                var b = room.Bounds;
                if (position.x >= b.min.x && position.x < b.max.x && position.y >= b.min.y && position.y < b.max.y) return room;
            }
            return null;
        }

        /// <summary>玩家進入：設為目前區塊。</summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other) || s_occupied.Contains(this)) return;
            s_occupied.Add(this);
            PlayerEntered?.Invoke();
            CurrentChanged?.Invoke(this);
        }

        /// <summary>玩家離開：切回仍在其中的區塊。</summary>
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other) || !s_occupied.Remove(this)) return;
            PlayerExited?.Invoke();
            if (Current != null) CurrentChanged?.Invoke(Current);
        }

        /// <summary>碰撞對象是否為玩家。</summary>
        private static bool IsPlayer(Collider2D other) =>
            other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<Player>() != null;

        /// <summary>在場景中畫出區塊邊界。</summary>
        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
        }
    }
}
