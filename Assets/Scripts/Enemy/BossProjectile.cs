using UnityEngine;

namespace DrownedDream
{
    /// <summary>Boss 彈幕。碰到玩家扣 HP，碰到地形消失。</summary>
    public class BossProjectile : MonoBehaviour
    {
        /// <summary>命中判定半徑。</summary>
        private const float HitRadius = 0.4f;

        /// <summary>移動速度向量。</summary>
        private Vector2 _velocity;
        /// <summary>傷害。</summary>
        private float _damage;
        /// <summary>剩餘存活秒數。</summary>
        private float _lifetime;
        /// <summary>會擋下彈幕的 Layer。</summary>
        private LayerMask _blockMask;

        /// <summary>執行期產生一顆彈幕。</summary>
        public static BossProjectile Spawn(Sprite sprite, Vector2 position, Vector2 velocity, float damage, float lifetime, LayerMask blockMask)
        {
            var go = new GameObject("BossProjectile");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.5f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.8f, 0.2f, 1f);
            sr.sortingOrder = 20;
            var p = go.AddComponent<BossProjectile>();
            p._velocity = velocity;
            p._damage = damage;
            p._lifetime = lifetime;
            p._blockMask = blockMask;
            return p;
        }

        /// <summary>移動、檢查撞牆與命中玩家。</summary>
        private void Update()
        {
            float dt = Time.deltaTime;
            _lifetime -= dt;
            Vector2 pos = transform.position;
            Vector2 step = _velocity * dt;

            if (_lifetime <= 0f || Physics2D.Raycast(pos, step.normalized, step.magnitude, _blockMask).collider != null)
            {
                Destroy(gameObject);
                return;
            }
            transform.position = pos + step;

            var player = Player.Instance;
            if (player != null && player.IsVisibleToEnemies &&
                Vector2.Distance(player.transform.position, transform.position) <= HitRadius + 0.5f)
            {
                if (player.Status.TakeHit(_damage)) Destroy(gameObject);
            }
        }
    }
}
