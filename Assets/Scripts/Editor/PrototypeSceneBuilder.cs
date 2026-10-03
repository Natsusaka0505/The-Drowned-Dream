using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static DrownedDream.EditorTools.EditorBuildUtil;

namespace DrownedDream.EditorTools
{
    /// <summary>
    /// 一鍵產生原型場景：選單 Drowned Dream/Build Prototype Scene。
    /// 建立 Layer、佔位圖、ScriptableObject 資料、魚叉 Prefab、隨機洞窟地圖（SD-03）與自動擺放的關卡內容。
    /// 已存在的資料資產不會覆蓋（保留企劃調整過的數值）；場景、洞窟遮罩與魚叉 Prefab 每次依種子重建。
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        /// <summary>輸出場景路徑。</summary>
        private const string ScenePath = "Assets/Scenes/Prototype.unity";
        /// <summary>資料資產根目錄。</summary>
        private const string DataDir = "Assets/Data";
        /// <summary>佔位美術目錄。</summary>
        public const string ArtDir = "Assets/Art/Placeholder";
        /// <summary>Prefab 目錄。</summary>
        private const string PrefabDir = "Assets/Prefabs";
        /// <summary>水流（憋氣屏障外觀）圖路徑。</summary>
        private const string WaterfallPath = "Assets/Art/Map/Props/prop_waterfall.png";
        /// <summary>每單位像素數。</summary>
        private const int PixelsPerUnit = 32;
        /// <summary>遮罩一格像素數（= 碰撞格 0.5 單位）。</summary>
        private const int CellPixels = 16;
        /// <summary>遠景背景圖路徑。</summary>
        public const string FarBackgroundPath = "Assets/Art/Background/background.png";
        /// <summary>地圖設定資產路徑。</summary>
        public const string MapConfigPath = "Assets/Data/Map/MapConfig.asset";
        /// <summary>探索 BGM 路徑。</summary>
        public const string ExploreBgmPath = "Assets/Audio/BGM/bgm_explore.wav";
        /// <summary>開場 BGM 路徑。</summary>
        public const string IntroBgmPath = "Assets/Audio/BGM/bgm_intro.wav";

        /// <summary>地面 Layer。</summary>
        private static int s_groundLayer;
        /// <summary>玩家 Layer。</summary>
        private static int s_playerLayer;
        /// <summary>敵人 Layer。</summary>
        private static int s_enemyLayer;
        /// <summary>魚叉 Layer。</summary>
        private static int s_harpoonLayer;
        /// <summary>方塊佔位 Sprite。</summary>
        private static Sprite s_square;
        /// <summary>圓形佔位 Sprite。</summary>
        private static Sprite s_circle;

