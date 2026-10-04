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
        public const string FarBackgroundPath = "Assets/Art/Background/background_far.png";
        /// <summary>遠景背景原圖（美術交付；background_far.png 由它模糊 + 降對比 + 暗部拉向深藍產生，避免岩石剪影被誤認成牆）。</summary>
        public const string FarBackgroundSourcePath = "Assets/Art/Background/background.png";
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
        /// <summary>Boss 腳下浮岩圖（依 float / terrain 色票產生）。</summary>
        public const string BossRockPath = "Assets/Art/Boss/boss_rock.png";
        /// <summary>鎖鏈正面環節圖。</summary>
        public const string ChainLinkFacePath = "Assets/Art/Boss/chain_link_a.png";
        /// <summary>鎖鏈側面環節圖。</summary>
        public const string ChainLinkEdgePath = "Assets/Art/Boss/chain_link_b.png";
        /// <summary>鎖鏈地錨圖。</summary>
        public const string ChainAnchorPath = "Assets/Art/Boss/chain_anchor.png";
        /// <summary>Boss 下方碎石骨堆圖。</summary>
        public const string BossRubblePath = "Assets/Art/Boss/boss_rubble.png";
        /// <summary>柔光圓點（綠霧粒子）。</summary>
        public const string SoftGlowPath = "Assets/Art/Boss/soft_glow.png";
        /// <summary>不受光的 Sprite 材質（粒子用）。</summary>
        private const string SpriteUnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        /// <summary>封印祭壇圖（美術交付，裁掉透明邊；沒有就用佔位方塊）。</summary>
        public const string AltarSpritePath = "Assets/Art/Props/altar.png";
        /// <summary>復活點圖（美術交付，裁掉透明邊；沒有就用佔位方塊）。</summary>
        public const string CheckpointSpritePath = "Assets/Art/Props/checkpoint.png";
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
        /// <summary>角色圖相對碰撞框中心的高度（碰撞框半高 0.7 + 邊緣 0.05，再往下壓一點讓腳踩進地面）。</summary>
        private const float PlayerSpriteOffsetY = -0.85f;
        /// <summary>魚叉圖（美術 Speargun.png 轉成水平、槍頭朝右）。</summary>
        public const string HarpoonSpritePath = "Assets/Art/Weapon/harpoon.png";
        /// <summary>魚槍圖示（背包的魚叉欄位）。</summary>
        public const string GunIconPath = "Assets/Art/UI/gun_icon.png";
        /// <summary>精靈圖（提示對話框與背包）。</summary>
        public const string ElfSpritePath = "Assets/Art/UI/Elf/elf.png";
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
            if (ScriptsOutOfDate(out string reason))
            {
                EditorUtility.DisplayDialog("重建原型場景", reason + "，現在建出來會是舊版本。\n請等右下角轉圈結束後再執行一次。", "OK");
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
            var farSource = AssetDatabase.LoadAssetAtPath<Texture2D>(FarBackgroundSourcePath);
            if (d.Map.FarBackground == null || d.Map.FarBackground == farSource)
            {
                // 還沒設定，或仍指向原圖 → 改用處理過的遠景（企劃改成別張圖時不動）
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
                ("_hoverHeight", 1f),   // 水母漂浮在空中
                ("_bobHeight", 0.35f),
                ("_size", new Vector2(1.6f, 1.6f))));

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
            AssignEnemyArt(d.Fish, "jellyfish", 6, new[] { 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f }, null); // 水母（取代紅色魚怪）
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
            ConfigureSprite(HarpoonSpritePath, 420, SpriteAlignment.Center, 512, compressed: true); // 512px ≈ 1.2 單位長
            var harpoonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HarpoonSpritePath);
            if (harpoonSprite != null)
            {
                MakeSprite(Child(visual, "Spear", Vector2.zero).gameObject, harpoonSprite, Color.white, 12); // 美術魚叉圖（槍頭朝右）
            }
            else
            {
                Debug.LogWarning("[DrownedDream] 找不到魚叉圖，改用佔位方塊：" + HarpoonSpritePath);
                var shaft = MakeSprite(Child(visual, "Shaft", Vector2.zero).gameObject, s_square, new Color(0.85f, 0.85f, 0.8f), 12);
                shaft.transform.localScale = new Vector3(0.9f, 0.1f, 1f);
                var tip = MakeSprite(Child(visual, "Tip", new Vector2(0.5f, 0f)).gameObject, s_square, new Color(0.7f, 0.75f, 0.8f), 12);
                tip.transform.localScale = new Vector3(0.2f, 0.22f, 1f);
            }

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
                ("_harpoonSprite", AssetDatabase.LoadAssetAtPath<Sprite>(HarpoonSpritePath)),
                ("_elfSprite", UISprite(ElfSpritePath, 256)),
                ("_portrait", UISprite($"{HudArtDir}/hud_portrait.png", 256)),
                ("_portraitFrame", UISprite($"{HudArtDir}/hud_portrait_frame.png", 512)),
                ("_hpFrame", UISprite($"{HudArtDir}/hud_hp_frame.png", 1024)),
                ("_hpFill", UISprite($"{HudArtDir}/hud_hp_fill.png", 1024)),
                ("_sanFrame", UISprite($"{HudArtDir}/hud_san_frame.png", 1024)),
                ("_sanFill", UISprite($"{HudArtDir}/hud_san_fill.png", 1024)),
                ("_bubbleSprites", Enumerable.Range(1, 10).Select(i => (Object)UISprite($"{HudArtDir}/hud_bubble_{i:00}.png", 128)).ToArray()));
            var inventory = canvasGo.AddComponent<InventoryPanel>();
            Wire(inventory,
                ("_harpoonIcon", UISprite(GunIconPath, 256)), // 背包用魚槍圖
                ("_sealIcon", AssetDatabase.LoadAssetAtPath<Sprite>(IdolSpritePath)),
                ("_hpIcon", AssetDatabase.LoadAssetAtPath<Sprite>(BandageSpritePath)),
                ("_sanityIcon", AssetDatabase.LoadAssetAtPath<Sprite>(PillSpritePath)),
                ("_elfSprite", UISprite(ElfSpritePath, 256)));
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
            // 祭壇靠左牆（寬 3，左緣離牆約 0.1），避開 Boss 左外側鎖鏈的地錨
            MakeAltar(root, boss, new Vector2(PrototypeMapLayout.BossArena.x * PrototypeMapLayout.RoomUnits + 2.1f, arenaFloor + 0.5f));
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
            var lengths = PrototypeMapLayout.PlatformLengths;
            for (int i = 0; i < spots.Count; i++)
            {
                float length = i < lengths.Count ? lengths[i] : PrototypeMapLayout.PlatformWidth;
                if (prefabs.Count > 0)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i % prefabs.Count], group);
                    inst.transform.position = spots[i];
                    ResizeFloat(inst, length);
                    SoftenFloat(inst);
                    continue;
                }
                var go = new GameObject($"Platform{i}");
                go.transform.SetParent(group, false);
                go.transform.position = spots[i] + Vector2.up * 0.25f;
                go.transform.localScale = new Vector3(length, 0.5f, 1f);
                MakeSprite(go, s_square, new Color(0.35f, 0.38f, 0.42f), 4);
                go.AddComponent<BoxCollider2D>();
            }
        }

        /// <summary>
        /// 調整 float 浮台實例的長度（單位）：重排 Tilemap 那一排圖塊（保留左右端圖塊，中間圖塊循環補足或刪減），
        /// 並把碰撞框寬度改成相同長度，中心不變。只改場景裡的實例（Prefab 覆寫），不動 Prefab 本身。
        /// </summary>
        private static void ResizeFloat(GameObject inst, float lengthUnits)
        {
            var tilemap = inst.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap == null) return;
            float cellUnits = tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize.x * inst.transform.lossyScale.x : 0.5f;
            int count = Mathf.Max(2, Mathf.RoundToInt(lengthUnits / Mathf.Max(0.01f, cellUnits)));

            // 讀出原本那一排圖塊（依 x 排序）
            tilemap.CompressBounds();
            var bounds = tilemap.cellBounds;
            var row = new System.Collections.Generic.List<UnityEngine.Tilemaps.TileBase>();
            int rowY = bounds.yMin;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                var t = tilemap.GetTile(new Vector3Int(x, rowY, 0));
                if (t != null) row.Add(t);
            }
            if (row.Count < 2) return;

            var left = row[0];
            var right = row[row.Count - 1];
            var middle = row.GetRange(1, row.Count - 2);
            tilemap.ClearAllTiles();
            int start = -count / 2; // 置中：格 start ~ start+count-1
            for (int i = 0; i < count; i++)
            {
                var tile = i == 0 ? left : i == count - 1 ? right : middle.Count > 0 ? middle[(i - 1) % middle.Count] : left;
                tilemap.SetTile(new Vector3Int(start + i, rowY, 0), tile);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(tilemap);

            var box = tilemap.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(count, box.size.y);
                box.offset = new Vector2(start + count / 2f, box.offset.y);
                PrefabUtility.RecordPrefabInstancePropertyModifications(box);
            }
        }

        /// <summary>柔化後的浮台圖存放資料夾（依 Prefab 名稱 + 格數快取；改了柔化參數要刪掉這個資料夾重建）。</summary>
        private const string SoftPlatformDir = "Assets/Art/Platforms";

        /// <summary>
        /// 浮台邊緣不規則柔化：把這個實例 Tilemap 用到的圖塊拼成一張圖，套上不規則的透明遮罩
        /// （兩端參差淡出、底部像岩石下緣、頂面只微微起伏以免看起來站不穩），存成 Sprite 取代 Tilemap 的顯示。
        /// 碰撞框不變。
        /// </summary>
        private static void SoftenFloat(GameObject inst)
        {
            var tilemap = inst.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap == null) return;
            tilemap.CompressBounds();
            var bounds = tilemap.cellBounds;
            int count = bounds.size.x;
            if (count <= 0) return;

            var source = PrefabUtility.GetCorrespondingObjectFromSource(inst);
            string key = $"{(source != null ? source.name : inst.name)}_{count}";
            string path = $"{SoftPlatformDir}/{key}.png";
            if (!File.Exists(path) && !BuildSoftStrip(tilemap, bounds, path)) return;

            ConfigureSprite(path, 100, SpriteAlignment.Center, 2048, compressed: true);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) return;

            // 圖塊範圍：x = xMin ~ xMax、y = yMin ~ yMin+1（tilemap 局部座標，一格 = 1）
            var soft = new GameObject("SoftVisual");
            soft.transform.SetParent(tilemap.transform, false);
            soft.transform.localPosition = new Vector3(bounds.xMin + count / 2f, bounds.yMin + 0.5f, 0f);
            var renderer = tilemap.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            var sr = MakeSprite(soft, sprite, Color.white, renderer != null ? renderer.sortingOrder + 1 : 1);
            if (renderer != null)
            {
                renderer.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        /// <summary>拼出圖塊長條並套不規則柔化遮罩，存成 PNG；成功回傳 true。</summary>
        private static bool BuildSoftStrip(UnityEngine.Tilemaps.Tilemap tilemap, BoundsInt bounds, string path)
        {
            int count = bounds.size.x;
            var first = tilemap.GetSprite(new Vector3Int(bounds.xMin, bounds.yMin, 0));
            if (first == null) return false;
            int tileW = Mathf.RoundToInt(first.rect.width);
            int tileH = Mathf.RoundToInt(first.rect.height);

            // 原圖不一定開 Read/Write，直接讀檔
            var sheetPath = AssetDatabase.GetAssetPath(first.texture);
            var sheet = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(sheet, File.ReadAllBytes(sheetPath))) return false;

            int w = count * tileW;
            var pixels = new Color[w * tileH];
            for (int i = 0; i < count; i++)
            {
                var sp = tilemap.GetSprite(new Vector3Int(bounds.xMin + i, bounds.yMin, 0));
                if (sp == null) continue;
                var r = sp.rect;
                var block = sheet.GetPixels((int)r.x, (int)r.y, tileW, tileH);
                for (int y = 0; y < tileH; y++)
                {
                    System.Array.Copy(block, y * tileW, pixels, y * w + i * tileW, tileW);
                }
            }
            Object.DestroyImmediate(sheet);

            // 不規則柔化遮罩（固定種子：同一個檔名每次結果相同）
            float seed = (path.GetHashCode() & 0xffff) * 0.37f;
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)tileW;
                float bottom = tileH * (0.08f + 0.32f * Mathf.PerlinNoise(seed + u * 0.9f, 1.3f)); // 下緣參差（像岩石底部）
                float top = tileH * (0.02f + 0.05f * Mathf.PerlinNoise(seed + u * 1.7f, 7.1f));    // 頂面微微起伏
                for (int y = 0; y < tileH; y++)
                {
                    float fromBottom = y;
                    float fromTop = tileH - 1 - y;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((fromBottom - bottom) / (tileH * 0.12f)));
                    a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((fromTop - top) / (tileH * 0.05f)));
                    // 兩端：淡出寬度隨高度不規則變化
                    float endW = tileW * (0.25f + 0.45f * Mathf.PerlinNoise(seed + y * 0.05f, 3.3f));
                    float fromEnd = Mathf.Min(x, w - 1 - x);
                    a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fromEnd / endW));
                    var c = pixels[y * w + x];
                    c.a *= a;
                    pixels[y * w + x] = c;
                }
            }

            var tex = new Texture2D(w, tileH, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            EnsureFolder(SoftPlatformDir);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return true;
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
                artSr = MakeSprite(Child(go.transform, "Sprite", new Vector2(0f, PlayerSpriteOffsetY)).gameObject, (Sprite)art[0][0], Color.white, 15);
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
            // pos 比地面高 0.6；有美術圖時底部貼地、高 2.2 單位；頂部眼睛打小範圍光（未啟用紅光、啟用後黃光）
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.6f);
            if (MakePropSprite(go.transform, CheckpointSpritePath, new Vector2(0f, -0.6f), 0f, 2.2f, Color.white, 3))
            {
                var light = MakePointLight(go.transform, "TopLight", new Vector2(0f, 1.2f), new Color(1f, 0.15f, 0.12f), 1.6f, 0.15f, 1f);
                Wire(go.AddComponent<Checkpoint>(), ("_renderer", go.GetComponentInChildren<SpriteRenderer>()), ("_activeColor", Color.white), ("_light", light));
            }
            else
            {
                var sr = MakeSprite(Child(go.transform, "Visual", Vector2.zero).gameObject, s_square, new Color(0.5f, 0.55f, 0.6f), 3);
                sr.transform.localScale = new Vector3(0.5f, 1.2f, 1f);
                Wire(go.AddComponent<Checkpoint>(), ("_renderer", sr));
            }
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
            go.transform.position = feet + Vector2.up * (data.Size.y / 2f + data.HoverHeight); // 碰撞框底部貼地（漂浮怪再往上 HoverHeight）
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

        /// <summary>Boss 腳底離地面的高度（下半身埋在浮岩後面）。</summary>
        private const float BossHoverHeight = 3f;
        /// <summary>浮岩大小（寬 × 高，單位）。</summary>
        private static readonly Vector2 BossRockSize = new Vector2(20f, 5f);
        /// <summary>浮岩頂面比 Boss 腳底高多少（浮岩畫在 Boss 前面，遮住下半部的空隙）。</summary>
        private const float BossRockCover = 3f;
        /// <summary>Boss 圖的底色（略暗，讓紅光打亮的地方對比更明顯）。</summary>
        private static readonly Color BossTint = new Color(0.78f, 0.76f, 0.8f);

        /// <summary>建立 Boss（feet = 房間地面位置）：Boss 站在浮岩上，周圍打紅綠光；碰撞框 4×6 × BossScale。</summary>
        private static BossController MakeBoss(Transform parent, Vector2 feet)
        {
            var go = new GameObject("Boss") { layer = s_enemyLayer };
            go.transform.SetParent(parent);
            go.transform.localScale = new Vector3(BossScale, BossScale, 1f);
            var bossFeet = feet + Vector2.up * BossHoverHeight;
            go.transform.position = bossFeet + Vector2.up * (3f * BossScale);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(4f, 6f);
            col.isTrigger = true;
            var sr = MakeBossVisual(go.transform);
            var boss = go.AddComponent<BossController>();
            var lightningFx = AssetDatabase.LoadAssetAtPath<GameObject>(LightningFxPath);
            if (lightningFx == null) Debug.LogWarning("[DrownedDream] 找不到落雷特效：" + LightningFxPath);
            Wire(boss, ("_renderer", sr), ("_projectileBlockMask", Mask(s_groundLayer)), ("_lightningFx", lightningFx), ("_hoverHeight", BossHoverHeight));
            Wire(go.GetComponent<FearSource>(), ("_radius", 18f), ("_drainPerSecond", 4f)); // Boss 變大，恐懼範圍跟著放大
            MakeBossRock(parent, bossFeet + Vector2.up * BossRockCover);
            MakeBossChains(parent, feet, bossFeet + Vector2.up * BossRockCover);
            MakeBossMist(parent, feet);
            MakeBossLights(parent, bossFeet);
            return boss;
        }

        /// <summary>Boss 腳下的浮岩（純外觀、無碰撞；top = 岩石頂面中央）。</summary>
        private static void MakeBossRock(Transform parent, Vector2 top)
        {
            var go = new GameObject("BossRock");
            go.transform.SetParent(parent);
            go.transform.position = top;
            ConfigureSprite(BossRockPath, 100, SpriteAlignment.TopCenter, 2048, compressed: true);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BossRockPath);
            if (sprite == null)
            {
                Debug.LogWarning("[DrownedDream] 找不到浮岩圖：" + BossRockPath);
                return;
            }
            MakeSprite(go, sprite, Color.white, 9); // 在 Boss（8）前面，遮住下半部
            go.transform.localScale = new Vector3(BossRockSize.x / sprite.bounds.size.x, BossRockSize.y / sprite.bounds.size.y, 1f);
        }

        /// <summary>鎖鏈：(浮岩頂面相對位置 → 地面相對位置)，左右對稱各兩條。</summary>
        private static readonly (Vector2 top, Vector2 ground)[] BossChains =
        {
            (new Vector2(-7.5f, -2.2f), new Vector2(-11.8f, 0f)), // 外側地錨往內收，讓出左側祭壇位置
            (new Vector2(-3.5f, -3.2f), new Vector2(-6f, 0f)),
            (new Vector2(3.5f, -3.2f), new Vector2(6f, 0f)),
            (new Vector2(7.5f, -2.2f), new Vector2(11.8f, 0f)),
        };
        /// <summary>鎖鏈環節大小（寬 × 高，單位）。</summary>
        private static readonly Vector2 ChainLinkSize = new Vector2(0.5f, 0.8f);
        /// <summary>鎖鏈環節間距（單位）。</summary>
        private const float ChainLinkSpacing = 0.58f;

        /// <summary>
        /// 封印鎖鏈 + 地面碎石骨堆（純外觀、無碰撞）：浮岩底下拉 4 條鎖鏈斜斜釘到地面，正面 / 側面環節交錯。
        /// floor = 房間地面中央、rockTop = 浮岩頂面中央。
        /// </summary>
        private static void MakeBossChains(Transform parent, Vector2 floor, Vector2 rockTop)
        {
            var root = new GameObject("BossChains").transform;
            root.SetParent(parent);
            root.position = floor;
            var face = LoadCenteredSprite(ChainLinkFacePath, SpriteAlignment.Center);
            var edge = LoadCenteredSprite(ChainLinkEdgePath, SpriteAlignment.Center);
            var anchor = LoadCenteredSprite(ChainAnchorPath, SpriteAlignment.BottomCenter);
            var rubble = LoadCenteredSprite(BossRubblePath, SpriteAlignment.BottomCenter);

            if (rubble != null)
            {
                var sr = MakeSprite(Child(root, "Rubble", Vector2.zero).gameObject, rubble, Color.white, 6);
                FitSprite(sr, 11f, 0f);
            }
            if (face == null || edge == null) return;

            for (int c = 0; c < BossChains.Length; c++)
            {
                var chain = Child(root, $"Chain{c}", Vector2.zero);
                Vector2 from = rockTop + BossChains[c].top;
                Vector2 to = floor + BossChains[c].ground + Vector2.up * 0.4f; // 接在地錨的環上
                Vector2 dir = to - from;
                int count = Mathf.Max(1, Mathf.RoundToInt(dir.magnitude / ChainLinkSpacing));
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f; // 環節圖是直的
                for (int i = 0; i <= count; i++)
                {
                    bool isFace = i % 2 == 0;
                    var link = Child(chain, $"Link{i}", Vector2.zero);
                    link.position = from + dir * (i / (float)count);
                    link.rotation = Quaternion.Euler(0f, 0f, angle);
                    var sr = MakeSprite(link.gameObject, isFace ? face : edge, Color.white, 7); // 在浮岩（9）與 Boss（8）後面
                    FitSprite(sr, isFace ? ChainLinkSize.x : ChainLinkSize.x * 0.4f, ChainLinkSize.y);
                }
                if (anchor != null)
                {
                    var a = Child(chain, "Anchor", Vector2.zero);
                    a.position = floor + BossChains[c].ground;
                    FitSprite(MakeSprite(a.gameObject, anchor, Color.white, 7), 1.1f, 0f);
                }
            }
        }

        /// <summary>Boss 下方緩緩上升的綠霧光點（粒子，不受光、半透明）。floor = 房間地面中央。</summary>
        private static void MakeBossMist(Transform parent, Vector2 floor)
        {
            var glow = LoadCenteredSprite(SoftGlowPath, SpriteAlignment.Center);
            if (glow == null) return;
            var go = new GameObject("BossMist");
            go.transform.SetParent(parent);
            go.transform.position = floor + Vector2.up * 0.3f;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startColor = new Color(0.35f, 1f, 0.5f, 0.35f);
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 8f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 0.2f, 0.1f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = fade;

            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.SetSprite(0, glow);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            renderer.sortingOrder = 10; // 浮岩前、玩家後
        }

        /// <summary>匯入為 Sprite（PPU 100、指定 pivot）並載入，缺圖時警告並回傳 null。</summary>
        private static Sprite LoadCenteredSprite(string path, SpriteAlignment alignment)
        {
            ConfigureSprite(path, 100, alignment, 2048, compressed: true);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning("[DrownedDream] 找不到 Boss 房裝飾圖：" + path);
            return sprite;
        }

        /// <summary>依目標寬 / 高縮放 Sprite（其中一個為 0 時等比縮放）。</summary>
        private static void FitSprite(SpriteRenderer sr, float width, float height)
        {
            var size = sr.sprite.bounds.size;
            float sx = width > 0f ? width / size.x : height / size.y;
            float sy = height > 0f ? height / size.y : sx;
            if (width <= 0f) sx = sy;
            sr.transform.localScale = new Vector3(sx, sy, 1f);
        }

        /// <summary>Boss 周圍光源（feet = Boss 腳底）：紅色為主（背光 + 左右兩盞），綠色為輔（浮岩下方兩盞）。</summary>
        private static void MakeBossLights(Transform parent, Vector2 feet)
        {
            var root = new GameObject("BossLights").transform;
            root.SetParent(parent);
            root.position = feet;
            var red = new Color(1f, 0.15f, 0.12f);
            var green = new Color(0.35f, 1f, 0.5f);
            // 內半徑小、強度高：中心亮、往外快速變暗，拉高明暗對比
            MakePointLight(root, "RedMain", new Vector2(0f, 8f), red, 2.6f, 1f, 11f);
            MakePointLight(root, "RedLeft", new Vector2(-7f, 6f), red, 2f, 0.5f, 6f);
            MakePointLight(root, "RedRight", new Vector2(7f, 6f), red, 2f, 0.5f, 6f);
            MakePointLight(root, "GreenLeft", new Vector2(-5f, -1.5f), green, 1.5f, 0.3f, 5f);
            MakePointLight(root, "GreenRight", new Vector2(5f, -1.5f), green, 1.5f, 0.3f, 5f);
        }

        /// <summary>建立一盞點光源（局部位置、顏色、強度、內外半徑）。</summary>
        private static Light2D MakePointLight(Transform parent, string name, Vector2 localPos, Color color, float intensity, float inner, float outer)
        {
            var light = Child(parent, name, localPos).gameObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = inner;
            light.pointLightOuterRadius = outer;
            return light;
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

            var sr = MakeSprite(visual, frames[0], BossTint, 8);
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
            // pos 為祭壇中心（底部 = pos.y - 0.5）；有美術圖時底部貼地、寬 3 單位
            if (!MakePropSprite(go.transform, AltarSpritePath, new Vector2(0f, -0.5f), 3f, 0f, Color.white, 3))
            {
                var sr = MakeSprite(Child(go.transform, "Visual", Vector2.zero).gameObject, s_square, new Color(0.75f, 0.65f, 0.4f), 3);
                sr.transform.localScale = new Vector3(2f, 1f, 1f);
            }
            MakePointLight(go.transform, "GreenLight", new Vector2(0f, 0.8f), new Color(0.35f, 1f, 0.5f), 1.8f, 0.3f, 2.5f); // 祭壇綠光
            var altar = go.AddComponent<SealAltar>();
            Wire(altar, ("_boss", boss));
        }

        /// <summary>
        /// 放一張底部置中的道具圖（Visual 子物件，localPos = 底部位置）；width / height 擇一大於 0 決定縮放。
        /// 圖不存在回傳 false（由呼叫端改用佔位圖）。
        /// </summary>
        private static bool MakePropSprite(Transform parent, string path, Vector2 localPos, float width, float height, Color color, int order)
        {
            ConfigureSprite(path, 100, SpriteAlignment.BottomCenter, 1024, compressed: true);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("[DrownedDream] 找不到道具圖，改用佔位方塊：" + path);
                return false;
            }
            var sr = MakeSprite(Child(parent, "Visual", localPos).gameObject, sprite, color, order);
            float scale = width > 0f ? width / sprite.bounds.size.x : height / sprite.bounds.size.y;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            return true;
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
