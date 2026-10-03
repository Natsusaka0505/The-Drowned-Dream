using UnityEngine;

namespace DrownedDream
{
    /// <summary>隨機洞窟生成參數（F-MAP-01/11，SD-03）。同一個種子 → 同一張地圖；長度單位皆為「單位」（1 單位 = 32 px）。</summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Cave Gen Config", fileName = "CaveGenConfig")]
    public class CaveGenConfig : ScriptableObject
    {
        [Header("地圖")]
        /// <summary>亂數種子（換種子 = 換一張地圖）</summary>
        [SerializeField] private int _seed = 20261003;
        /// <summary>亂數種子（唯讀）</summary>
        public int Seed => _seed;
        /// <summary>地圖寬（單位）</summary>
        [SerializeField] private int _widthUnits = 128;
        /// <summary>地圖寬（唯讀）</summary>
        public int WidthUnits => _widthUnits;
        /// <summary>地圖高（單位）</summary>
        [SerializeField] private int _heightUnits = 96;
        /// <summary>地圖高（唯讀）</summary>
        public int HeightUnits => _heightUnits;
        /// <summary>初始填牆比例（越高洞越少越窄）</summary>
        [Range(0.3f, 0.65f)] [SerializeField] private float _fillPercent = 0.5f;
        /// <summary>初始填牆比例（唯讀）</summary>
        public float FillPercent => _fillPercent;
        /// <summary>細胞自動機平滑次數（越多洞越圓滑）</summary>
        [SerializeField] private int _smoothIterations = 5;
        /// <summary>細胞自動機平滑次數（唯讀）</summary>
        public int SmoothIterations => _smoothIterations;

        [Header("洞廳（單位）")]
        /// <summary>起點洞廳大小</summary>
        [SerializeField] private Vector2 _startRoomSize = new Vector2(12f, 6f);
        /// <summary>起點洞廳大小（唯讀）</summary>
        public Vector2 StartRoomSize => _startRoomSize;
        /// <summary>Boss 廳大小</summary>
        [SerializeField] private Vector2 _bossRoomSize = new Vector2(22f, 11f);
        /// <summary>Boss 廳大小（唯讀）</summary>
        public Vector2 BossRoomSize => _bossRoomSize;
        /// <summary>Boss 入口隧道高度</summary>
        [SerializeField] private float _tunnelHeight = 3.5f;
        /// <summary>Boss 入口隧道高度（唯讀）</summary>
        public float TunnelHeight => _tunnelHeight;

        [Header("浮台（單位）")]
        /// <summary>同一排浮台最小間隔（越大浮台越稀疏，不夠的地方生成器會自動補）</summary>
        [SerializeField] private float _ledgeGapMin = 2.5f;
        /// <summary>同一排浮台最小間隔（唯讀）</summary>
        public float LedgeGapMin => _ledgeGapMin;
        /// <summary>同一排浮台最大間隔</summary>
        [SerializeField] private float _ledgeGapMax = 5.5f;
        /// <summary>同一排浮台最大間隔（唯讀）</summary>
        public float LedgeGapMax => _ledgeGapMax;

        [Header("物件數量")]
        /// <summary>存檔點數（含起點與 Boss 入口外）</summary>
        [SerializeField] private int _checkpoints = 6;
        /// <summary>存檔點數（唯讀）</summary>
        public int Checkpoints => _checkpoints;
        /// <summary>封印碎片數（也是封印所需數量）</summary>
        [SerializeField] private int _seals = 3;
        /// <summary>封印碎片數（唯讀）</summary>
        public int Seals => _seals;
        /// <summary>鎮靜藥丸數</summary>
        [SerializeField] private int _pills = 6;
        /// <summary>鎮靜藥丸數（唯讀）</summary>
        public int Pills => _pills;
        /// <summary>海草繃帶數</summary>
        [SerializeField] private int _medkits = 6;
        /// <summary>海草繃帶數（唯讀）</summary>
        public int Medkits => _medkits;
        /// <summary>巡游魚怪數</summary>
        [SerializeField] private int _fish = 10;
        /// <summary>巡游魚怪數（唯讀）</summary>
        public int Fish => _fish;
        /// <summary>觸手數</summary>
        [SerializeField] private int _tentacles = 6;
        /// <summary>觸手數（唯讀）</summary>
        public int Tentacles => _tentacles;
        /// <summary>深淵之眼數</summary>
        [SerializeField] private int _eyes = 6;
        /// <summary>深淵之眼數（唯讀）</summary>
        public int Eyes => _eyes;
        /// <summary>幻覺物件數</summary>
        [SerializeField] private int _hallucinations = 4;
        /// <summary>幻覺物件數（唯讀）</summary>
        public int Hallucinations => _hallucinations;

        [Header("間距（單位）")]
        /// <summary>道具之間最小距離</summary>
        [SerializeField] private float _itemSpacing = 8f;
        /// <summary>道具之間最小距離（唯讀）</summary>
        public float ItemSpacing => _itemSpacing;
        /// <summary>敵人之間最小距離</summary>
        [SerializeField] private float _enemySpacing = 9f;
        /// <summary>敵人之間最小距離（唯讀）</summary>
        public float EnemySpacing => _enemySpacing;
        /// <summary>敵人離起點最小距離</summary>
        [SerializeField] private float _enemySafeRadius = 14f;
        /// <summary>敵人離起點最小距離（唯讀）</summary>
        public float EnemySafeRadius => _enemySafeRadius;
    }
}