        /// <summary>選單入口：重建整個原型場景。</summary>
        [MenuItem("Drowned Dream/Build Prototype Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("重建原型場景", "請先停止 Play 模式再執行。", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("重建原型場景", $"{ScenePath} 已存在，要覆蓋嗎？\n（資料資產不會被覆蓋）", "覆蓋", "取消"))
            {
                return;
            }

            // 必須先開新場景：NewScene(Single) 會卸載未使用的資產，
            // 若先建立資料資產，之後拿到的參照會變成已銷毀物件（綁定結果為 null）。
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureFolder("Assets/Scenes");
            EnsureFolder(PrefabDir);
            s_groundLayer = EnsureLayer("Ground", 6);
            s_playerLayer = EnsureLayer("Player", 7);
            s_enemyLayer = EnsureLayer("Enemy", 8);
            s_harpoonLayer = EnsureLayer("Harpoon", 9);
            s_square = EnsureShapeSprite(ArtDir, "Square", circle: false);
            s_circle = EnsureShapeSprite(ArtDir, "Circle", circle: true);

            var data = CreateData();
            var harpoonPrefab = CreateHarpoonPrefab();
            BuildScene(data, harpoonPrefab);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[DrownedDream] 原型場景建立完成：" + ScenePath);
        }

        // ───────────────────────── 資料 ─────────────────────────

        /// <summary>本次建置用到的所有資料資產。</summary>
        private class DataSet
        {
            /// <summary>移動參數。</summary>
            public MovementConfig Movement;
            /// <summary>HP / 氧氣 / 復活參數。</summary>
            public VitalsConfig Vitals;
            /// <summary>憋氣參數。</summary>
            public BreathConfig Breath;
            /// <summary>SAN 參數。</summary>
            public SanityConfig Sanity;
            /// <summary>魚槍參數。</summary>
            public HarpoonConfig Harpoon;
            /// <summary>地圖設定。</summary>
            public MapConfig Map;
            /// <summary>洞窟生成參數。</summary>
            public CaveGenConfig Cave;
            /// <summary>封印碎片 Prefab。</summary>
            public GameObject Seal;
            /// <summary>鎮靜藥丸 Prefab（回復 SAN）。</summary>
            public GameObject Pill;
            /// <summary>海草繃帶 Prefab（回復 HP）。</summary>
            public GameObject Medkit;
            /// <summary>巡游魚怪。</summary>
            public EnemyData Fish;
            /// <summary>觸手。</summary>
            public EnemyData Tentacle;
            /// <summary>深淵之眼。</summary>
            public EnemyData Eye;
            /// <summary>玩家用零摩擦材質（避免黏牆）。</summary>
            public PhysicsMaterial2D NoFriction;
        }

        /// <summary>載入或建立所有資料資產與道具 Prefab。</summary>
        private static DataSet CreateData()
        {
            var d = new DataSet
            {
                Movement = Asset<MovementConfig>($"{DataDir}/Config/MovementConfig.asset"),
                Vitals = Asset<VitalsConfig>($"{DataDir}/Config/VitalsConfig.asset"),
                Breath = Asset<BreathConfig>($"{DataDir}/Config/BreathConfig.asset"),
                Sanity = Asset<SanityConfig>($"{DataDir}/Config/SanityConfig.asset"),
                Harpoon = Asset<HarpoonConfig>($"{DataDir}/Config/HarpoonConfig.asset"),
            };

            d.Map = Asset<MapConfig>(MapConfigPath, so => Set(so,
                ("_farBackground", AssetDatabase.LoadAssetAtPath<Texture2D>(FarBackgroundPath)),
                ("_pixelsPerUnit", PixelsPerUnit),
                ("_maskCellPixels", CellPixels)));
            d.Cave = Asset<CaveGenConfig>(CaveBuild.ConfigPath);
            MapBuilder.EnsureTerrainSet(d.Map); // 舊的 MapConfig 沒有地形素材時補上預設切片
            if (d.Map.FarBackground == null)
            {
                // 舊的 MapConfig 沒有遠景欄位時補上（不覆蓋企劃已設定的值）
                var far = AssetDatabase.LoadAssetAtPath<Texture2D>(FarBackgroundPath);
                if (far != null)
                {
                    Wire(d.Map, ("_farBackground", far));
                    EditorUtility.SetDirty(d.Map);
                }
            }

            d.Seal = ItemPrefab<SealItem>("SealFragment", "封印碎片", new Color(1f, 0.85f, 0.3f), null);
            d.Pill = ItemPrefab<RecoveryItem>("Pill", "鎮靜藥丸", new Color(0.95f, 0.75f, 0.9f),
                so => Set(so, ("_sanityRestore", 30d), ("_hpRestore", 0d)));
            d.Medkit = ItemPrefab<RecoveryItem>("Medkit", "海草繃帶", new Color(0.5f, 1f, 0.6f),
                so => Set(so, ("_sanityRestore", 0d), ("_hpRestore", 30d)));

            d.Fish = Asset<EnemyData>($"{DataDir}/Enemies/FishMonster.asset", so => Set(so,
                ("_displayName", "巡游魚怪"),
                ("_behaviour", EnemyBehaviour.Patrol),
                ("_maxHits", 2),
                ("_moveSpeed", 2f),
                ("_chaseSpeed", 4.5f),
                ("_patrolDistance", 4f),
                ("_detectRange", 6f),
                ("_attackRange", 0.8f),
                ("_fearRange", 4f),
                ("_sanityDrainPerSecond", 5f),
                ("_attackDamage", 20f),
                ("_sanityRestoreOnKill", 15f),
                ("_dropPrefab", d.Pill),
                ("_dropChance", 0.5f),
                ("_color", new Color(0.8f, 0.3f, 0.3f)),
                ("_size", new Vector2(1.4f, 0.8f))));

            d.Tentacle = Asset<EnemyData>($"{DataDir}/Enemies/Tentacle.asset", so => Set(so,
                ("_displayName", "觸手"),
                ("_behaviour", EnemyBehaviour.Stationary),
                ("_maxHits", 3),
                ("_moveSpeed", 0f),
                ("_detectRange", 2.5f),
                ("_attackRange", 2.5f),
                ("_fearRange", 4f),
                ("_sanityDrainPerSecond", 4f),
                ("_attackDamage", 25f),
                ("_attackWindup", 0.6f),
                ("_attackActiveTime", 0.4f),
                ("_attackCooldown", 1.5f),
                ("_sanityRestoreOnKill", 15f),
                ("_dropPrefab", d.Medkit),
                ("_dropChance", 1f),
                ("_color", new Color(0.4f, 0.7f, 0.35f)),
                ("_size", new Vector2(0.8f, 2f))));

            d.Eye = Asset<EnemyData>($"{DataDir}/Enemies/AbyssEye.asset", so => Set(so,
                ("_displayName", "深淵之眼"),
                ("_behaviour", EnemyBehaviour.Passive),
                ("_maxHits", 4),
                ("_moveSpeed", 0f),
                ("_detectRange", 0f),
                ("_attackRange", 0f),
                ("_fearRange", 7f),
                ("_sanityDrainPerSecond", 8f),
                ("_attackDamage", 0f),
                ("_sanityRestoreOnKill", 25f),
                ("_color", new Color(0.95f, 0.9f, 0.4f)),
                ("_size", new Vector2(1.2f, 1.2f))));

            string matPath = $"{DataDir}/Config/PlayerNoFriction.physicsMaterial2D";
            d.NoFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
            if (d.NoFriction == null)
            {
                d.NoFriction = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(d.NoFriction, matPath);
            }
            return d;
        }

        /// <summary>建立魚叉 Prefab（每次覆蓋）。</summary>
        private static Harpoon CreateHarpoonPrefab()
        {
            var go = new GameObject("Harpoon") { layer = s_harpoonLayer };
            var visual = Child(go.transform, "Visual", Vector2.zero);
            var shaft = MakeSprite(Child(visual, "Shaft", Vector2.zero).gameObject, s_square, new Color(0.85f, 0.85f, 0.8f), 12);
            shaft.transform.localScale = new Vector3(0.9f, 0.1f, 1f);
            var tip = MakeSprite(Child(visual, "Tip", new Vector2(0.5f, 0f)).gameObject, s_square, new Color(0.7f, 0.75f, 0.8f), 12);
            tip.transform.localScale = new Vector3(0.2f, 0.22f, 1f);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 0.6f);
            var harpoon = go.AddComponent<Harpoon>();
            Wire(harpoon,
                ("_groundMask", Mask(s_groundLayer)),
                ("_enemyMask", Mask(s_enemyLayer)));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/Harpoon.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Harpoon>();
        }

