using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
    public static partial class PrototypeSceneBuilder
    {
        /// <summary>輸出場景路徑。</summary>
        private const string ScenePath = "Assets/Scenes/Prototype.unity";
        /// <summary>資料資產根目錄。</summary>
        private const string DataDir = "Assets/Data";
        /// <summary>佔位美術目錄。</summary>
        public const string ArtDir = "Assets/Art/Placeholder";
        /// <summary>Prefab 目錄。</summary>
        private const string PrefabDir = "Assets/Prefabs";
        /// <summary>遠景背景圖路徑。</summary>
        public const string FarBackgroundPath = "Assets/Art/Background/background.png";
        /// <summary>地圖設定資產路徑。</summary>
        public const string MapConfigPath = "Assets/Data/Map/MapConfig.asset";
        /// <summary>探索 BGM 路徑。</summary>
        public const string ExploreBgmPath = "Assets/Audio/BGM/bgm_explore.wav";
        /// <summary>開場 BGM 路徑。</summary>
        public const string IntroBgmPath = "Assets/Audio/BGM/bgm_intro.wav";
        /// <summary>封面底圖路徑。</summary>
        public const string TitleCoverPath = "Assets/Art/UI/title_cover.png";
        /// <summary>HUD 圖資料夾（HP / SAN 框與填充條、氧氣泡泡）。</summary>
        public const string HudArtDir = "Assets/Art/UI/HUD";
        /// <summary>Start 按鈕圖路徑。</summary>
        public const string StartButtonPath = "Assets/Art/UI/btn_start.png";
        /// <summary>Quit 按鈕圖路徑。</summary>
        public const string QuitButtonPath = "Assets/Art/UI/btn_quit.png";
        /// <summary>邪神雕像（封印道具）圖路徑。</summary>
        public const string IdolSpritePath = "Assets/Art/Items/item_idol.png";
        /// <summary>藥丸（回復 SAN）圖路徑。</summary>
        public const string PillSpritePath = "Assets/Art/Items/item_pill.png";
        /// <summary>繃帶（回復 HP）圖路徑。</summary>
        public const string BandageSpritePath = "Assets/Art/Items/item_bandage.png";
        /// <summary>寶箱圖資料夾（chest_{顏色}_{closed|open}.png）。</summary>
        public const string ChestArtDir = "Assets/Art/Chests";
        /// <summary>寶箱圖 PPU（195px 寬 ≈ 1.3 單位）。</summary>
        private const int ChestPixelsPerUnit = 150;
        /// <summary>落雷特效 Prefab（FX Lightning II free）。</summary>
        public const string LightningFxPath = "Assets/FX_Kandol_Pack/FX_lightning_II/Prefabs/fx_lightning_02.prefab";
        /// <summary>深淵之眼光束特效 Prefab（FX Lightning II free）。</summary>
        public const string BeamFxPath = "Assets/FX_Kandol_Pack/FX_lightning_II/Prefabs/fx_lightning_01.prefab";
        /// <summary>Boss 畫格資料夾（由 boss.gif 拆出，boss_00~32.png）。</summary>
        public const string BossArtDir = "Assets/Art/Boss";
        /// <summary>Boss 畫格 PPU（459px 高 ≈ 6 單位）。</summary>
        private const int BossPixelsPerUnit = 76;
        /// <summary>Boss 每格秒數（照 boss.gif 原本的每格時間）。</summary>
        private static readonly float[] BossFrameDurations =
        {
            0.1f, 0.06f, 0.1f, 0.06f, 0.1f, 0.06f, 0.1f, 0.06f, 0.1f, 0.06f, 0.1f, 0.06f, 0.1f, 0.06f, 0.1f, 0.06f,
            0.1f, 0.06f, 0.1f, 0.06f, 0.1f, 0.06f, 0.1f, 0.06f, 0.1f, 0.06f, 0.1f, 0.06f, 0.1f, 0.06f, 0.1f, 0.06f, 0.03f,
        };
        /// <summary>怪物畫格資料夾（眼球 / 海蝶 / 觸鬚）。</summary>
        public const string EnemyArtDir = "Assets/Art/Enemies";
        /// <summary>角色畫格資料夾。</summary>
        public const string PlayerArtDir = "Assets/Art/Player";
        /// <summary>角色畫格 PPU（站姿約 455px 高 ≈ 1.6 單位）。</summary>
        private const int PlayerPixelsPerUnit = 285;
        /// <summary>面向右的 pivot（裁切框內：身體中心 x、腳底 y）。</summary>
        private static readonly Vector2 PlayerPivotRight = new Vector2(0.277f, 0.015f);
        /// <summary>面向左的 pivot。</summary>
        private static readonly Vector2 PlayerPivotLeft = new Vector2(0.660f, 0.015f);
        /// <summary>音效設定資產路徑。</summary>
        private const string AudioConfigPath = "Assets/Data/Config/AudioConfig.asset";
        /// <summary>音效設定欄位 ↔ 音檔路徑 ↔ 是否循環（見 SD-02 音效）。</summary>
        private static readonly (string field, string path, bool loop)[] AudioClips =
        {
            ("_jump", "Assets/Audio/SFX/sfx_jump.wav", false),
            ("_land", "Assets/Audio/SFX/sfx_land.wav", false),
            ("_harpoonThrow", "Assets/Audio/SFX/sfx_harpoon_throw.wav", false),
            ("_breathHold", "Assets/Audio/SFX/sfx_breath_hold.wav", false),
            ("_eat", "Assets/Audio/SFX/sfx_eat.wav", false),
            ("_harpoonHit", "Assets/Audio/SFX/sfx_harpoon_hit.wav", false),
            ("_enemyKill", "Assets/Audio/SFX/sfx_harpoon_hit.wav", false),
            ("_bossRoar", "Assets/Audio/SFX/sfx_boss_roar.wav", false),
            ("_sealComplete", "Assets/Audio/SFX/sfx_boss_roar.wav", false),
            ("_whisper", "Assets/Audio/SFX/sfx_whisper.wav", false),
            ("_jumpScare", "Assets/Audio/SFX/sfx_jumpscare.wav", false),
            ("_footsteps", "Assets/Audio/SFX/sfx_footsteps_water_loop.wav", true),
            ("_caveWind", "Assets/Audio/Ambience/amb_cave_wind.wav", true),
            ("_lowSanity", "Assets/Audio/Ambience/amb_low_sanity.wav", true),
            ("_breathLoop", "Assets/Audio/Ambience/amb_breath_hold.wav", true),
            ("_bossHall", "Assets/Audio/Ambience/amb_boss_hall.wav", true),
            ("_water", "Assets/Audio/Ambience/amb_water.wav", true),
        };

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
            // 先匯入新檔；若因此觸發腳本重新編譯，這次建置會用到舊程式 → 擋下來請使用者稍後再按
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("重建原型場景", "腳本正在編譯中，建出來會是舊版本。\n請等右下角轉圈結束後再執行一次。", "OK");
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
            // 先匯入外部新放進來的圖檔（例如剛複製的 HUD 圖），避免綁定時找不到資產
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            EnsureFolder("Assets/Scenes");
            InitShared();
            PrototypeMapLayout.EnsurePlaceholders();

            var data = CreateData();
            var harpoonPrefab = CreateHarpoonPrefab();
            BuildScene(data, harpoonPrefab);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[DrownedDream] 原型場景建立完成：" + ScenePath);
        }

        /// <summary>建立 Layer 與佔位圖（建場景與建關卡範本共用）。</summary>
        private static void InitShared()
        {
            EnsureFolder(PrefabDir);
            s_groundLayer = EnsureLayer("Ground", 6);
            s_playerLayer = EnsureLayer("Player", 7);
            s_enemyLayer = EnsureLayer("Enemy", 8);
            s_harpoonLayer = EnsureLayer("Harpoon", 9);
            s_square = EnsureShapeSprite(ArtDir, "Square", circle: false);
            s_circle = EnsureShapeSprite(ArtDir, "Circle", circle: true);
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
            /// <summary>灰寶箱（開出海草繃帶）。</summary>
            public GameObject ChestGray;
            /// <summary>藍灰寶箱（開出鎮靜藥丸）。</summary>
            public GameObject ChestBlueGray;
            /// <summary>黑寶箱（開出邪神雕像）。</summary>
            public GameObject ChestBlack;
            /// <summary>巡游魚怪。</summary>
            public EnemyData Fish;
            /// <summary>觸手。</summary>
            public EnemyData Tentacle;
            /// <summary>深淵之眼。</summary>
            public EnemyData Eye;
            /// <summary>玩家用零摩擦材質（避免黏牆）。</summary>
            public PhysicsMaterial2D NoFriction;
            /// <summary>音效設定。</summary>
            public AudioConfig Audio;
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
                ("_farBackground", AssetDatabase.LoadAssetAtPath<Texture2D>(FarBackgroundPath)),
                ("_columns", PrototypeMapLayout.Grid),
                ("_rows", PrototypeMapLayout.Grid),
                ("_pixelsPerUnit", PrototypeMapLayout.PixelsPerUnit),
                ("_maskCellPixels", PrototypeMapLayout.CellPixels)));
            if (d.Map.MapTexture == null || (d.Map.CollisionMask == null && !d.Map.CollisionFromMapAlpha))
            {
                // 圖片參照遺失（例如佔位圖重新產生）時補回佔位圖
                if (d.Map.MapTexture == null) Wire(d.Map, ("_mapTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(PrototypeMapLayout.MapPath)));
                if (d.Map.CollisionMask == null && !d.Map.CollisionFromMapAlpha) Wire(d.Map, ("_collisionMask", AssetDatabase.LoadAssetAtPath<Texture2D>(PrototypeMapLayout.MaskPath)));
                EditorUtility.SetDirty(d.Map);
            }
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

            d.Seal = ItemPrefab<SealItem>("SealFragment", "邪神雕像", new Color(1f, 0.85f, 0.3f), null);
            d.Pill = ItemPrefab<RecoveryItem>("Pill", "鎮靜藥丸", new Color(0.95f, 0.75f, 0.9f),
                so => Set(so, ("_sanityRestore", 20d), ("_hpRestore", 0d)));
            d.Medkit = ItemPrefab<RecoveryItem>("Medkit", "海草繃帶", new Color(0.5f, 1f, 0.6f),
                so => Set(so, ("_sanityRestore", 0d), ("_hpRestore", 20d)));
            ApplyItemArt(d.Seal, IdolSpritePath, 384, ("封印碎片", "邪神雕像"));
            ApplyItemArt(d.Pill, PillSpritePath, 320);
            ApplyItemArt(d.Medkit, BandageSpritePath, 320);

            var chestBase = ChestBasePrefab();
            d.ChestGray = ChestVariant(chestBase, "Chest_Gray", "gray", d.Medkit);
            d.ChestBlueGray = ChestVariant(chestBase, "Chest_BlueGray", "bluegray", d.Pill);
            d.ChestBlack = ChestVariant(chestBase, "Chest_Black", "black", d.Seal);

            d.Fish = Asset<EnemyData>($"{DataDir}/Enemies/FishMonster.asset", so => Set(so,
                ("_displayName", "巡游魚怪"),
                ("_behaviour", EnemyBehaviour.Patrol),
                ("_maxHits", 3),
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
                ("_detectRange", 7f), // 地刺：玩家在此範圍、橫戳範圍外時使用
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
                ("_maxHits", 3),
                ("_moveSpeed", 0f),
                ("_detectRange", 9f), // 凝視光束偵測距離
                ("_attackRange", 0f),
                ("_fearRange", 7f),
                ("_sanityDrainPerSecond", 8f),
                ("_attackDamage", 20f),
                ("_sanityRestoreOnKill", 25f),
                ("_color", new Color(0.95f, 0.9f, 0.4f)),
                ("_size", new Vector2(1.2f, 1.2f))));
            AssignEnemyArt(d.Eye, "eye_lid", 4, new[] { 2.2f, 0.05f, 0.08f, 0.05f }, null);      // 眼皮：張眼久一點，偶爾眨眼
            if (d.Eye.BaseSprite == null)
            {
                // 眼球分層：眼白 + 追視玩家的瞳孔（眼皮畫格疊在最上層）
                Wire(d.Eye,
                    ("_baseSprite", AssetDatabase.LoadAssetAtPath<Sprite>(ImportSprite($"{EnemyArtDir}/eye_base.png", 100, SpriteAlignment.Center))),
                    ("_lookSprite", AssetDatabase.LoadAssetAtPath<Sprite>(ImportSprite($"{EnemyArtDir}/eye_iris.png", 100, SpriteAlignment.Center))));
                EditorUtility.SetDirty(d.Eye);
            }
            AssignEnemyArt(d.Tentacle, "seabutterfly", 4, new[] { 0.12f, 0.12f, 0.12f, 0.12f }, // 拍翅
                AssetDatabase.LoadAssetAtPath<Sprite>(ImportSprite($"{EnemyArtDir}/tendril.png", 32, SpriteAlignment.LeftCenter)));
            if (d.Eye.BeamFx == null)
            {
                // 已存在的資產不會重設數值，特效欄位空著時補上
                var beamFx = AssetDatabase.LoadAssetAtPath<GameObject>(BeamFxPath);
                if (beamFx == null) Debug.LogWarning("[DrownedDream] 找不到光束特效：" + BeamFxPath);
                Wire(d.Eye, ("_beamFx", beamFx));
                EditorUtility.SetDirty(d.Eye);
            }

            string matPath = $"{DataDir}/Config/PlayerNoFriction.physicsMaterial2D";
            d.NoFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
            if (d.NoFriction == null)
            {
                d.NoFriction = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(d.NoFriction, matPath);
            }
            d.Audio = Asset<AudioConfig>(AudioConfigPath);
            FillAudioClips(d.Audio);
            return d;
        }

        /// <summary>音檔套用匯入設定，並補上 AudioConfig 裡還空著的 clip（已設定的不覆蓋，保留企劃替換）。</summary>
        private static void FillAudioClips(AudioConfig config)
        {
            var so = new SerializedObject(config);
            foreach (var (field, path, loop) in AudioClips)
            {
                var clip = EnsureAudioImport(path, loop);
                if (clip == null)
                {
                    Debug.LogWarning("[DrownedDream] 找不到音效：" + path);
                    continue;
                }
                var p = so.FindProperty($"{field}._clip");
                if (p != null && p.objectReferenceValue == null) p.objectReferenceValue = clip;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        /// <summary>音效匯入設定：單次音效解壓進記憶體（ADPCM，延遲低）；循環音壓縮存放（Vorbis）。</summary>
        private static AudioClip EnsureAudioImport(string path, bool loop)
        {
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer) return null;

            var settings = importer.defaultSampleSettings;
            var loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            var format = loop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            if (settings.loadType != loadType || settings.compressionFormat != format)
            {
                settings.loadType = loadType;
                settings.compressionFormat = format;
                settings.quality = 0.6f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
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

            var rooms = MapBuilder.Build(d.Map, s_groundLayer, s_square);
            var spawn = d.Map.LevelPrefab != null ? PlaceLevel(d.Map) : BuildPrototypeContent(d, rooms);
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
            canvasGo.AddComponent<GraphicRaycaster>(); // 滑鼠點擊 UI（封面按鈕）需要
            var effects = canvasGo.AddComponent<SanityScreenEffects>();
            Wire(effects, ("_globalLight", globalLight), ("_camera", gameCamera));
            var hud = canvasGo.AddComponent<HUD>();
            Wire(hud,
                ("_portrait", UISprite($"{HudArtDir}/hud_portrait.png", 256)),
                ("_portraitFrame", UISprite($"{HudArtDir}/hud_portrait_frame.png", 512)),
                ("_hpFrame", UISprite($"{HudArtDir}/hud_hp_frame.png", 1024)),
                ("_hpFill", UISprite($"{HudArtDir}/hud_hp_fill.png", 1024)),
                ("_sanFrame", UISprite($"{HudArtDir}/hud_san_frame.png", 1024)),
                ("_sanFill", UISprite($"{HudArtDir}/hud_san_fill.png", 1024)),
                ("_bubbleSprites", Enumerable.Range(1, 10).Select(i => (Object)UISprite($"{HudArtDir}/hud_bubble_{i:00}.png", 128)).ToArray()));
            canvasGo.AddComponent<InventoryPanel>();
            var story = canvasGo.AddComponent<StoryPanel>();
            var title = BuildTitleScreen(canvasGo);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var flow = new GameObject("GameFlow").AddComponent<GameFlow>();
            Wire(flow, ("_titleScreen", title), ("_storyPanel", story));

            BuildBgm();

            var audio = new GameObject("GameAudio").AddComponent<GameAudio>();
            Wire(audio, ("_config", d.Audio));
        }

        /// <summary>在 Canvas 上建立封面並綁定封面底圖與按鈕圖（檔案不存在時只警告）。</summary>
        private static TitleScreen BuildTitleScreen(GameObject canvasGo)
        {
            var title = canvasGo.AddComponent<TitleScreen>();
            Wire(title,
                ("_cover", UISprite(TitleCoverPath, 4096)),
                ("_startSprite", UISprite(StartButtonPath, 1024)),
                ("_quitSprite", UISprite(QuitButtonPath, 1024)));
            return title;
        }

        /// <summary>以 UI 用 Sprite 匯入並載入圖片（找不到時警告並回傳 null）。</summary>
        private static Sprite UISprite(string path, int maxSize)
        {
            ConfigureSprite(path, 100, SpriteAlignment.Center, maxSize, compressed: true);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning("[DrownedDream] 找不到 UI 圖：" + path);
            return sprite;
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

        /// <summary>
        /// 各區塊內容（局部座標，單位；地板頂端 y = 0.5）。
        /// 路線：上排 左→右 → 第 2 排 右→左 → 第 1 排 左→右 → 下排 右→左（Boss）。
        /// </summary>
        private static void BuildContent(DataSet d)
        {
            var root = new GameObject("Content").transform;
            const float floorItemY = 1.1f;   // 地板上的道具高度
            const float lowPlatY = 3.1f;      // 貼地方塊（頂 2.5）上的道具高度
            const float highPlatY = 5.1f;     // 高平台（頂 4.5）上的道具高度
            const float floorY = 0.5f;        // 地面高度（敵人腳底，F-ENM-00 怪物站在地面）
            // 一般區塊右側空地（貼地方塊 8 ~ 牆 15.5，扣掉魚怪半寬 0.7）：魚怪只在這段巡邏 / 追擊
            Vector2 FishZone(int col) => new Vector2(col * PrototypeMapLayout.RoomUnits + 8.7f, col * PrototypeMapLayout.RoomUnits + 14.8f);

            PlacePlatforms(root);

            // 上排（起點 (0,3) → 往右）。(0,3)、(3,3) 地板有洞（6.5~9.5 單位），不放平台
            MakeCheckpoint(root, PrototypeMapLayout.Local(0, 3, 2f, floorItemY));
            MakeChest(root, d.ChestBlueGray, PrototypeMapLayout.Local(0, 3, 12f, floorItemY));
            MakeEnemy(root, d.Fish, PrototypeMapLayout.Local(1, 3, 10f, floorY), FishZone(1));
            MakeEnemy(root, d.Fish, PrototypeMapLayout.Local(1, 3, 13.5f, floorY), FishZone(1));
            MakeChest(root, d.ChestBlack, PrototypeMapLayout.Local(1, 3, 11f, highPlatY));      // 封印道具 1
            MakeEnemy(root, d.Eye, PrototypeMapLayout.Local(2, 3, 12f, floorY));
            MakeEnemy(root, d.Eye, PrototypeMapLayout.Local(2, 3, 2f, floorY));
            MakeChest(root, d.ChestGray, PrototypeMapLayout.Local(2, 3, 11f, highPlatY));
            MakeHallucination(root, PrototypeMapLayout.Local(2, 3, 6f, 12f), 1);
            MakeCheckpoint(root, PrototypeMapLayout.Local(3, 3, 3f, floorItemY));
            MakeChest(root, d.ChestBlack, PrototypeMapLayout.Local(3, 3, 12.5f, floorItemY));    // 封印道具 2

            // 支線小房間 (0,2)：從起點地板洞掉下來，左側有階梯回去
            MakeChest(root, d.ChestBlack, PrototypeMapLayout.Local(0, 2, 12f, floorItemY));      // 封印道具 3
            MakeEnemy(root, d.Eye, PrototypeMapLayout.Local(0, 2, 10f, floorY));

            // 右側直井 (3,2)+(3,1)：從 (3,3) 掉下來會一路穿到 (3,0)；直井底部左側有觸手
            MakeEnemy(root, d.Tentacle, PrototypeMapLayout.Local(3, 1, 3f, floorY));

            // 下排（(3,0) → 往左）
            MakeChest(root, d.ChestBlack, PrototypeMapLayout.Local(3, 0, 1.5f, floorItemY));     // 封印道具 4
            MakeEnemy(root, d.Tentacle, PrototypeMapLayout.Local(3, 0, 3.5f, floorY));
            MakeEnemy(root, d.Tentacle, PrototypeMapLayout.Local(3, 0, 12f, floorY));            // 階梯方塊（13~15.5）左邊
            MakeEnemy(root, d.Fish, PrototypeMapLayout.Local(2, 0, 10f, floorY), FishZone(2));
            MakeEnemy(root, d.Fish, PrototypeMapLayout.Local(2, 0, 13.5f, floorY), FishZone(2));
            MakeChest(root, d.ChestBlueGray, PrototypeMapLayout.Local(2, 0, 11f, highPlatY));
            MakeCheckpoint(root, PrototypeMapLayout.Local(1, 0, 12f, floorItemY));
            MakeChest(root, d.ChestGray, PrototypeMapLayout.Local(1, 0, 6f, lowPlatY));
            MakeHallucination(root, PrototypeMapLayout.Local(1, 0, 9f, 12f), 2);
            // 憋氣屏障擋住往 (0,0) 的門
            MakeBreathGate(root, PrototypeMapLayout.Local(1, 0, 1.5f, 0.5f), new Vector2(0.5f, 15f), d.Audio.Water);
            MakeEnemy(root, d.Eye, PrototypeMapLayout.Local(0, 0, 12f, floorY));

            // (0,1)：從 (0,0) 爬上來，右側就是 Boss 房入口
            MakeCheckpoint(root, PrototypeMapLayout.Local(0, 1, 12f, floorItemY));
            MakeChest(root, d.ChestGray, PrototypeMapLayout.Local(0, 1, 3f, floorItemY));

            // 中央 Boss 房（2×2 打通）：Boss 站在正中央地面，祭壇在入口側
            float arenaFloor = PrototypeMapLayout.ArenaFloorY;
            float arenaX = PrototypeMapLayout.ArenaCenterX;
            var boss = MakeBoss(root, new Vector2(arenaX, arenaFloor));
            MakeAltar(root, boss, new Vector2(PrototypeMapLayout.BossArena.x * PrototypeMapLayout.RoomUnits + 3f, arenaFloor + 0.5f));
            // 第 5 個封印道具：第一次進 Boss 房後，從這些候選點（一般區塊地板）隨機出現
            MakeHiddenSealSpawner(root, d.ChestBlack, new[]
            {
                PrototypeMapLayout.Local(1, 3, 2f, floorItemY),
                PrototypeMapLayout.Local(2, 3, 9f, floorItemY),
                PrototypeMapLayout.Local(2, 0, 2f, floorItemY),
                PrototypeMapLayout.Local(1, 0, 10f, floorItemY),
            });
            MakeBossArea(root, boss, new Vector2(arenaX, arenaFloor + 8f));
        }

        /// <summary>
        /// 在地圖算好的位置放浮台：輪流使用隊友的 float1~6 Prefab，放在掛 PlatformGroup 的 Platforms 底下
        /// （執行時設成 Ground Layer + 單向平台）。找不到 Prefab 時用灰色方塊代替。
        /// </summary>
        private static void PlacePlatforms(Transform parent)
        {
            var group = new GameObject("Platforms", typeof(PlatformGroup)).transform;
            group.SetParent(parent, false);
            var prefabs = new System.Collections.Generic.List<GameObject>();
            for (int i = 1; i <= 6; i++)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PlatformPrefabDir}/float{i}.prefab");
                if (p != null) prefabs.Add(p);
            }
            if (prefabs.Count == 0) Debug.LogWarning("[DrownedDream] 找不到 float 平台 Prefab，改用佔位方塊：" + PlatformPrefabDir);

            var spots = PrototypeMapLayout.PlatformSpots;
            for (int i = 0; i < spots.Count; i++)
            {
                if (prefabs.Count > 0)
                {
                    Place(prefabs[i % prefabs.Count], group, spots[i]);
                    continue;
                }
                var go = new GameObject($"Platform{i}");
                go.transform.SetParent(group, false);
                go.transform.position = spots[i] + Vector2.up * 0.25f;
                go.transform.localScale = new Vector3(PrototypeMapLayout.PlatformWidth, 0.5f, 1f);
                MakeSprite(go, s_square, new Color(0.35f, 0.38f, 0.42f), 4);
                go.AddComponent<BoxCollider2D>();
            }
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

            // 有美術畫格：一個 SpriteRenderer（腳底對齊碰撞框底部），左右各一組圖不翻轉；沒有就用佔位方塊
            var art = LoadPlayerArt();
            Transform visual = null;
            Object[] renderers;
            SpriteRenderer artSr = null;
            if (art != null)
            {
                artSr = MakeSprite(Child(go.transform, "Sprite", new Vector2(0f, -0.7f)).gameObject, (Sprite)art[0][0], Color.white, 15);
                renderers = new Object[] { artSr };
            }
            else
            {
                visual = Child(go.transform, "Visual", Vector2.zero);
                var bodySr = MakeSprite(Child(visual, "Body", Vector2.zero).gameObject, s_square, new Color(0.85f, 0.75f, 0.55f), 15);
                bodySr.transform.localScale = new Vector3(0.75f, 1.5f, 1f);
                var maskSr = MakeSprite(Child(visual, "DivingMask", new Vector2(0.18f, 0.4f)).gameObject, s_square, new Color(0.4f, 0.85f, 1f), 16);
                maskSr.transform.localScale = new Vector3(0.4f, 0.3f, 1f);
                renderers = new Object[] { bodySr, maskSr };
            }

            var lantern = Child(go.transform, "Lantern", new Vector2(0f, 0.3f)).gameObject.AddComponent<Light2D>();
            lantern.lightType = Light2D.LightType.Point;
            lantern.pointLightOuterRadius = 7f;
            lantern.pointLightInnerRadius = 1f;
            lantern.intensity = 0.9f;
            lantern.color = new Color(1f, 0.9f, 0.7f);

            go.AddComponent<Player>(); // RequireComponent 會自動補齊其他玩家元件
            if (art != null)
            {
                Wire(go.AddComponent<PlayerAnimator>(), ("_renderer", artSr),
                    ("_walkRight", art[0]), ("_walkLeft", art[1]), ("_jumpRight", art[2]), ("_jumpLeft", art[3]));
            }

            Wire(go.GetComponent<PlayerStatus>(),
                ("_vitals", d.Vitals), ("_sanityConfig", d.Sanity), ("_movement", d.Movement), ("_harpoon", d.Harpoon),
                ("_requiredSeals", 5), ("_flashRenderers", renderers));
            Wire(go.GetComponent<PlayerMove>(), ("_groundMask", Mask(s_groundLayer)), ("_visual", visual)); // 有美術畫格時 visual = null（不翻轉）
            Wire(go.GetComponent<PlayerBreath>(), ("_config", d.Breath), ("_fadeRenderers", renderers));
            Wire(go.GetComponent<PlayerAttack>(), ("_harpoonPrefab", harpoonPrefab));
            return go;
        }

        /// <summary>
        /// 載入角色畫格（Assets/Art/Player/{walk,jump}_{r,l}_N.png）：回傳 [右走, 左走, 右跳, 左跳]，缺圖回傳 null。
        /// 圖由美術 2192×2156 原圖裁成同一框、縮成 1/4；pivot 在腳底、身體中心（右向身體偏左、左向偏右，魚槍朝前伸出）。
        /// </summary>
        private static Object[][] LoadPlayerArt()
        {
            var sets = new[] { ("walk_r", 6, PlayerPivotRight), ("walk_l", 6, PlayerPivotLeft), ("jump_r", 7, PlayerPivotRight), ("jump_l", 7, PlayerPivotLeft) };
            var result = new Object[sets.Length][];
            for (int s = 0; s < sets.Length; s++)
            {
                var (name, count, pivot) = sets[s];
                result[s] = new Object[count];
                for (int i = 0; i < count; i++)
                {
                    string path = $"{PlayerArtDir}/{name}_{i}.png";
                    ImportPivotSprite(path, PlayerPixelsPerUnit, pivot);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite == null)
                    {
                        Debug.LogWarning("[DrownedDream] 找不到角色畫格，改用佔位方塊：" + path);
                        return null;
                    }
                    result[s][i] = sprite;
                }
            }
            return result;
        }

        /// <summary>匯入為自訂 pivot 的 Sprite（設定不同才重新匯入）。</summary>
        private static void ImportPivotSprite(string path, int pixelsPerUnit, Vector2 pivot)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (importer.textureType == TextureImporterType.Sprite && settings.spritePixelsPerUnit == pixelsPerUnit &&
                settings.spriteAlignment == (int)SpriteAlignment.Custom && settings.spritePivot == pivot) return;

            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = pixelsPerUnit;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.filterMode = FilterMode.Bilinear;
            importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        // ───────────────────────── 物件工廠 ─────────────────────────

        /// <summary>建立存檔點。</summary>
        private static GameObject MakeCheckpoint(Transform parent, Vector2 pos)
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
            return go;
        }

        /// <summary>在場景放置寶箱 Prefab（itemPos = 原本道具的位置，道具中心比地面高 0.6，寶箱底部貼地）。</summary>
        private static void MakeChest(Transform parent, GameObject prefab, Vector2 itemPos)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = itemPos + Vector2.down * 0.6f;
        }

        /// <summary>建立最後一個封印道具的隨機出現點（itemPos 同 MakeChest，寶箱底部貼地）。</summary>
        private static GameObject MakeHiddenSealSpawner(Transform parent, GameObject chestPrefab, Vector2[] itemPositions)
        {
            var go = new GameObject("HiddenSealSpawner");
            go.transform.SetParent(parent);
            for (int i = 0; i < itemPositions.Length; i++)
            {
                var point = new GameObject($"Candidate{i + 1}").transform;
                point.SetParent(go.transform);
                point.position = itemPositions[i] + Vector2.down * 0.6f;
            }
            Wire(go.AddComponent<HiddenSealSpawner>(), ("_chestPrefab", chestPrefab));
            return go;
        }

        /// <summary>載入或建立寶箱基底 Prefab（TreasureChest + Visual 子物件；各顏色為其 Variant）。</summary>
        private static GameObject ChestBasePrefab()
        {
            string path = $"{PrefabDir}/Chests/TreasureChest.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            EnsureFolder($"{PrefabDir}/Chests");
            var go = new GameObject("TreasureChest");
            var sr = MakeSprite(Child(go.transform, "Visual", Vector2.zero).gameObject, ChestSprite("gray", "closed"), Color.white, 4);
            var chest = go.AddComponent<TreasureChest>();
            Wire(chest, ("_renderer", sr));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>載入或建立寶箱顏色 Variant（綁定關 / 開圖與內容物；已存在則保留企劃調整）。</summary>
        private static GameObject ChestVariant(GameObject basePrefab, string fileName, string color, GameObject item)
        {
            string path = $"{PrefabDir}/Chests/{fileName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            go.name = fileName;
            var closed = ChestSprite(color, "closed");
            go.GetComponentInChildren<SpriteRenderer>().sprite = closed;
            Wire(go.GetComponent<TreasureChest>(),
                ("_closedSprite", closed),
                ("_openSprite", ChestSprite(color, "open")),
                ("_itemPrefab", item.GetComponent<PickupItem>()));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>匯入並載入寶箱圖（pivot 底部中心，找不到時警告）。</summary>
        private static Sprite ChestSprite(string color, string state)
        {
            string path = $"{ChestArtDir}/chest_{color}_{state}.png";
            ConfigureSprite(path, ChestPixelsPerUnit, SpriteAlignment.BottomCenter, 512, compressed: true);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning("[DrownedDream] 找不到寶箱圖：" + path);
            return sprite;
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

        /// <summary>
        /// 把道具 Prefab 的佔位圓換成正式圖（已換過則略過）。pixelsPerUnit 決定場景中的大小；
        /// rename = (舊名, 新名)：顯示名稱仍是舊名時改成新名（企劃改過的名稱不動）。
        /// </summary>
        private static void ApplyItemArt(GameObject prefab, string spritePath, int pixelsPerUnit, (string from, string to)? rename = null)
        {
            ConfigureSprite(spritePath, pixelsPerUnit, SpriteAlignment.Center, 1024, compressed: true);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning("[DrownedDream] 找不到道具圖：" + spritePath);
                return;
            }

            string path = AssetDatabase.GetAssetPath(prefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            var sr = root.GetComponent<SpriteRenderer>();
            var item = root.GetComponent<PickupItem>();
            var so = new SerializedObject(item);
            var nameProp = so.FindProperty("_displayName");
            bool needRename = rename.HasValue && nameProp.stringValue == rename.Value.from;

            if (sr.sprite != sprite || needRename)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
                root.transform.localScale = Vector3.one;
                if (needRename)
                {
                    nameProp.stringValue = rename.Value.to;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        /// <summary>建立一般敵人：feet = 地面位置（碰撞框底部貼地）；territory = 活動範圍世界 X（左, 右），null = 不限制。</summary>
        private static GameObject MakeEnemy(Transform parent, EnemyData data, Vector2 feet, Vector2? territory = null)
        {
            var go = new GameObject($"Enemy_{data.name}") { layer = s_enemyLayer };
            go.transform.SetParent(parent);
            go.transform.position = feet + Vector2.up * (data.Size.y / 2f); // 碰撞框底部貼地
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = data.Size;
            col.isTrigger = true;
            SpriteRenderer sr;
            var frames = data.AnimFrames;
            if (data.BaseSprite != null)
            {
                // 分層：底層（眼白，受擊閃白用這層）→ 追視層（瞳孔）→ 畫格層（眼皮眨眼）
                sr = MakeSprite(Child(go.transform, "Visual", Vector2.zero).gameObject, data.BaseSprite, Color.white, 10);
                float scale = data.Size.y / data.BaseSprite.bounds.size.y;
                sr.transform.localScale = new Vector3(scale, scale, 1f);
                if (data.LookSprite != null)
                {
                    var look = MakeSprite(Child(sr.transform, "Look", Vector2.zero).gameObject, data.LookSprite, Color.white, 11);
                    look.gameObject.AddComponent<EnemyLook>().Init(data.LookRadius);
                }
                if (frames != null && frames.Length > 0 && frames[0] != null)
                {
                    var anim = MakeSprite(Child(sr.transform, "Anim", Vector2.zero).gameObject, frames[0], Color.white, 12);
                    Wire(anim.gameObject.AddComponent<SpriteFrameAnimator>(),
                        ("_frames", frames.Cast<Object>().ToArray()),
                        ("_durations", data.AnimDurations ?? new float[0]));
                }
            }
            else if (frames != null && frames.Length > 0 && frames[0] != null)
            {
                // 有畫格：原色顯示，依圖片高度縮放到碰撞框高度，逐格播放
                sr = MakeSprite(Child(go.transform, "Visual", Vector2.zero).gameObject, frames[0], Color.white, 10);
                float scale = data.Size.y / frames[0].bounds.size.y;
                sr.transform.localScale = new Vector3(scale, scale, 1f);
                Wire(sr.gameObject.AddComponent<SpriteFrameAnimator>(),
                    ("_frames", frames.Cast<Object>().ToArray()),
                    ("_durations", data.AnimDurations ?? new float[0]));
            }
            else
            {
                sr = MakeSprite(Child(go.transform, "Visual", Vector2.zero).gameObject,
                    data.Behaviour == EnemyBehaviour.Passive ? s_circle : s_square, data.Color, 10);
                sr.transform.localScale = new Vector3(data.Size.x, data.Size.y, 1f);
            }
            var status = go.AddComponent<EnemyStatus>();
            Wire(status, ("_data", data));
            var ai = go.AddComponent<EnemyAI>();
            Wire(ai, ("_renderer", sr), ("_groundMask", Mask(s_groundLayer)));
            if (territory.HasValue) Wire(ai, ("_minX", territory.Value.x), ("_maxX", territory.Value.y));
            return go;
        }

        /// <summary>Boss 整體縮放（外觀 + 判定一起放大；2026-10-04 調成 2）。</summary>
        private const float BossScale = 2f;

        /// <summary>建立 Boss（feet = 地面位置；碰撞框 4×6 × BossScale，底部貼地）。</summary>
        private static BossController MakeBoss(Transform parent, Vector2 feet)
        {
            var go = new GameObject("Boss") { layer = s_enemyLayer };
            go.transform.SetParent(parent);
            go.transform.localScale = new Vector3(BossScale, BossScale, 1f);
            go.transform.position = feet + Vector2.up * (3f * BossScale);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(4f, 6f);
            col.isTrigger = true;
            var sr = MakeBossVisual(go.transform);
            var boss = go.AddComponent<BossController>();
            var lightningFx = AssetDatabase.LoadAssetAtPath<GameObject>(LightningFxPath);
            if (lightningFx == null) Debug.LogWarning("[DrownedDream] 找不到落雷特效：" + LightningFxPath);
            Wire(boss, ("_renderer", sr), ("_projectileBlockMask", Mask(s_groundLayer)), ("_lightningFx", lightningFx));
            Wire(go.GetComponent<FearSource>(), ("_radius", 18f), ("_drainPerSecond", 4f)); // Boss 變大，恐懼範圍跟著放大
            return boss;
        }

        /// <summary>
        /// Boss 外觀：有 GIF 拆出的畫格（Assets/Art/Boss/boss_00~32.png）時逐格播放，否則用佔位橢圓。
        /// 圖片中心對齊碰撞框中心（碰撞框高 6），PPU 76 → 約 8.3×6 單位。
        /// </summary>
        private static SpriteRenderer MakeBossVisual(Transform boss)
        {
            var visual = Child(boss, "Visual", Vector2.zero).gameObject;
            var frames = new Sprite[BossFrameDurations.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                string path = $"{BossArtDir}/boss_{i:00}.png";
                ConfigureSprite(path, BossPixelsPerUnit, SpriteAlignment.Center, 1024, compressed: true);
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            if (frames[0] == null)
            {
                Debug.LogWarning("[DrownedDream] 找不到 Boss 畫格，改用佔位圖：" + BossArtDir);
                var placeholder = MakeSprite(visual, s_circle, new Color(0.25f, 0.45f, 0.35f), 8);
                placeholder.transform.localScale = new Vector3(4.5f, 6.5f, 1f);
                return placeholder;
            }

            var sr = MakeSprite(visual, frames[0], Color.white, 8);
            Wire(visual.AddComponent<SpriteFrameAnimator>(),
                ("_frames", frames.Cast<Object>().ToArray()),
                ("_durations", BossFrameDurations));
            return sr;
        }

        /// <summary>怪物畫格欄位空著時，補上 Assets/Art/Enemies/{prefix}_0~N.png（已設定的不動，保留企劃調整）。</summary>
        private static void AssignEnemyArt(EnemyData data, string prefix, int count, float[] durations, Sprite tendril)
        {
            if (data.AnimFrames != null && data.AnimFrames.Length > 0 && data.AnimFrames[0] != null) return;
            var frames = new Object[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(ImportSprite($"{EnemyArtDir}/{prefix}_{i}.png", 100, SpriteAlignment.Center));
                if (frames[i] == null)
                {
                    Debug.LogWarning($"[DrownedDream] 找不到怪物畫格：{EnemyArtDir}/{prefix}_{i}.png");
                    return;
                }
            }
            Wire(data, ("_animFrames", frames), ("_animDurations", durations));
            if (tendril != null) Wire(data, ("_tendrilSprite", tendril));
            EditorUtility.SetDirty(data);
        }

        /// <summary>設定圖片匯入為 Sprite 並回傳路徑。</summary>
        private static string ImportSprite(string path, int pixelsPerUnit, SpriteAlignment alignment)
        {
            ConfigureSprite(path, pixelsPerUnit, alignment, 512, compressed: true);
            return path;
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

        /// <summary>建立憋氣屏障（左下角位置 + 尺寸），並在底部附近放水聲（靠近才聽得到）。</summary>
        private static void MakeBreathGate(Transform parent, Vector2 bottomLeft, Vector2 size, LoopEntry water)
        {
            if (water != null && water.Clip != null)
            {
                var sound = new GameObject("BreathGateWater");
                sound.transform.SetParent(parent);
                sound.transform.position = bottomLeft + new Vector2(size.x / 2f, 2f);
                var emitter = sound.AddComponent<AmbientEmitter>();
                Wire(emitter, ("_clip", water.Clip), ("_volume", water.Volume));
            }

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
