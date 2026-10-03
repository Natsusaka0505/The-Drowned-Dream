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
    /// 建立 Layer、佔位圖、ScriptableObject 資料、魚叉 Prefab、地圖（4×4 區塊）與關卡內容。
    /// 已存在的資料資產不會覆蓋（保留企劃調整過的數值）；場景與魚叉 Prefab 每次重建。
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
        /// <summary>地圖設定資產路徑。</summary>
        public const string MapConfigPath = "Assets/Data/Map/MapConfig.asset";

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
            PrototypeMapLayout.EnsurePlaceholders();

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
                ("_mapTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(PrototypeMapLayout.MapPath)),
                ("_collisionMask", AssetDatabase.LoadAssetAtPath<Texture2D>(PrototypeMapLayout.MaskPath)),
                ("_columns", PrototypeMapLayout.Grid),
                ("_rows", PrototypeMapLayout.Grid),
                ("_pixelsPerUnit", PrototypeMapLayout.PixelsPerUnit),
                ("_maskCellPixels", PrototypeMapLayout.CellPixels)));

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

            MapBuilder.Build(d.Map, s_groundLayer, s_square);
            BuildContent(d);

            var spawn = PrototypeMapLayout.Local(0, 3, 3f, 1.3f);
            var player = BuildPlayer(d, harpoonPrefab, spawn);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(spawn.x, spawn.y, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.02f, 0.04f);
            camGo.AddComponent<AudioListener>();
            var gameCamera = camGo.AddComponent<GameCamera>();
            Wire(gameCamera, ("_target", player));

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
        }

        /// <summary>
        /// 各區塊內容（局部座標，單位；地板頂端 y = 0.5）。
        /// 路線：上排 左→右 → 第 2 排 右→左 → 第 1 排 左→右 → 下排 右→左（Boss）。
        /// </summary>
        private static void BuildContent(DataSet d)
        {
            var root = new GameObject("Content").transform;
            const float floorItemY = 1.1f;   // 地板上的道具高度
            const float lowPlatY = 3.6f;      // 低平台（頂 3.0）上的道具高度
            const float highPlatY = 5.6f;     // 高平台（頂 5.0）上的道具高度

            // 第 3 排（起點）
            MakeCheckpoint(root, PrototypeMapLayout.Local(0, 3, 5f, floorItemY));
            MakePickup(root, d.Pill, PrototypeMapLayout.Local(0, 3, 11f, highPlatY));
            MakeEnemy(root, d.Fish, PrototypeMapLayout.Local(1, 3, 8f, 3f));
            MakeEnemy(root, d.Eye, PrototypeMapLayout.Local(2, 3, 12f, 11f));
            MakePickup(root, d.Seal, PrototypeMapLayout.Local(2, 3, 11f, highPlatY));
            MakeHallucination(root, PrototypeMapLayout.Local(2, 3, 6f, 12f), 1);
            MakePickup(root, d.Medkit, PrototypeMapLayout.Local(3, 3, 12f, floorItemY));

            // 第 2 排（右 → 左）
            MakeEnemy(root, d.Tentacle, PrototypeMapLayout.Local(3, 2, 14.5f, 1.5f));
            MakeEnemy(root, d.Fish, PrototypeMapLayout.Local(2, 2, 8f, 3f));
            MakePickup(root, d.Pill, PrototypeMapLayout.Local(2, 2, 11f, highPlatY));
            MakePickup(root, d.Medkit, PrototypeMapLayout.Local(1, 2, 6f, lowPlatY));
            MakeEnemy(root, d.Eye, PrototypeMapLayout.Local(1, 2, 3f, 11f));
            MakeHallucination(root, PrototypeMapLayout.Local(1, 2, 9f, 12f), 2);
            MakeCheckpoint(root, PrototypeMapLayout.Local(0, 2, 12f, floorItemY));

            // 第 1 排（左 → 右），(0,1) 有憋氣屏障擋住往右的路
            MakeBreathGate(root, PrototypeMapLayout.Local(0, 1, 14f, 0.5f), new Vector2(0.5f, 15f));
            MakePickup(root, d.Seal, PrototypeMapLayout.Local(1, 1, 8f, floorItemY));
            MakeEnemy(root, d.Fish, PrototypeMapLayout.Local(1, 1, 10f, 3f));
            MakeEnemy(root, d.Eye, PrototypeMapLayout.Local(2, 1, 8f, 11f));
            MakePickup(root, d.Medkit, PrototypeMapLayout.Local(2, 1, 11f, highPlatY));
            MakeCheckpoint(root, PrototypeMapLayout.Local(3, 1, 4.5f, floorItemY));

            // 第 0 排（右 → 左），終點 Boss
            MakePickup(root, d.Seal, PrototypeMapLayout.Local(3, 0, 1.5f, floorItemY));
            MakeEnemy(root, d.Tentacle, PrototypeMapLayout.Local(3, 0, 3.5f, 1.5f));
            MakeEnemy(root, d.Fish, PrototypeMapLayout.Local(2, 0, 8f, 3f));
            MakePickup(root, d.Pill, PrototypeMapLayout.Local(2, 0, 11f, highPlatY));
            MakeCheckpoint(root, PrototypeMapLayout.Local(1, 0, 12f, floorItemY));
            MakePickup(root, d.Medkit, PrototypeMapLayout.Local(1, 0, 6f, lowPlatY));

            var boss = MakeBoss(root, PrototypeMapLayout.Local(0, 0, 4f, 9f));
            MakeAltar(root, boss, PrototypeMapLayout.Local(0, 0, 10f, 1f));
            MakeBossArea(root, boss, PrototypeMapLayout.Local(0, 0, 8f, 8f));
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
                ("_requiredSeals", 3), ("_flashRenderers", renderers));
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

        /// <summary>建立一般敵人。</summary>
        private static void MakeEnemy(Transform parent, EnemyData data, Vector2 pos)
        {
            var go = new GameObject($"Enemy_{data.name}") { layer = s_enemyLayer };
            go.transform.SetParent(parent);
            go.transform.position = pos;
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

        /// <summary>建立 Boss。</summary>
        private static BossController MakeBoss(Transform parent, Vector2 pos)
        {
            var go = new GameObject("Boss") { layer = s_enemyLayer };
            go.transform.SetParent(parent);
            go.transform.position = pos;
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

        /// <summary>建立 Boss 房 area（放在 Boss 房內任一點即可）。</summary>
        private static void MakeBossArea(Transform parent, BossController boss, Vector2 pos)
        {
            var go = new GameObject("BossArea");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var area = go.AddComponent<BossArea>();
            Wire(area, ("_boss", boss));
        }

        /// <summary>建立憋氣屏障（左下角位置 + 尺寸）。</summary>
        private static void MakeBreathGate(Transform parent, Vector2 bottomLeft, Vector2 size)
        {
            var go = new GameObject("BreathGate") { layer = s_groundLayer };
            go.transform.SetParent(parent);
            go.transform.position = bottomLeft + size / 2f;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = MakeSprite(go, s_square, new Color(0.45f, 0.9f, 0.6f), 4);
            go.AddComponent<BoxCollider2D>();
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
