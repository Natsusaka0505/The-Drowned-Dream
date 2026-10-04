using UnityEngine;

namespace DrownedDream
{
    /// <summary>地形自動貼圖素材（F-MAP，SD-02 A 方案）：3×3 地形圖塊 + 天花板 / 側面裝飾，依碰撞遮罩自動拼出地形。</summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Terrain Tile Set", fileName = "TerrainTileSet")]
    public class TerrainTileSet : ScriptableObject
    {
        [Header("地形 3×3（依序：上左、上中、上右、中左、中中、中右、下左、下中、下右）")]
        /// <summary>3×3 地形圖塊，列優先（索引 = 列 × 3 + 欄，列 0 = 上排）</summary>
        [SerializeField] private Sprite[] _terrain = new Sprite[9];
        /// <summary>3×3 地形圖塊（唯讀）</summary>
        public Sprite[] Terrain => _terrain;

        [Header("裝飾")]
        /// <summary>天花板垂吊裝飾（頂端對齊天花板，pivot 上緣）</summary>
        [SerializeField] private Sprite[] _ceilingDecor;
        /// <summary>天花板垂吊裝飾（唯讀）</summary>
        public Sprite[] CeilingDecor => _ceilingDecor;
        /// <summary>牆面朝左（往左突出）的裝飾（pivot 右緣）</summary>
        [SerializeField] private Sprite[] _leftDecor;
        /// <summary>牆面朝左的裝飾（唯讀）</summary>
        public Sprite[] LeftDecor => _leftDecor;
        /// <summary>牆面朝右（往右突出）的裝飾（pivot 左緣）</summary>
        [SerializeField] private Sprite[] _rightDecor;
        /// <summary>牆面朝右的裝飾（唯讀）</summary>
        public Sprite[] RightDecor => _rightDecor;
        /// <summary>天花板每格放裝飾的機率</summary>
        [Range(0f, 1f)] [SerializeField] private float _ceilingChance = 0.2f;
        /// <summary>天花板每格放裝飾的機率（唯讀）</summary>
        public float CeilingChance => _ceilingChance;
        /// <summary>牆面每格放裝飾的機率</summary>
        [Range(0f, 1f)] [SerializeField] private float _sideChance = 0.25f;
        /// <summary>牆面每格放裝飾的機率（唯讀）</summary>
        public float SideChance => _sideChance;
        /// <summary>裝飾縮放倍率（相對地形圖塊比例）</summary>
        [SerializeField] private float _decorScale = 1.5f;
        /// <summary>裝飾縮放倍率（唯讀）</summary>
        public float DecorScale => _decorScale;
        /// <summary>裝飾亂數種子（同種子 → 每次重建結果相同）</summary>
        [SerializeField] private int _seed = 12345;
        /// <summary>裝飾亂數種子（唯讀）</summary>
        public int Seed => _seed;

        [Header("牆面石塊（newWall 美術，沿牆自動鋪設）")]
        /// <summary>水平牆（地板 / 天花板）用的橫向石條</summary>
        [SerializeField] private Sprite[] _wallStonesH;
        /// <summary>水平牆用的橫向石條（唯讀）</summary>
        public Sprite[] WallStonesH => _wallStonesH;
        /// <summary>垂直牆用的直向石柱</summary>
        [SerializeField] private Sprite[] _wallStonesV;
        /// <summary>垂直牆用的直向石柱（唯讀）</summary>
        public Sprite[] WallStonesV => _wallStonesV;
        /// <summary>貼地方塊等短而厚的凸塊用的大石塊</summary>
        [SerializeField] private Sprite[] _wallRocks;
        /// <summary>凸塊用的大石塊（唯讀）</summary>
        public Sprite[] WallRocks => _wallRocks;
        /// <summary>石條 / 石柱的厚度（單位；牆厚 1，略大一點蓋住接縫）</summary>
        [SerializeField] private float _wallThickness = 1.15f;
        /// <summary>石條 / 石柱的厚度（唯讀）</summary>
        public float WallThickness => _wallThickness;
        /// <summary>相鄰石塊重疊比例（0 = 剛好相接）</summary>
        [Range(0f, 0.5f)] [SerializeField] private float _wallOverlap = 0.1f;
        /// <summary>相鄰石塊重疊比例（唯讀）</summary>
        public float WallOverlap => _wallOverlap;

        /// <summary>取得指定列 / 欄的地形圖塊（0~2）。</summary>
        public Sprite GetTerrain(int row, int col) => _terrain != null && _terrain.Length == 9 ? _terrain[row * 3 + col] : null;
    }
}
