using UnityEngine;

namespace DrownedDream
{
    /// <summary>存檔 / 復活點（F-DTH-01）。[待確認] 復活點形式，原型為碰觸物件更新。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        /// <summary>外觀 Renderer（啟用時變色）。</summary>
        [SerializeField] private SpriteRenderer _renderer;
        /// <summary>啟用中的顏色。</summary>
        [SerializeField] private Color _activeColor = new Color(0.4f, 1f, 0.8f);

        /// <summary>目前啟用的存檔點。</summary>
        private static Checkpoint s_current;
        /// <summary>未啟用時的顏色。</summary>
        private Color _inactiveColor;

        /// <summary>設定為 Trigger 並記錄原色。</summary>
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (_renderer != null) _inactiveColor = _renderer.color;
        }

        /// <summary>玩家碰觸：更新復活點。</summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (s_current == this || other.attachedRigidbody == null) return;
            var respawn = other.attachedRigidbody.GetComponent<PlayerRespawn>();
            if (respawn == null) return;

            if (s_current != null && s_current._renderer != null) s_current._renderer.color = s_current._inactiveColor;
            s_current = this;
            if (_renderer != null) _renderer.color = _activeColor;
            respawn.SetRespawnPoint(transform.position);
            GameEvents.ShowMessage("記憶在此刻下印記（復活點）", 1.5f);
        }

        /// <summary>銷毀時清除目前存檔點。</summary>
        private void OnDestroy()
        {
            if (s_current == this) s_current = null;
        }
    }
}
