using System.IO;
using UnityEditor;
using UnityEngine;

namespace DrownedDream.EditorTools
{
    /// <summary>
    /// 原型佔位地圖：產生 2048×2048 地圖圖 + 碰撞遮罩圖（黑 = 牆），切成 4×4 區塊。
    /// 美術交付正式圖後，直接替換 MapConfig 內的圖片即可，不再使用此佔位。
    ///
    /// 路線（蛇行）：第 3 列 左→右，往下；第 2 列 右→左，往下；第 1 列 左→右，往下；第 0 列 右→左（Boss 在左下角）。
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
            new Vector2Int(3, 3),
            new Vector2Int(0, 2),
            new Vector2Int(3, 1),
        };

        /// <summary>Boss 房（左下角）：Boss 站在左側地面，地形另外設計。</summary>
        public static readonly Vector2Int BossRoom = new Vector2Int(0, 0);

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

            // 同一列相鄰區塊之間的門（地板上方 4 單位高）
            for (int r = 0; r < Grid; r++)
            {
                for (int c = 1; c < Grid; c++)
                {
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
                // 階梯放在門的另一側：第 0 欄只有右門 → 階梯在左；其餘（目前是第 3 欄，只有左門）→ 階梯在右
                bool stairsRight = link.x != 0;
                int X(int x) => stairsRight ? x : RoomCells - 1 - x; // 以「階梯在右」設計，左側時鏡像

                for (int x = 13; x <= 18; x++)
                {
                    w[ox + x, upperY] = false;      // 上方區塊地板
                    w[ox + x, upperY - 1] = false;  // 下方區塊天花板
                }
                for (int x = 13; x <= 24; x++) w[ox + X(x), upperY - 1] = false; // 最上層石台的頭頂空間

                FillLocal(w, ox, lowerY, X, 26, 30, 1, 4);   // 貼地方塊（不用從石台底下鑽過）
                FillLocal(w, ox, lowerY, X, 20, 24, 8, 8);
                FillLocal(w, ox, lowerY, X, 26, 30, 12, 12);
                FillLocal(w, ox, lowerY, X, 20, 24, 16, 16);
                FillLocal(w, ox, lowerY, X, 26, 30, 20, 20);
                FillLocal(w, ox, lowerY, X, 20, 24, 24, 24);
                FillLocal(w, ox, lowerY, X, 17, 18, 28, 28); // 洞口下方偏一側的小石台：從這裡跳回上方區塊
            }

            // 一般區塊：貼地方塊（跳 2 單位上得去）+ 高平台；有洞或有階梯的區塊不放
            for (int c = 0; c < Grid; c++)
            {
                for (int r = 0; r < Grid; r++)
                {
                    if (HasStairs(c, r) || HasHole(c, r)) continue;
                    if (c == BossRoom.x && r == BossRoom.y)
                    {
                        // Boss 房：左側留給 Boss 站地面；方塊在右側門口內（12~14），高平台在祭壇上方（9~12）躲子彈
                        Fill(w, c * RoomCells + 24, r * RoomCells + 1, 4, 4);
                        Fill(w, c * RoomCells + 18, r * RoomCells + 8, 6, 1);
                        continue;
                    }
                    Fill(w, c * RoomCells + 8, r * RoomCells + 1, 8, 4);
                    Fill(w, c * RoomCells + 18, r * RoomCells + 8, 8, 1);
                }
            }
            return w;
        }

        /// <summary>將區塊內局部格範圍（含頭尾，x 經過鏡像函式）設為牆。</summary>
        private static void FillLocal(bool[,] w, int ox, int oy, System.Func<int, int> mapX, int x0, int x1, int y0, int y1)
        {
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++) w[ox + mapX(x), oy + y] = true;
            }
        }

        /// <summary>將矩形格設為牆。</summary>
        private static void Fill(bool[,] w, int x, int y, int width, int height)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++) w[x + i, y + j] = true;
            }
        }

        /// <summary>輸出碰撞遮罩：黑 = 牆、白 = 空。</summary>
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
