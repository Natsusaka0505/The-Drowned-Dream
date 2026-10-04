using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static DrownedDream.EditorTools.EditorBuildUtil;

namespace DrownedDream.EditorTools
{
    /// <summary>
    /// 關卡 Prefab 流程（見 docs/feature/需求/08-map/SD/SD-03-level-prefab.md）：
    /// MapConfig.LevelPrefab 有設定時，建場景改為放入關卡 Prefab（平台 / 怪物 / 寶箱 / 存檔點 / Boss / 玩家起點），
    /// 不再用程式配置。選單 Drowned Dream/Create Level Template 產生可編輯的範本與零件 Prefab。
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        /// <summary>關卡 Prefab 與零件資料夾。</summary>
        private const string LevelDir = "Assets/Prefabs/Level";
        /// <summary>關卡零件（怪物 / 存檔點 / Boss 區）資料夾。</summary>
        private const string LevelPartsDir = LevelDir + "/Parts";
        /// <summary>關卡範本路徑。</summary>
        private const string LevelTemplatePath = LevelDir + "/Level.prefab";
        /// <summary>平台 Prefab 資料夾（隊友製作的 float1~6）。</summary>
        private const string PlatformPrefabDir = "Assets/Prefabs/float";

        /// <summary>
        /// 程式是否還沒編譯完：正在編譯，或 Assets/Scripts 裡有 .cs 比已載入的編譯結果還新（剛存檔、Unity 還沒開始編譯）。
        /// 這時建置會用到舊程式，必須擋下。
        /// </summary>
        public static bool ScriptsOutOfDate(out string reason)
        {
            reason = null;
            if (EditorApplication.isCompiling)
            {
                reason = "腳本正在編譯中";
                return true;
            }
            // 一般程式比對 Assembly-CSharp.dll、Editor 資料夾比對 Assembly-CSharp-Editor.dll（Unity 只在內容變動時才重寫各自的 dll）
            string dir = Path.Combine("Library", "ScriptAssemblies");
            string runtimeDll = Path.Combine(dir, "Assembly-CSharp.dll");
            string editorDll = Path.Combine(dir, "Assembly-CSharp-Editor.dll");
            if (!File.Exists(runtimeDll) || !File.Exists(editorDll)) return false;
            var runtimeBuilt = File.GetLastWriteTimeUtc(runtimeDll);
            var editorBuilt = File.GetLastWriteTimeUtc(editorDll);
            foreach (var cs in Directory.GetFiles(Path.Combine("Assets", "Scripts"), "*.cs", SearchOption.AllDirectories))
            {
                bool isEditor = cs.Replace('\\', '/').Contains("/Editor/");
                var built = isEditor ? editorBuilt : runtimeBuilt;
                if (File.GetLastWriteTimeUtc(cs) > built.AddSeconds(1))
                {
                    reason = $"程式檔 {Path.GetFileName(cs)} 比編譯結果新（Unity 還沒重新編譯）";
                    return true;
                }
            }
            return false;
        }

        /// <summary>原型配置：中央 2×2 合成 Boss 房、依程式放關卡內容，回傳玩家起點。</summary>
        private static Vector2 BuildPrototypeContent(DataSet d, Room[,] rooms)
        {
            MapBuilder.MergeRooms(rooms, PrototypeMapLayout.BossArena);
            BuildContent(d);
            return PrototypeMapLayout.Local(0, 3, 3f, 1.3f);
        }

        /// <summary>放入 MapConfig 的關卡 Prefab，回傳玩家起點（找不到 PlayerSpawnPoint 時用地圖底部中央）。</summary>
        private static Vector2 PlaceLevel(MapConfig map)
        {
            var level = (GameObject)PrefabUtility.InstantiatePrefab(map.LevelPrefab);
            level.name = "Level";
            var spawn = level.GetComponentInChildren<PlayerSpawnPoint>();
            if (spawn != null) return spawn.transform.position;

            Debug.LogWarning("[DrownedDream] 關卡 Prefab 裡沒有 PlayerSpawnPoint，玩家放在地圖底部中央");
            var size = MapBuilder.MapSizeUnits(map);
            return new Vector2(size.x / 2f, 2f);
        }

