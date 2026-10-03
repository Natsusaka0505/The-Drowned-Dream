using UnityEngine;

namespace DrownedDream
{
    /// <summary>地形自動貼圖素材（F-MAP-10，SD-02/03）：3×3 地形圖塊 + 天花板 / 側面裝飾 + 背景牆，依碰撞遮罩自動拼出地形。</summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Terrain Tile Set", fileName = "TerrainTileSet")]
    public class TerrainTileSet : ScriptableObject
    {
        [Header("地形 3×3（依序：上左、上中、上右、中左、中中、中右、下左、下中、下右）")]
        /// <summary>3×3 地形圖塊，列優先（索引 = 列 × 3 + 欄，列 0 = 上排）</summary>
        [SerializeField] private Sprite[] _terrain = new Sprite[9];
        /// <summary>3×3 地形圖塊（唯讀）</summary>
        public Sprite[] Terrain => _terrain;

        [Header("背景牆 3×3（洞穴空間後方；上排 = 天花板下的陰影）")]
        /// <summary>背景牆圖塊，列優先（索引 = 列 × 3 + 欄）；依空格四周的牆決定用哪一塊</summary>
        [SerializeField] private Sprite[] _backWall = new Sprite[9];
        /// <summary>背景牆圖塊（唯讀）</summary>
        public Sprite[] BackWall => _backWall;
        /// <summary>背景牆顏色（透明度越低，越看得到後方遠景）</summary>
        [SerializeField] private Color _backWallColor = new Color(1f, 1f, 1f, 0.85f);
        /// <summary>背景牆顏色（唯讀）</summary>
        public Color BackWallColor => _backWallColor;
        /// <summary>背景牆上的裝飾（裂紋等）</summary>
        [SerializeField] private Sprite[] _wallDecor;
        /// <summary>背景牆裝飾（唯讀）</summary>
        public Sprite[] WallDecor => _wallDecor;
        /// <summary>背景牆每格放裝飾的機率</summary>
        [Range(0f, 0.2f)] [SerializeField] private float _wallDecorChance = 0.01f;
        /// <summary>背景牆每格放裝飾的機率（唯讀）</summary>
        public float WallDecorChance => _wallDecorChance;

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

        /// <summary>取得指定列 / 欄的地形圖塊（0~2）。</summary>
        public Sprite GetTerrain(int row, int col) => _terrain != null && _terrain.Length == 9 ? _terrain[row * 3 + col] : null;

        /// <summary>取得指定列 / 欄的背景牆圖塊（0~2）。</summary>
        public Sprite GetBackWall(int row, int col) => _backWall != null && _backWall.Length == 9 ? _backWall[row * 3 + col] : null;
    }
}