        // ───────────────────────── 場景 ─────────────────────────

        /// <summary>建立光源、地圖、關卡內容、玩家、攝影機、UI 與流程。</summary>
        private static void BuildScene(DataSet d, Harpoon harpoonPrefab)
        {
            var globalLight = new GameObject("Global Light 2D").AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 0.85f;
            globalLight.color = new Color(0.55f, 0.75f, 1f);

            // 依種子生成洞窟 → 輸出遮罩 → MapConfig 改用新遮罩（不再使用整張地圖圖）
            float cs = d.Map.MaskCellPixels / (float)d.Map.PixelsPerUnit;
            var layout = CaveBuild.Generate(d.Cave, cs, d.Fish.PatrolDistance);
            var mask = CaveBuild.WriteMask(layout, d.Map.MaskCellPixels);
            Wire(d.Map, ("_collisionMask", mask), ("_mapTexture", null));
            EditorUtility.SetDirty(d.Map);

            var mapBounds = MapBuilder.Build(d.Map, s_groundLayer, s_square);
            BuildContent(d, layout, cs);

            var spawn = CaveBuild.Feet(layout.Start, cs) + new Vector2(0f, 0.8f);
            var player = BuildPlayer(d, harpoonPrefab, spawn);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(spawn.x, spawn.y, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.02f, 0.04f);
            camGo.AddComponent<AudioListener>();
            var gameCamera = camGo.AddComponent<GameCamera>();
            Wire(gameCamera, ("_target", player), ("_mapBounds", mapBounds));

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var effects = canvasGo.AddComponent<SanityScreenEffects>();
            Wire(effects, ("_globalLight", globalLight), ("_camera", gameCamera));
            canvasGo.AddComponent<HUD>();
            canvasGo.AddComponent<InventoryPanel>();
            var story = canvasGo.AddComponent<StoryPanel>();

            var flow = new GameObject("GameFlow").AddComponent<GameFlow>();
            Wire(flow, ("_storyPanel", story));

            BuildBgm();
        }