        /// <summary>選單：產生關卡範本 Prefab 與零件 Prefab（已存在的不覆蓋）。</summary>
        [MenuItem("Drowned Dream/Create Level Template")]
        public static void CreateLevelTemplate()
        {
            if (File.Exists(LevelTemplatePath) &&
                !EditorUtility.DisplayDialog("產生關卡範本", $"{LevelTemplatePath} 已存在，要覆蓋嗎？\n（零件 Prefab 不會被覆蓋）", "覆蓋", "取消"))
            {
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (ScriptsOutOfDate(out string reason))
            {
                EditorUtility.DisplayDialog("產生關卡範本", reason + "。\n請等右下角轉圈結束後再執行一次。", "OK");
                return;
            }
            InitShared();
            var d = CreateData();
            EnsureFolder(LevelPartsDir);

            var fish = PartPrefab("Enemy_Fish", () => MakeEnemy(null, d.Fish, Vector2.zero));
            var tentacle = PartPrefab("Enemy_Tentacle", () => MakeEnemy(null, d.Tentacle, Vector2.zero));
            var eye = PartPrefab("Enemy_Eye", () => MakeEnemy(null, d.Eye, Vector2.zero));
            var checkpoint = PartPrefab("Checkpoint", () => MakeCheckpoint(null, Vector2.zero));
            var arena = PartPrefab("BossArena", MakeBossArena);
            var spawnMarker = PartPrefab("PlayerSpawn", () => new GameObject("PlayerSpawn", typeof(PlayerSpawnPoint)));

            var size = MapBuilder.MapSizeUnits(d.Map);
            var root = new GameObject("Level");

            // 玩家起點：地圖最下方中央
            Place(spawnMarker, root.transform, new Vector2(size.x / 2f, 2f));

            // 平台：放在掛 PlatformGroup 的 Platforms 底下（執行時自動設成 Ground Layer），由下往上之字形
            var platforms = new GameObject("Platforms", typeof(PlatformGroup)).transform;
            platforms.SetParent(root.transform, false);
            var platformTops = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PlatformPrefabDir}/float{i + 1}.prefab");
                var pos = new Vector2(size.x / 2f + (i % 2 == 0 ? -5f : 5f), 4f + i * 4f);
                platformTops[i] = pos + Vector2.up * 0.5f;
                if (prefab != null) Place(prefab, platforms, pos);
                else Debug.LogWarning($"[DrownedDream] 找不到平台 Prefab：{PlatformPrefabDir}/float{i + 1}.prefab");
            }

            // 範例內容（之後在 Prefab 模式自由搬動 / 刪除 / 複製）
            var content = new GameObject("Content").transform;
            content.SetParent(root.transform, false);
            Place(d.ChestGray, content, platformTops[0]);
            Place(fish, content, new Vector2(size.x / 2f + 4f, 1f + d.Fish.Size.y / 2f));
            Place(tentacle, content, platformTops[1] + Vector2.up * (d.Tentacle.Size.y / 2f));
            Place(checkpoint, content, platformTops[2] + Vector2.up * 0.8f);
            Place(d.ChestBlueGray, content, platformTops[3]);
            Place(eye, content, platformTops[4] + Vector2.up * 2f);
            Place(d.ChestBlack, content, platformTops[5]);

            // 第 4 個封印道具 + 第 5 個的隨機出現點（第一次進 Boss 房後隨機選一個放黑寶箱）
            Place(d.ChestBlack, content, platformTops[2] + Vector2.right * 2f);
            MakeHiddenSealSpawner(content, d.ChestBlack, new[]
            {
                platformTops[0] + new Vector2(2f, 0.6f),
                platformTops[3] + new Vector2(-2f, 0.6f),
                platformTops[4] + new Vector2(2f, 0.6f),
            });

            // Boss 區：地圖正中央（2026-10-04 改；根物件在 Boss 腳底）
            Place(arena, root.transform, new Vector2(size.x / 2f, size.y / 2f));

            PrefabUtility.SaveAsPrefabAsset(root, LevelTemplatePath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DrownedDream] 關卡範本建立完成：{LevelTemplatePath}。把它拖到 MapConfig 的 Level Prefab 欄位後執行 Build Prototype Scene。");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(LevelTemplatePath);
        }

        /// <summary>載入或建立零件 Prefab（已存在則保留企劃調整）；create 回傳的物件會放到原點後存檔。</summary>
        private static GameObject PartPrefab(string fileName, System.Func<GameObject> create)
        {
            string path = $"{LevelPartsDir}/{fileName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = create();
            go.name = fileName;
            go.transform.position = Vector3.zero;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Boss 區：根物件在 Boss 腳底，內含 Boss、封印祭壇、BossArea（已互相綁定）。</summary>
        private static GameObject MakeBossArena()
        {
            var root = new GameObject("BossArena");
            var boss = MakeBoss(root.transform, Vector2.zero);
            // Boss 外觀約 16.6 寬，祭壇放在 Boss 左側外面
            MakeAltar(root.transform, boss, new Vector2(-11f, 0.5f));
            MakeBossArea(root.transform, boss, new Vector2(0f, 8f));
            return root;
        }

        /// <summary>在父物件下放一個 Prefab 實例。</summary>
        private static void Place(GameObject prefab, Transform parent, Vector2 position)
        {
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = position;
        }
    }
}
