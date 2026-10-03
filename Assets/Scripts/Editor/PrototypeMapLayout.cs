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

        /// <summary>區塊內局部座標（單位）轉世界座標。</summary>
        public static Vector2 Local(int col, int row, float x, float y) =>
            new Vector2(col * RoomUnits + x, row * RoomUnits + y);

        /// <summary>此區塊下方是否接著有階梯的區塊（即本區塊是上下通道的下方）。</summary>
        public static bool HasStairs(int col, int row)
        {
            foreach (var link in VerticalLinks)
            {
                if (link.x == col && link.y - 1 == row) return true;
            }
            return false;
        }

        /// <summary>佔位圖不存在時產生地圖圖與遮罩圖。</summary>
        public static void EnsurePlaceholders()
        {
            if (File.Exists(MapPath) && File.Exists(MaskPath)) return;
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

            // 上下通道：地洞 + 下方區塊的之字階梯
            foreach (var link in VerticalLinks)
            {
                int ox = link.x * RoomCells;
                int upperY = link.y * RoomCells;
                for (int x = 13; x <= 17; x++)
                {
                    w[ox + x, upperY] = false;
                    w[ox + x, upperY - 1] = false;
                }

                int lowerY = (link.y - 1) * RoomCells;
                int[] tops = { 4, 8, 12, 16, 20, 24 };
                for (int i = 0; i < tops.Length; i++)
                {
                    bool left = i % 2 == 0;
                    Fill(w, ox + (left ? 5 : 19), lowerY + tops[i], 8, 1);
                }
                Fill(w, ox + 14, lowerY + 28, 3, 1);
            }

            // 沒有階梯的區塊加兩塊平台
            for (int c = 0; c < Grid; c++)
            {
                for (int r = 0; r < Grid; r++)
                {
                    if (HasStairs(c, r)) continue;
                    Fill(w, c * RoomCells + 8, r * RoomCells + 5, 8, 1);
                    Fill(w, c * RoomCells + 18, r * RoomCells + 9, 8, 1);
                }
            }
            return w;
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
