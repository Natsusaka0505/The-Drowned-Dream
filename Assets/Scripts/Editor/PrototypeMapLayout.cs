using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DrownedDream.EditorTools
{
    /// <summary>
    /// 原型佔位地圖：產生 2048×2048 地圖圖 + 碰撞遮罩圖（黑 = 牆），切成 4×4 區塊。
    /// 美術交付正式圖後，直接替換 MapConfig 內的圖片即可，不再使用此佔位。
    ///
    /// 中央 2×2（區塊 1~2 × 1~2）打通成一間 Boss 房；外圍一圈為探索路線（2026-10-04）：
    /// 起點左上 (0,3) → 上排往右 → 從 (3,3) 地板洞掉進右側直井（(3,2)+(3,1) 打通）→ 一路落到 (3,0)
    /// → 下排往左 → (0,0) 爬階梯上 (0,1) → 右側唯一入口進 Boss 房。
    /// 支線：起點 (0,3) 地板洞往下是死路小房間 (0,2)（有階梯回去）。
    /// </summary>
    internal static class PrototypeMapLayout
    {
        /// <summary>佔位地圖圖片路徑。</summary>
        public const string MapPath = "Assets/Art/Map/Map_Placeholder.png";
        /// <summary>佔位碰撞遮罩路徑。</summary>
        public const string MaskPath = "Assets/Art/Map/MapMask_Placeholder.png";

        /// <summary>圖片邊長（像素）。</summary>
        public const int ImageSize = 2048;
        /// <summary>區塊行列數。</summary>
        public const int Grid = 4;
        /// <summary>遮罩取樣格大小（像素）。</summary>
        public const int CellPixels = 16;
        /// <summary>每單位像素數。</summary>
        public const int PixelsPerUnit = 32;
        /// <summary>每區塊邊長（單位）。</summary>
        public const float RoomUnits = ImageSize / (float)Grid / PixelsPerUnit;

        /// <summary>整張圖的格數（每邊）。</summary>
        private const int Cells = ImageSize / CellPixels;
        /// <summary>每區塊的格數（每邊）。</summary>
        private const int RoomCells = Cells / Grid;

        /// <summary>有上下通道的區塊對（上方區塊的 col, 上方 row）。下方區塊會產生階梯。</summary>
        public static readonly Vector2Int[] VerticalLinks =
        {
            new Vector2Int(3, 3), // (3,3) → 右側直井
            new Vector2Int(3, 1), // 右側直井 → (3,0)
            new Vector2Int(0, 1), // (0,0) ↔ (0,1)
            new Vector2Int(0, 3), // 起點 → 支線小房間 (0,2)
        };

        /// <summary>中央 Boss 房（區塊座標 x, y, 寬, 高）。</summary>
        public static readonly RectInt BossArena = new RectInt(1, 1, 2, 2);

        /// <summary>右側直井：這兩個區塊之間的牆打通（下方, 上方）。</summary>
        private static readonly (Vector2Int lower, Vector2Int upper) RightShaft = (new Vector2Int(3, 1), new Vector2Int(3, 2));

        /// <summary>Boss 房地面高度（單位，站立面）。</summary>
        public static float ArenaFloorY => BossArena.y * RoomUnits + 0.5f;
        /// <summary>Boss 房水平中央（單位）。</summary>
        public static float ArenaCenterX => (BossArena.x + BossArena.width / 2f) * RoomUnits;

        /// <summary>區塊是否在 Boss 房內。</summary>
        public static bool InArena(int col, int row) => BossArena.Contains(new Vector2Int(col, row));

        /// <summary>(col-1,row) 與 (col,row) 之間的門是否封住（Boss 房只留左下入口；不讓外圍直接進 Boss 房）。</summary>
        private static bool IsDoorClosed(int col, int row) =>
            (col == 1 && row == 2) || (col == 3 && (row == 1 || row == 2));

        /// <summary>
        /// 浮台位置（世界座標，float1~6 Prefab 的根物件 = 底部中央，頂面再高 0.5）。產生地圖時一併算出，
        /// 由 PrototypeSceneBuilder 放入 float Prefab（單向平台，可從下方跳穿）。浮台不畫在遮罩圖裡。
        /// </summary>
        public static readonly List<Vector2> PlatformSpots = new List<Vector2>();
        /// <summary>每個浮台的長度（單位，與 PlatformSpots 一一對應；隨機但固定種子，每次重建結果相同）。</summary>
        public static readonly List<float> PlatformLengths = new List<float>();
        /// <summary>浮台長度亂數（固定種子）。</summary>
        private static System.Random s_random = new System.Random(20261004);
        /// <summary>浮台寬度（float Prefab：12 格 × 0.5 縮放 = 6 單位）。</summary>
        public const float PlatformWidth = 6f;
        /// <summary>階梯每層高差（單位；跳躍高度約 2.58，浮台為單向不會撞頭）。</summary>
        private const float StepHeight = 2f;

        /// <summary>區塊內局部座標（單位）轉世界座標。</summary>
        public static Vector2 Local(int col, int row, float x, float y) =>
            new Vector2(col * RoomUnits + x, row * RoomUnits + y);

        /// <summary>此區塊是否為上下通道的下方（有階梯）。</summary>
        public static bool HasStairs(int col, int row)
        {
            foreach (var link in VerticalLinks)
            {
                if (link.x == col && link.y - 1 == row) return true;
            }
            return false;
        }

        /// <summary>此區塊是否為上下通道的上方（地板有洞）。</summary>
        public static bool HasHole(int col, int row)
        {
            foreach (var link in VerticalLinks)
            {
                if (link.x == col && link.y == row) return true;
            }
            return false;
        }

        /// <summary>依目前佈局重新產生地圖圖與遮罩圖（佔位圖完全由程式決定，每次重建都覆蓋，佈局修改才會生效）。</summary>
        public static void EnsurePlaceholders()
        {
            EditorBuildUtil.EnsureFolder(Path.GetDirectoryName(MapPath)?.Replace('\\', '/'));

            var walls = BuildWallGrid();
            WriteMask(walls);
            WriteMap(walls);
            AssetDatabase.ImportAsset(MaskPath);
            AssetDatabase.ImportAsset(MapPath);
        }

        /// <summary>依路線產生牆格（true = 牆）。</summary>
        private static bool[,] BuildWallGrid()
        {
            var w = new bool[Cells, Cells];
            PlatformSpots.Clear();
            PlatformLengths.Clear();
            s_random = new System.Random(20261004);

            // 每個區塊四周 1 格牆（相鄰區塊共用邊界 = 2 格厚）
            for (int c = 0; c < Grid; c++)
            {
                for (int r = 0; r < Grid; r++)
                {
                    int ox = c * RoomCells;
                    int oy = r * RoomCells;
                    for (int i = 0; i < RoomCells; i++)
                    {
                        w[ox + i, oy] = true;
                        w[ox + i, oy + RoomCells - 1] = true;
                        w[ox, oy + i] = true;
                        w[ox + RoomCells - 1, oy + i] = true;
                    }
                }
            }

            // 同一列相鄰區塊之間的門（地板上方 4 單位高；Boss 房旁的門封住）
            for (int r = 0; r < Grid; r++)
            {
                for (int c = 1; c < Grid; c++)
                {
                    if (IsDoorClosed(c, r)) continue;
                    int x = c * RoomCells;
                    for (int y = 1; y <= 8; y++)
                    {
                        w[x - 1, r * RoomCells + y] = false;
                        w[x, r * RoomCells + y] = false;
                    }
                }
            }

            // 上下通道：上方區塊地板開洞（往下直接掉到地面），下方區塊在「門的另一側」放之字階梯往上爬。
            // 角色 2×3 格、保守可跳 4 格；洞口正下方不放石台（舊版會卡在洞裡），洞口旁天花板多挖一排給最上層石台頭頂空間。
            foreach (var link in VerticalLinks)
            {
                int ox = link.x * RoomCells;
                int upperY = link.y * RoomCells;
                int lowerY = upperY - RoomCells;
                for (int x = 13; x <= 18; x++)
                {
                    w[ox + x, upperY] = false;      // 上方區塊地板
                    w[ox + x, upperY - 1] = false;  // 下方區塊天花板
                }
                // 下方區塊天花板（雙層邊界的下層）大範圍挖開，最上面幾階浮台不會卡頭；上方區塊地板仍在，只有洞口能穿過
                for (int x = 4; x <= 27; x++) w[ox + x, upperY - 1] = false;

                AddStairs(link.x, link.y - 1, 0);
            }

            BuildRightShaft(w);
            BuildArena(w);

            // 一般區塊：貼地方塊（跳 2 單位上得去）+ 高平台；有洞、有階梯、Boss 房的區塊不放
            for (int c = 0; c < Grid; c++)
            {
                for (int r = 0; r < Grid; r++)
                {
                    if (HasStairs(c, r) || HasHole(c, r) || InArena(c, r)) continue;
                    Fill(w, c * RoomCells + 8, r * RoomCells + 1, 8, 4);  // 貼地方塊（4~8 單位，高 2）
                    AddPlatform(c * RoomCells / 2f + 12f, r * RoomUnits + 4.5f); // 高浮台（9~15 單位，頂面 4.5）
                }
            }

            BuildUpperRoutes(w);
            return w;
        }

        /// <summary>上層路線最上層浮台的頂面高度（單位，區塊內局部）。</summary>
        public const float UpperTop = 12.5f;

        /// <summary>
        /// 上層路線（2026-10-04）：房間上半部加浮台一路疊到頂面 12.5，上層放獎勵；
        /// 一般區塊接在高浮台（頂面 4.5）之後，最上層貼牆，並在 (1,r)–(2,r) 之間的牆上方開門，形成上層通道；
        /// 有地洞的區塊避開中間的洞，從地面之字往上。單向浮台可從下方跳穿，每層高差 2（跳躍約 2.58）。
        /// </summary>
        private static void BuildUpperRoutes(bool[,] w)
        {
            foreach (int r in UpperDoorRows)
            {
                // 一般區塊 (1,r)：最上層靠右牆；(2,r)：最上層靠左牆
                AddUpperStack(1, r, new[] { 4.5f, 11.5f, 4.5f }, 6.5f, 12.5f);
                AddUpperStack(2, r, new[] { 4.5f, 11.5f, 4.5f }, 6.5f, 3.5f);
                // 兩區塊之間的牆在頂面 12.5 ~ 15.5 開門（牆頂本身就是 1 單位寬的走道）
                int x = 2 * RoomCells;
                for (int y = 25; y <= 30; y++)
                {
                    w[x - 1, r * RoomCells + y] = false;
                    w[x, r * RoomCells + y] = false;
                }
            }
            // 有地洞的區塊：從地面之字往上（左右 4.5 / 11.5，中間是洞）
            foreach (var room in HoleRoomRoutes)
            {
                bool startLeft = room == new Vector2Int(0, 1); // (0,1) 右側地面有復活點，第一層改放左邊
                var xs = startLeft ? new[] { 4.5f, 11.5f, 4.5f, 11.5f, 4.5f } : new[] { 11.5f, 4.5f, 11.5f, 4.5f, 11.5f };
                AddUpperStack(room.x, room.y, xs, 2.5f, -1f);
            }
        }

        /// <summary>有上層通道（(1,r)–(2,r) 牆上開門）的列。</summary>
        public static readonly int[] UpperDoorRows = { 0, 3 };

        /// <summary>有地洞、也加上層路線的區塊（直井上方的 (3,3) 與 Boss 房入口 (0,1)、起點 (0,3)）。</summary>
        public static readonly Vector2Int[] HoleRoomRoutes = { new Vector2Int(0, 3), new Vector2Int(3, 3), new Vector2Int(0, 1) };

        /// <summary>
        /// 在區塊 (col,row) 依序放浮台：xs = 各層中心（局部 x），第一層頂面 firstTop，每層 +2；
        /// topX ≥ 0 時再加一層頂面 12.5、長 6 的最上層浮台（貼牆用）。
        /// </summary>
        private static void AddUpperStack(int col, int row, float[] xs, float firstTop, float topX)
        {
            float ox = col * RoomUnits;
            float oy = row * RoomUnits;
            for (int i = 0; i < xs.Length; i++) AddPlatform(ox + xs[i], oy + firstTop + i * StepHeight, 4f, 6f);
            if (topX >= 0f) AddPlatform(ox + topX, oy + UpperTop, 6f, 6f);
        }

        /// <summary>
        /// 在區塊 (col,row) 放之字浮台：中心左右交錯（4.5 / 11.5 單位），頂面從 2.5 起每層 +2；
        /// extraSteps = 額外往下延伸的層數（直井用）。最後一層放在洞口正下方（中心 8），從這裡跳穿洞口回到上方區塊。
        /// </summary>
        private static void AddStairs(int col, int row, int extraSteps)
        {
            float ox = col * RoomUnits;
            float oy = row * RoomUnits - extraSteps * StepHeight;
            int steps = 6 + extraSteps;
            for (int i = 0; i < steps; i++)
            {
                float x = (i % 2 == extraSteps % 2) ? 4.5f : 11.5f;
                AddPlatform(ox + x, oy + 2.5f + i * StepHeight, 4f, 6f);
            }
            AddPlatform(ox + 8f, oy + 2.5f + steps * StepHeight, 4f, 6f); // 洞口正下方（頂面 14.5，上方地板 16.5）
        }

        /// <summary>
        /// 加一個浮台（x = 中心、top = 頂面高度，單位）；長度在 minLength~maxLength 間隨機（0.5 為單位、偶數格，置中不變）。
        /// 階梯用 4~6：兩欄內緣最遠相距 3 單位，跳得過去。
        /// </summary>
        private static void AddPlatform(float x, float top, float minLength = 4f, float maxLength = 7f)
        {
            int minTiles = Mathf.RoundToInt(minLength * 2f) / 2;
            int maxTiles = Mathf.RoundToInt(maxLength * 2f) / 2;
            int pairs = s_random.Next(minTiles, maxTiles + 1); // 以 2 格（1 單位）為一組，保持偶數格、中心不偏
            PlatformSpots.Add(new Vector2(x, top - 0.5f));
            PlatformLengths.Add(pairs);
        }

        /// <summary>
        /// 右側直井：打通上下兩個區塊之間的牆。上方區塊已由 VerticalLinks 放了階梯浮台，
        /// 這裡在下方區塊補 8 層，接到上方區塊第一層（每層高差 2，單向浮台可從下方跳穿）。
        /// </summary>
        private static void BuildRightShaft(bool[,] w)
        {
            int ox = RightShaft.lower.x * RoomCells;
            int boundary = RightShaft.upper.y * RoomCells; // 上方區塊的地板列
            for (int x = 1; x < RoomCells - 1; x++)
            {
                w[ox + x, boundary - 1] = false; // 下方區塊天花板
                w[ox + x, boundary] = false;     // 上方區塊地板
            }

            float sx = RightShaft.lower.x * RoomUnits;
            float sy = RightShaft.lower.y * RoomUnits;
            for (int i = 0; i < 8; i++) // 頂面 2.5 ~ 16.5；上方區塊第一層頂面 18.5（中心 4.5）→ 這裡最後一層放 11.5 交錯
            {
                AddPlatform(sx + (i % 2 == 0 ? 11.5f : 4.5f), sy + 2.5f + i * StepHeight, 4f, 6f);
            }
        }

        /// <summary>
        /// 中央 Boss 房：打通 2×2 區塊內部的牆，只留外框與左下入口；
        /// 左右各一組「貼地方塊 + 石台」讓玩家跳起來躲觸手掃地，中央留給 Boss（放大 2 倍，約 16 單位寬）。
        /// </summary>
        private static void BuildArena(bool[,] w)
        {
            int x0 = BossArena.x * RoomCells;
            int y0 = BossArena.y * RoomCells;
            int x1 = (BossArena.x + BossArena.width) * RoomCells - 1;
            int y1 = (BossArena.y + BossArena.height) * RoomCells - 1;
            for (int x = x0 + 1; x < x1; x++)
            {
                for (int y = y0 + 1; y < y1; y++) w[x, y] = false;
            }

            // 左側（入口那側）：貼地方塊在 22~25 單位（頂面高 2）、浮台在 16.5~22.5 單位（頂面高 4）
            Fill(w, x0 + 12, y0 + 1, 6, 4);
            AddPlatform(x0 / 2f + 3.5f, y0 / 2f + 4.5f);
            // 右側鏡像
            Fill(w, x1 - 17, y0 + 1, 6, 4);
            AddPlatform((x1 + 1) / 2f - 3.5f, y0 / 2f + 4.5f);
        }

        /// <summary>將矩形格設為牆。</summary>
        private static void Fill(bool[,] w, int x, int y, int width, int height)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++) w[x + i, y + j] = true;
            }
        }

        /// <summary>輸出碰撞遮罩：黑 = 牆、白 = 空（浮台另用 Prefab 放置，不在遮罩裡）。</summary>
        private static void WriteMask(bool[,] walls)
        {
            var pixels = new Color32[ImageSize * ImageSize];
            for (int py = 0; py < ImageSize; py++)
            {
                for (int px = 0; px < ImageSize; px++)
                {
                    bool wall = walls[px / CellPixels, py / CellPixels];
                    pixels[py * ImageSize + px] = wall ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);
                }
            }
            SavePng(MaskPath, pixels);
        }

        /// <summary>輸出佔位地圖：只畫岩壁，水域透明（讓後方遠景透出），區塊邊界畫淡格線。</summary>
        private static void WriteMap(bool[,] walls)
        {
            var pixels = new Color32[ImageSize * ImageSize];
            var rock = new Color(0.13f, 0.14f, 0.16f);
            var rockEdge = new Color(0.24f, 0.27f, 0.32f);
            var gridLine = new Color(1f, 1f, 1f, 0.12f);
            int roomPixels = ImageSize / Grid;

            for (int py = 0; py < ImageSize; py++)
            {
                for (int px = 0; px < ImageSize; px++)
                {
                    int cx = px / CellPixels;
                    int cy = py / CellPixels;
                    Color col;
                    if (walls[cx, cy])
                    {
                        bool edge = cy + 1 < Cells && !walls[cx, cy + 1];
                        col = edge && py % CellPixels >= CellPixels - 3 ? rockEdge : rock;
                        float n = Mathf.PerlinNoise(px * 0.05f, py * 0.05f) * 0.06f;
                        col = new Color(col.r + n, col.g + n, col.b + n, 1f);
                    }
                    else
                    {
                        col = px % roomPixels == 0 || py % roomPixels == 0 ? gridLine : Color.clear;
                    }
                    pixels[py * ImageSize + px] = col;
                }
            }
            SavePng(MapPath, pixels);
        }

        /// <summary>將像素存成 PNG。</summary>
        private static void SavePng(string path, Color32[] pixels)
        {
            var tex = new Texture2D(ImageSize, ImageSize, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
