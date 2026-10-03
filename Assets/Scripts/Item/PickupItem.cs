using UnityEngine;

namespace DrownedDream
{
    /// <summary>場景道具基底：Trigger 碰撞 + 上下浮動；玩家碰到時由 PlayerPickup 呼叫 Apply。</summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public abstract class PickupItem : MonoBehaviour
    {
        /// <summary>顯示名稱（拾取提示用）。</summary>
        [SerializeField] private string _displayName = "道具";
        /// <summary>上下浮動幅度。</summary>
        [SerializeField] private float _bobHeight = 0.15f;
        /// <summary>上下浮動速度。</summary>
        [SerializeField] private float _bobSpeed = 2f;

        /// <summary>浮動基準位置。</summary>
        private Vector3 _basePos;

        /// <summary>顯示名稱。</summary>
        public string DisplayName => _displayName;

        /// <summary>設定為 Trigger。</summary>
        protected virtual void Awake()
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
        }

        /// <summary>記錄浮動基準。</summary>
        private void Start()
        {
            _basePos = transform.position;
        }

        /// <summary>上下浮動。</summary>
        private void Update()
        {
            transform.position = _basePos + Vector3.up * (Mathf.Sin(Time.time * _bobSpeed) * _bobHeight);
        }

        /// <summary>對玩家套用效果，回傳是否被拾取（false 則留在場上）。</summary>
        public abstract bool Apply(Player player);
    }
}
