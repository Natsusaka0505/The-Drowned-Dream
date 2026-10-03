using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 地圖設定（F-MAP）：一張地圖美術圖 + 碰撞（遮罩圖或美術圖透明度），切成 Columns × Rows 區塊；
    /// 關卡內容可由關卡 Prefab 提供。
    /// </summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Map Config", fileName = "MapConfig")]
    public class MapConfig : ScriptableObject
    {
        [Header("圖片")]
        /// <summary>地圖美術圖（2048×2048；可通行處需透明，才看得到遠景）</summary>
        [SerializeField] private Texture2D _mapTexture;
        /// <summary>地圖美術圖（2048×2048；可通行處需透明，才看得到遠景）（唯讀）</summary>
        public Texture2D MapTexture => _mapTexture;
        /// <summary>碰撞遮罩圖（同尺寸；黑色 = 牆 / 地板）</summary>
        [SerializeField] private Texture2D _collisionMask;
        /// <summary>碰撞遮罩圖（同尺寸；黑色 = 牆 / 地板）（唯讀）</summary>
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
        /// <summary>遮罩取樣亮度低於此值為牆；介於此值與 WallThreshold 之間為單向平台（深灰，可從下方跳穿）</summary>
        [Range(0f, 1f)] [SerializeField] private float _platformThreshold = 0.2f;
        /// <summary>單向平台亮度下限（唯讀）</summary>
        public float PlatformThreshold => _platformThreshold;
        /// <summary>改用地圖美術圖的透明度產生碰撞（不透明 = 牆；勾選時忽略碰撞遮罩圖）</summary>
        [SerializeField] private bool _collisionFromMapAlpha;
        /// <summary>改用地圖美術圖的透明度產生碰撞（唯讀）</summary>
        public bool CollisionFromMapAlpha => _collisionFromMapAlpha;
        /// <summary>透明度高於此值視為牆</summary>
        [Range(0f, 1f)] [SerializeField] private float _alphaThreshold = 0.5f;
        /// <summary>透明度高於此值視為牆（唯讀）</summary>
        public float AlphaThreshold => _alphaThreshold;

        [Header("關卡內容")]
        /// <summary>關卡 Prefab（平台 / 怪物 / 寶箱 / 存檔點 / Boss / 玩家起點；留空 = 用原型程式配置）</summary>
        [SerializeField] private GameObject _levelPrefab;
        /// <summary>關卡 Prefab（唯讀）</summary>
        public GameObject LevelPrefab => _levelPrefab;
    }
}
