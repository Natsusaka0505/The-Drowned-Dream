using UnityEngine;

namespace DrownedDream
{
    /// <summary>地圖設定（F-MAP）：碰撞遮罩（由洞窟生成器產生）+ 地形素材 + 遠景；不分區塊。</summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Map Config", fileName = "MapConfig")]
    public class MapConfig : ScriptableObject
    {
        [Header("圖片")]
        /// <summary>（選用）整張地圖美術圖；有設定地形素材時不使用</summary>
        [SerializeField] private Texture2D _mapTexture;
        /// <summary>（選用）整張地圖美術圖（唯讀）</summary>
        public Texture2D MapTexture => _mapTexture;
        /// <summary>碰撞遮罩圖（黑色 = 牆 / 地板）</summary>
        [SerializeField] private Texture2D _collisionMask;
        /// <summary>碰撞遮罩圖（黑色 = 牆 / 地板）（唯讀）</summary>
        public Texture2D CollisionMask => _collisionMask;

        /// <summary>地形自動貼圖素材（留空 = 不貼地形，只顯示地圖美術圖）</summary>
        [SerializeField] private TerrainTileSet _terrain;
        /// <summary>地形自動貼圖素材（唯讀）</summary>
        public TerrainTileSet Terrain => _terrain;

        /// <summary>遠景背景圖（不碰撞，視差捲動）</summary>
        [SerializeField] private Texture2D _farBackground;
        /// <summary>遠景背景圖（不碰撞，視差捲動）（唯讀）</summary>
        public Texture2D FarBackground => _farBackground;
        /// <summary>遠景跟隨攝影機的比例（1 = 跟著畫面不動，0 = 固定在世界）</summary>
        [Range(0f, 1f)]
        [SerializeField] private float _parallaxFollow = 0.9f;
        /// <summary>遠景跟隨攝影機的比例（唯讀）</summary>
        public float ParallaxFollow => _parallaxFollow;

        [Header("尺寸")]
        /// <summary>每單位像素數（32 → 遮罩 16px 一格 = 0.5 單位）</summary>
        [SerializeField] private int _pixelsPerUnit = 32;
        /// <summary>每單位像素數（32 → 遮罩 16px 一格 = 0.5 單位）（唯讀）</summary>
        public int PixelsPerUnit => _pixelsPerUnit;

        [Header("碰撞")]
        /// <summary>遮罩取樣格大小（像素），即碰撞 Tile 大小</summary>
        [SerializeField] private int _maskCellPixels = 16;
        /// <summary>遮罩取樣格大小（像素），即碰撞 Tile 大小（唯讀）</summary>
        public int MaskCellPixels => _maskCellPixels;
        /// <summary>取樣亮度低於此值視為牆</summary>
        [Range(0f, 1f)] [SerializeField] private float _wallThreshold = 0.5f;
        /// <summary>取樣亮度低於此值視為牆（唯讀）</summary>
        public float WallThreshold => _wallThreshold;
    }
}
