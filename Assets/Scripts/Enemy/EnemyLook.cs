using UnityEngine;

namespace DrownedDream
{
    /// <summary>眼球瞳孔：在眼白範圍內朝玩家方向移動（玩家隱形或不在時慢慢回到正中央）。</summary>
    public class EnemyLook : MonoBehaviour
    {
        /// <summary>瞳孔最大偏移（父物件局部座標）。</summary>
        [SerializeField] private float _radius = 0.22f;
        /// <summary>垂直方向偏移比例（眼白是橫向橢圓，上下可移動範圍較小）。</summary>
        [SerializeField] private float _verticalRatio = 0.85f;
        /// <summary>追視平滑速度（越大越快）。</summary>
        [SerializeField] private float _followSpeed = 8f;
        /// <summary>玩家在此距離內時偏移量依距離縮小（貼很近時不會斜到邊）。</summary>
        [SerializeField] private float _fullOffsetDistance = 3f;

        /// <summary>設定最大偏移（建置時用）。</summary>
        public void Init(float radius) => _radius = radius;

        /// <summary>每幀朝玩家方向平滑移動。</summary>
        private void Update()
        {
            Vector2 target = Vector2.zero;
            var player = Player.Instance;
            if (player != null && player.IsVisibleToEnemies && transform.parent != null)
            {
                Vector2 to = player.transform.position - transform.parent.position;
                float k = Mathf.Clamp01(to.magnitude / Mathf.Max(0.01f, _fullOffsetDistance));
                Vector2 dir = to.normalized;
                target = new Vector2(dir.x, dir.y * _verticalRatio) * (_radius * k);
            }
            transform.localPosition = Vector2.Lerp(transform.localPosition, target, 1f - Mathf.Exp(-_followSpeed * Time.deltaTime));
        }
    }
}
