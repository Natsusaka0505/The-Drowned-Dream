using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 憋氣機關範例：憋氣時可穿越的屏障（例如有毒氣泡牆）。
    /// 憋氣結束時若玩家還在屏障內，等玩家離開才恢復實體。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BreathGate : BreathBonusZone
    {
        /// <summary>外觀 Renderer。</summary>
        [SerializeField] private SpriteRenderer _renderer;
        /// <summary>開啟時的透明度。</summary>
        [SerializeField] private float _openAlpha = 0.25f;

        /// <summary>實體碰撞框。</summary>
        private BoxCollider2D _solid;
        /// <summary>等待玩家離開後關閉。</summary>
        private bool _pendingClose;

        /// <summary>快取碰撞框。</summary>
        private void Awake()
        {
            _solid = GetComponent<BoxCollider2D>();
        }

        /// <summary>等玩家離開後關閉屏障。</summary>
        protected override void Update()
        {
            base.Update();
            if (_pendingClose && !PlayerOverlaps()) SetOpen(false);
        }

        /// <summary>開始憋氣：開啟屏障。</summary>
        protected override void OnHoldStarted() => SetOpen(true);

        /// <summary>結束憋氣：玩家不在屏障內就關閉。</summary>
        protected override void OnHoldEnded()
        {
            if (PlayerOverlaps()) _pendingClose = true;
            else SetOpen(false);
        }

        /// <summary>切換屏障開關。</summary>
        private void SetOpen(bool open)
        {
            _pendingClose = false;
            _solid.enabled = !open;
            if (_renderer != null)
            {
                var c = _renderer.color;
                c.a = open ? _openAlpha : 1f;
                _renderer.color = c;
            }
        }

        /// <summary>玩家是否與屏障重疊。</summary>
        private bool PlayerOverlaps()
        {
            if (Player == null) return false;
            var playerCol = Player.GetComponent<Collider2D>();
            // 停用中的 collider bounds 不更新，改用 transform 推算
            var gateBounds = _solid.enabled
                ? _solid.bounds
                : new Bounds(transform.TransformPoint(_solid.offset), Vector3.Scale(_solid.size, transform.lossyScale));
            return gateBounds.Intersects(playerCol.bounds);
        }
    }
}
