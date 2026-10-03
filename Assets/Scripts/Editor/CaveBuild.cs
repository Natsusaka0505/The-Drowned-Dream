using System.IO;
using UnityEditor;
using UnityEngine;

namespace DrownedDream.EditorTools
{
    /// <summary>
    /// 洞窟生成與 Unity 的橋接（SD-03）：CaveGenConfig → CaveGenerator 參數、輸出碰撞遮罩 PNG、格座標 ↔ 世界座標。
    /// </summary>
    internal static class CaveBuild
    {
        /// <summary>洞窟碰撞遮罩輸出路徑。</summary>
        public const string MaskPath = "Assets/Art/Map/MapMask_Cave.png";
        /// <summary>洞窟生成參數資產路徑。</summary>
        public const string ConfigPath = "Assets/Data/Map/CaveGenConfig.asset";

        /// <summary>依設定生成洞窟（fishPatrolUnits = 魚怪單邊巡邏距離，用來找夠寬的位置）。</summary>
        public static CaveLayout Generate(CaveGenConfig config, float cellUnits, float fishPatrolUnits)
        {
            int Cells(float units) => Mathf.Max(1, Mathf.RoundToInt(units / cellUnits));
            var settings = new CaveSettings
            {
                Width = Cells(config.WidthUnits),
                Height = Cells(config.HeightUnits),
                Seed = config.Seed,
                FillPercent = config.FillPercent,
                SmoothIterations = config.SmoothIterations,
                StartW = Cells(config.StartRoomSize.x),
                StartH = Cells(config.StartRoomSize.y),
                BossW = Cells(config.BossRoomSize.x),
                BossH = Cells(config.BossRoomSize.y),
                TunnelH = Cells(config.TunnelHeight),
                GapMin = Cells(config.LedgeGapMin),
                GapMax = Cells(config.LedgeGapMax),
                ItemSpacing = Cells(config.ItemSpacing),
                EnemySpacing = Cells(config.EnemySpacing),
                EnemySafeRadius = Cells(config.EnemySafeRadius),
                FishPatrol = Cells(fishPatrolUnits),
            };
            settings.Counts[CaveThing.Checkpoint] = config.Checkpoints;
            settings.Counts[CaveThing.Seal] = config.Seals;
            settings.Counts[CaveThing.Pill] = config.Pills;
            settings.Counts[CaveThing.Medkit] = config.Medkits;
            settings.Counts[CaveThing.Fish] = config.Fish;
            settings.Counts[CaveThing.Tentacle] = config.Tentacles;
            settings.Counts[CaveThing.Eye] = config.Eyes;
            settings.Counts[CaveThing.Hallucination] = config.Hallucinations;

            var generator = new CaveGenerator(settings);
            var layout = generator.Generate();
            if (layout.Seed != config.Seed)
            {
                Debug.LogWarning($"[DrownedDream] 種子 {config.Seed} 不合格，改用 {layout.Seed}：\n" + string.Join("\n", generator.Failures));
            }
            Debug.Log($"[DrownedDream] 洞窟生成：種子 {layout.Seed}、補石台 {layout.Ledges}、可來回站立點 {layout.GoodStands}、物件 {layout.Things.Count}");
            return layout;
        }

        /// <summary>輸出碰撞遮罩 PNG（黑 = 牆、白 = 空，每格 cellPixels 像素）並匯入。</summary>
        public static Texture2D WriteMask(CaveLayout layout, int cellPixels)
        {
            int w = layout.Solid.GetLength(0), h = layout.Solid.GetLength(1);
            var tex = new Texture2D(w * cellPixels, h * cellPixels, TextureFormat.RGBA32, false);
            var pixels = new Color32[tex.width * tex.height];
            var black = new Color32(0, 0, 0, 255);
            var white = new Color32(255, 255, 255, 255);
            for (int py = 0; py < tex.height; py++)
            {
                for (int px = 0; px < tex.width; px++)
                {
                    pixels[py * tex.width + px] = layout.Solid[px / cellPixels, py / cellPixels] ? black : white;
                }
            }
            tex.SetPixels32(pixels);
            EditorBuildUtil.EnsureFolder(Path.GetDirectoryName(MaskPath)?.Replace('\\', '/'));
            File.WriteAllBytes(MaskPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(MaskPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(MaskPath) is TextureImporter importer)
            {
                // 只是給人看的預覽；程式直接讀檔，不依賴匯入結果
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath);
        }

        /// <summary>站立格 → 世界座標的腳底中心（角色 2 格寬，中心在第 2 格左緣）。</summary>
        public static Vector2 Feet(CaveCell c, float cellUnits) => new Vector2((c.X + 1) * cellUnits, c.Y * cellUnits);

        /// <summary>格矩形 → 世界矩形。</summary>
        public static Rect ToWorld(CaveRect r, float cellUnits) => new Rect(r.X * cellUnits, r.Y * cellUnits, r.W * cellUnits, r.H * cellUnits);
    }
}
