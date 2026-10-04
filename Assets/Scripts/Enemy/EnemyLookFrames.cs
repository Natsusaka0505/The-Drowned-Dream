using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 整張圖追視：依玩家所在方向切換畫格（0 正視、1 右上、2 左上、3 左下、4 右下）。
    /// 玩家隱形、不在偵測範圍或貼太近時看正前方。用於眼花（深淵之眼 2026-10-04 新圖）。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyLookFrames : MonoBehaviour
    {
        /// <summary>畫格：正視、右上、左上、左下、右下。</summary>
        [SerializeField] private Sprite[] _frames;
        /// <summary>玩家在此距離內看正前方（避免貼身時一直亂切）。</summary>
        [SerializeField] private float _centerDistance = 0.8f;
        /// <summary>玩家超過此距離就不再盯著看（0 = 不限）。</summary>
        [SerializeField] private float _maxDistance = 12f;
        /// <summary>切換方向後至少停留秒數（避免在象限邊界抖動）。</summary>
        [SerializeField] private float _minHoldSeconds = 0.15f;

        /// <summary>顯示用 Renderer。</summary>
        private SpriteRenderer _renderer;
        /// <summary>目前畫格索引。</summary>
        private int _index;
        /// <summary>目前畫格已停留秒數。</summary>
        private float _holdTimer;

        /// <summary>建置時設定畫格。</summary>
        public void Init(Sprite[] frames) => _frames = frames;

        /// <summary>快取 Renderer 並顯示正視畫格。</summary>
        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_frames != null && _frames.Length > 0) _renderer.sprite = _frames[0];
        }

        /// <summary>每幀依玩家方向挑選畫格。</summary>
        private void Update()
        {
            if (_frames == null || _frames.Length < 5) return;
            _holdTimer += Time.deltaTime;
            int next = PickFrame();
            if (next == _index || _holdTimer < _minHoldSeconds) return;
            _index = next;
            _holdTimer = 0f;
            _renderer.sprite = _frames[_index];
        }

        /// <summary>依玩家相對位置回傳畫格索引。</summary>
        private int PickFrame()
        {
            var player = Player.Instance;
            if (player == null || !player.IsVisibleToEnemies) return 0;
            Vector2 to = player.transform.position - transform.position;
            float dist = to.magnitude;
            if (dist < _centerDistance || (_maxDistance > 0f && dist > _maxDistance)) return 0;
            bool right = to.x >= 0f;
            bool up = to.y >= 0f;
            if (up) return right ? 1 : 2;
            return right ? 4 : 3;
        }
    }
}
