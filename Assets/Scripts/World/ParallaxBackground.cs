using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 遠景背景（F-MAP-09）：放在地圖後方，不參與碰撞；跟隨攝影機做視差移動，並自動縮放到永遠蓋滿畫面。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxBackground : MonoBehaviour
    {
        /// <summary>地圖世界範圍左下角。</summary>
        [SerializeField] private Vector2 _mapMin = Vector2.zero;
        /// <summary>地圖世界範圍大小。</summary>
        [SerializeField] private Vector2 _mapSize = new Vector2(64f, 64f);
        /// <summary>跟隨攝影機的比例（1 = 完全跟著畫面不動，0 = 固定在世界上）。</summary>
        [Range(0f, 1f)]
        [SerializeField] private float _follow = 0.9f;
        /// <summary>縮放安全邊距倍率（避免畫面邊緣露出）。</summary>
        [SerializeField] private float _margin = 1.05f;

        /// <summary>目標攝影機。</summary>
        private Camera _camera;
        /// <summary>背景 Renderer。</summary>
        private SpriteRenderer _renderer;

        /// <summary>地圖中心點。</summary>
        private Vector2 MapCenter => _mapMin + _mapSize * 0.5f;

        /// <summary>快取元件。</summary>
        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>找主攝影機。</summary>
        private void Start()
        {
            _camera = Camera.main;
        }

        /// <summary>依攝影機位置做視差移動，並依畫面大小調整縮放。</summary>
        private void LateUpdate()
        {
            if (_camera == null || _renderer.sprite == null) return;

            Vector2 camPos = _camera.transform.position;
            Vector2 pos = MapCenter + (camPos - MapCenter) * _follow;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

            FitToCover();
        }

        /// <summary>縮放到能蓋滿「畫面 + 視差最大位移」的範圍。</summary>
        private void FitToCover()
        {
            float viewH = _camera.orthographicSize * 2f;
            float viewW = viewH * _camera.aspect;
            float needW = viewW + _mapSize.x * (1f - _follow);
            float needH = viewH + _mapSize.y * (1f - _follow);

            Vector2 spriteSize = _renderer.sprite.bounds.size;
            float scale = Mathf.Max(needW / spriteSize.x, needH / spriteSize.y) * _margin;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
