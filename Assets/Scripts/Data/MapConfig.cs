using UnityEngine;

namespace DrownedDream
{
    /// <summary>地圖設定（F-MAP）：一張 2048×2048 地圖圖片 + 同尺寸碰撞遮罩，切成 4×4 區塊。</summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Map Config", fileName = "MapConfig")]
    public class MapConfig : ScriptableObject
    {
        [Header("圖片")]
        /// <summary>地圖美術圖（背景，2048×2048）</summary>
        [SerializeField] private Texture2D _mapTexture;
        /// <summary>地圖美術圖（背景，2048×2048）（唯讀）</summary>
        public Texture2D MapTexture => _mapTexture;
        /// <summary>碰撞遮罩圖（同尺寸；黑色 = 牆 / 地板）</summary>
        [SerializeField] private Texture2D _collisionMask;
        /// <summary>碰撞遮罩圖（同尺寸；黑色 = 牆 / 地板）（唯讀）</summary>
        public Texture2D CollisionMask => _collisionMask;

        [Header("切分")]
        /// <summary>橫向區塊數</summary>
        [SerializeField] private int _columns = 4;
        /// <summary>橫向區塊數（唯讀）</summary>
        public int Columns => _columns;
        /// <summary>縱向區塊數</summary>
        [SerializeField] private int _rows = 4;
        /// <summary>縱向區塊數（唯讀）</summary>
        public int Rows => _rows;
        /// <summary>每單位像素數（512px 區塊 = 16 單位）</summary>
        [SerializeField] private int _pixelsPerUnit = 32;
        /// <summary>每單位像素數（512px 區塊 = 16 單位）（唯讀）</summary>
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