        /// <summary>建立 BGM 播放器並綁定開場 / 探索 BGM（檔案不存在時只警告）。</summary>
        private static void BuildBgm()
        {
            var intro = EnsureBgmImport(IntroBgmPath);
            var explore = EnsureBgmImport(ExploreBgmPath);
            if (intro == null) Debug.LogWarning("[DrownedDream] 找不到 BGM：" + IntroBgmPath);
            if (explore == null) Debug.LogWarning("[DrownedDream] 找不到 BGM：" + ExploreBgmPath);

            var bgm = new GameObject("BGM").AddComponent<BgmPlayer>();
            Wire(bgm, ("_introClip", intro), ("_exploreClip", explore));
        }

        /// <summary>BGM 匯入設定：串流載入 + Vorbis 壓縮（WAV 原檔太大，不整首解壓進記憶體）。</summary>
        private static AudioClip EnsureBgmImport(string path)
        {
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer) return null;

            var settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.Streaming || settings.compressionFormat != AudioCompressionFormat.Vorbis)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        /// <summary>依洞窟生成結果擺放存檔點、道具、敵人、幻覺、Boss 廳（SD-03）。敵人一律站在地面上。</summary>
        private static void BuildContent(DataSet d, CaveLayout layout, float cs)
        {
            var root = new GameObject("Content").transform;
            const float itemLift = 0.6f; // 道具 / 存檔點中心離地高度
            int hallStage = 0;

            foreach (var (thing, cell) in layout.Things)
            {
                var feet = CaveBuild.Feet(cell, cs);
                switch (thing)
                {
                    case CaveThing.Checkpoint: MakeCheckpoint(root, feet + Vector2.up * itemLift); break;
                    case CaveThing.Seal: MakePickup(root, d.Seal, feet + Vector2.up * itemLift); break;
                    case CaveThing.Pill: MakePickup(root, d.Pill, feet + Vector2.up * itemLift); break;
                    case CaveThing.Medkit: MakePickup(root, d.Medkit, feet + Vector2.up * itemLift); break;
                    case CaveThing.Fish: MakeEnemy(root, d.Fish, feet); break;
                    case CaveThing.Tentacle: MakeEnemy(root, d.Tentacle, feet); break;
                    case CaveThing.Eye: MakeEnemy(root, d.Eye, feet); break;
                    case CaveThing.Hallucination: MakeHallucination(root, feet + Vector2.up * 2f, 1 + (hallStage++ % 2)); break;
                }
            }

            var bossRoom = CaveBuild.ToWorld(layout.BossRoom, cs);
            var boss = MakeBoss(root, CaveBuild.Feet(layout.Boss, cs));
            MakeAltar(root, boss, CaveBuild.Feet(layout.Altar, cs) + Vector2.up * 0.5f);
            MakeBossArea(root, boss, bossRoom);
            MakeBreathGate(root, CaveBuild.ToWorld(layout.Gate, cs));
        }

        /// <summary>建立玩家與所有玩家元件。</summary>
        private static GameObject BuildPlayer(DataSet d, Harpoon harpoonPrefab, Vector2 position)
        {
            var go = new GameObject("Player") { layer = s_playerLayer };
            go.transform.position = position;

            var body = go.AddComponent<Rigidbody2D>();
            body.sharedMaterial = d.NoFriction;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.7f, 1.4f);
            col.edgeRadius = 0.05f;
            col.sharedMaterial = d.NoFriction;

            var visual = Child(go.transform, "Visual", Vector2.zero);
            var bodySr = MakeSprite(Child(visual, "Body", Vector2.zero).gameObject, s_square, new Color(0.85f, 0.75f, 0.55f), 15);
            bodySr.transform.localScale = new Vector3(0.75f, 1.5f, 1f);
            var maskSr = MakeSprite(Child(visual, "DivingMask", new Vector2(0.18f, 0.4f)).gameObject, s_square, new Color(0.4f, 0.85f, 1f), 16);
            maskSr.transform.localScale = new Vector3(0.4f, 0.3f, 1f);

            var lantern = Child(go.transform, "Lantern", new Vector2(0f, 0.3f)).gameObject.AddComponent<Light2D>();
            lantern.lightType = Light2D.LightType.Point;
            lantern.pointLightOuterRadius = 7f;
            lantern.pointLightInnerRadius = 1f;
            lantern.intensity = 0.9f;
            lantern.color = new Color(1f, 0.9f, 0.7f);

            go.AddComponent<Player>(); // RequireComponent 會自動補齊其他玩家元件
            var renderers = new Object[] { bodySr, maskSr };

            Wire(go.GetComponent<PlayerStatus>(),
                ("_vitals", d.Vitals), ("_sanityConfig", d.Sanity), ("_movement", d.Movement), ("_harpoon", d.Harpoon),
                ("_requiredSeals", d.Cave.Seals), ("_flashRenderers", renderers));
            Wire(go.GetComponent<PlayerMove>(), ("_groundMask", Mask(s_groundLayer)), ("_visual", visual));
            Wire(go.GetComponent<PlayerBreath>(), ("_config", d.Breath), ("_fadeRenderers", renderers));
            Wire(go.GetComponent<PlayerAttack>(), ("_harpoonPrefab", harpoonPrefab));
            return go;
        }

        // ───────────────────────── 物件工廠 ─────────────────────────

        /// <summary>建立存檔點。</summary>
        private static void MakeCheckpoint(Transform parent, Vector2 pos)
        {
            var go = new GameObject("Checkpoint");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var sr = MakeSprite(Child(go.transform, "Visual", Vector2.zero).gameObject, s_square, new Color(0.5f, 0.55f, 0.6f), 3);
            sr.transform.localScale = new Vector3(0.5f, 1.2f, 1f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.6f);
            var cp = go.AddComponent<Checkpoint>();
            Wire(cp, ("_renderer", sr));
        }

        /// <summary>在場景放置道具 Prefab。</summary>
        private static void MakePickup(Transform parent, GameObject prefab, Vector2 pos)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = pos;
        }

        /// <summary>載入或建立道具 Prefab（已存在則保留企劃調整）。</summary>
        private static GameObject ItemPrefab<T>(string fileName, string displayName, Color color, System.Action<SerializedObject> init) where T : PickupItem
        {
            string path = $"{PrefabDir}/Items/{fileName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            EnsureFolder($"{PrefabDir}/Items");
            var go = new GameObject(fileName);
            go.transform.localScale = Vector3.one * 0.6f;
            MakeSprite(go, s_circle, color, 5);
            go.AddComponent<CircleCollider2D>();
            var item = go.AddComponent<T>();
            var so = new SerializedObject(item);
            Set(so, ("_displayName", displayName));
            init?.Invoke(so);
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>建立一般敵人（feet = 地面位置；碰撞框底部貼地，與玩家同一高度帶）。</summary>
        private static void MakeEnemy(Transform parent, EnemyData data, Vector2 feet)
        {
            var go = new GameObject($"Enemy_{data.name}") { layer = s_enemyLayer };
            go.transform.SetParent(parent);
            go.transform.position = feet + Vector2.up * (data.Size.y / 2f);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = data.Size;
            col.isTrigger = true;
            var sr = MakeSprite(Child(go.transform, "Visual", Vector2.zero).gameObject,
                data.Behaviour == EnemyBehaviour.Passive ? s_circle : s_square, data.Color, 10);
            sr.transform.localScale = new Vector3(data.Size.x, data.Size.y, 1f);
            var status = go.AddComponent<EnemyStatus>();
            Wire(status, ("_data", data));
            var ai = go.AddComponent<EnemyAI>();
            Wire(ai, ("_renderer", sr));
        }

        /// <summary>建立 Boss（feet = 地面位置；Boss 站在 Boss 廳地板上）。</summary>
        private static BossController MakeBoss(Transform parent, Vector2 feet)
        {
            var go = new GameObject("Boss") { layer = s_enemyLayer };
            go.transform.SetParent(parent);
            go.transform.position = feet + Vector2.up * 3f; // 碰撞框高 6
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(4f, 6f);
            col.isTrigger = true;
            var sr = MakeSprite(Child(go.transform, "Visual", Vector2.zero).gameObject, s_circle, new Color(0.25f, 0.45f, 0.35f), 8);
            sr.transform.localScale = new Vector3(4.5f, 6.5f, 1f);
            var boss = go.AddComponent<BossController>();
            Wire(boss, ("_renderer", sr), ("_projectileSprite", s_circle), ("_projectileBlockMask", Mask(s_groundLayer)));
            Wire(go.GetComponent<FearSource>(), ("_radius", 14f), ("_drainPerSecond", 4f));
            return boss;
        }

        /// <summary>建立封印祭壇。</summary>
        private static void MakeAltar(Transform parent, BossController boss, Vector2 pos)
        {
            var go = new GameObject("SealAltar");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var sr = MakeSprite(Child(go.transform, "Visual", Vector2.zero).gameObject, s_square, new Color(0.75f, 0.65f, 0.4f), 3);
            sr.transform.localScale = new Vector3(2f, 1f, 1f);
            var altar = go.AddComponent<SealAltar>();
            Wire(altar, ("_boss", boss));
        }

        /// <summary>建立 Boss 廳 area（觸發框涵蓋整個 Boss 廳）。</summary>
        private static void MakeBossArea(Transform parent, BossController boss, Rect room)
        {
            var go = new GameObject("BossArea") { layer = 2 }; // Ignore Raycast
            go.transform.SetParent(parent);
            go.transform.position = room.center;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = room.size;
            var area = go.AddComponent<BossArea>();
            Wire(area, ("_boss", boss));
        }

        /// <summary>建立憋氣屏障（Boss 廳入口隧道；外觀為水流，憋氣才能穿過，F-BRE-05）。</summary>
        private static void MakeBreathGate(Transform parent, Rect rect)
        {
            var go = new GameObject("BreathGate") { layer = s_groundLayer };
            go.transform.SetParent(parent);
            go.transform.position = rect.center;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = rect.size;

            // 水流圖：高度貼齊隧道、寬度依比例；沒有圖就用方塊
            ConfigureSprite(WaterfallPath, 100, SpriteAlignment.Center);
            var waterfall = AssetDatabase.LoadAssetAtPath<Sprite>(WaterfallPath);
            var visual = Child(go.transform, "Visual", Vector2.zero);
            SpriteRenderer sr;
            if (waterfall != null)
            {
                sr = MakeSprite(visual.gameObject, waterfall, Color.white, 4);
                float scale = rect.height / waterfall.bounds.size.y;
                visual.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                sr = MakeSprite(visual.gameObject, s_square, new Color(0.45f, 0.9f, 0.6f), 4);
                visual.localScale = new Vector3(rect.width, rect.height, 1f);
            }
            var gate = go.AddComponent<BreathGate>();
            Wire(gate, ("_renderer", sr));
        }

        /// <summary>低 SAN 時才出現的幻覺（F-SAN-10 範例）。</summary>
        private static void MakeHallucination(Transform parent, Vector2 pos, int minStage)
        {
            var root = new GameObject($"Hallucination_Stage{minStage}");
            root.transform.SetParent(parent);
            root.transform.position = pos;
            var target = Child(root.transform, "Eyes", Vector2.zero).gameObject;
            for (int i = 0; i < 4; i++)
            {
                var eye = MakeSprite(Child(target.transform, $"Eye{i}", new Vector2(i * 1.6f - 2.4f, (i % 2) * 0.8f)).gameObject,
                    s_circle, new Color(0.9f, 0.1f, 0.2f, 0.7f), 1);
                eye.transform.localScale = new Vector3(0.6f, 0.3f, 1f);
            }
            var stage = root.AddComponent<SanityStageObject>();
            Wire(stage, ("_minStage", minStage), ("_target", target));
            target.SetActive(false);
        }

        /// <summary>將原型場景放到 Build Settings 第一個。</summary>
        private static void AddSceneToBuild()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
