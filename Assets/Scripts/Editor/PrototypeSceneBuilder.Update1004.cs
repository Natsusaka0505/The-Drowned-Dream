using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using static DrownedDream.EditorTools.EditorBuildUtil;

namespace DrownedDream.EditorTools
{
    /// <summary>
    /// 2026-10-04 素材更新：新音效、眼花（深淵之眼新圖）、頭上的海兔、結局插圖 + Quit。
    /// 選單 Drowned Dream/Apply 10-04 Content Update 直接套用到目前開啟的場景（不重建，保留手動擺放的地圖）；
    /// 重建原型場景時也會呼叫同一組函式。
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        /// <summary>眼花畫格（正視、右上、左上、左下、右下）。</summary>
        public const string EyeFlowerPathFormat = "Assets/Art/Enemies/eyeflower_{0}.png";
        /// <summary>眼花 pivot（眼睛中心，裁切框內比例）。</summary>
        private static readonly Vector2 EyeFlowerPivot = new Vector2(0.498f, 0.518f);
        /// <summary>海兔畫格資料夾（seahare_walk_0~4、seahare_jump_0~7）。</summary>
        public const string SeaHareArtDir = "Assets/Art/SeaHare";
        /// <summary>海兔走動格數。</summary>
        private const int SeaHareWalkCount = 5;
        /// <summary>海兔跳躍格數。</summary>
        private const int SeaHareJumpCount = 8;
        /// <summary>結局插圖（ending_0~4：做夢、繼續做夢、驚醒、揉眼睛、床下的海兔）。</summary>
        public const string EndingImagePathFormat = "Assets/Art/Ending/ending_{0}.png";
        /// <summary>結局插圖張數。</summary>
        private const int EndingImageCount = 5;

        /// <summary>腳本載入後檢查一次：開著原型場景且玩家頭上還沒有海兔 → 自動套用（只會跑一次）。</summary>
        [InitializeOnLoadMethod]
        private static void AutoApplyContentUpdate1004()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (SceneManager.GetActiveScene().path != ScenePath) return;
                var player = Object.FindFirstObjectByType<Player>();
                if (player == null || player.GetComponentInChildren<SeaHare>(true) != null) return;
                ApplyContentUpdate1004();
            };
        }

        /// <summary>選單入口：把 10-04 素材套用到目前開啟的場景並存檔。</summary>
        [MenuItem("Drowned Dream/Apply 10-04 Content Update")]
        public static void ApplyContentUpdate1004()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("10-04 素材更新", "請先停止 Play 模式再執行。", "OK");
                return;
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (ScriptsOutOfDate(out string reason))
            {
                EditorUtility.DisplayDialog("10-04 素材更新", reason + "，請等右下角轉圈結束後再執行一次。", "OK");
                return;
            }

            // 資料資產：音效、眼花畫格
            FillAudioClips(Asset<AudioConfig>(AudioConfigPath));
            var eye = AssetDatabase.LoadAssetAtPath<EnemyData>($"{DataDir}/Enemies/AbyssEye.asset");
            if (eye != null) AssignEyeFlowerArt(eye);

            // 場景物件
            int eyes = eye != null ? RebuildLookFrameEnemies(eye) : 0;
            bool hare = EnsureSeaHare(Object.FindFirstObjectByType<Player>());
            bool ending = WireEnding(Object.FindFirstObjectByType<GameFlow>(), Object.FindFirstObjectByType<StoryPanel>());

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            string msg = $"音效：已補上\n眼花：更新 {eyes} 隻深淵之眼\n海兔：{(hare ? "已放到玩家頭上" : "找不到 Player")}\n結局插圖 / Quit：{(ending ? "已綁定" : "找不到 GameFlow / StoryPanel")}";
            Debug.Log("[DrownedDream] 10-04 素材更新完成\n" + msg);
            EditorUtility.DisplayDialog("10-04 素材更新", msg, "OK");
        }

        /// <summary>匯入眼花畫格（pivot 在眼睛中心）並寫進深淵之眼資料（已有 5 格時不動，保留企劃替換）。</summary>
        private static void AssignEyeFlowerArt(EnemyData eye)
        {
            if (eye.LookFrames != null && eye.LookFrames.Length >= 5 && eye.LookFrames[0] != null) return;
            var frames = new Object[5];
            for (int i = 0; i < frames.Length; i++)
            {
                string path = string.Format(EyeFlowerPathFormat, i);
                ImportPivotSprite(path, 100, EyeFlowerPivot);
                ShrinkTexture(path, 512);
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (frames[i] == null)
                {
                    Debug.LogWarning("[DrownedDream] 找不到眼花畫格：" + path);
                    return;
                }
            }
            Wire(eye, ("_lookFrames", frames));
            EditorUtility.SetDirty(eye);
        }

        /// <summary>場景中使用指定資料的敵人：外觀換成整張圖追視（刪掉舊的 Visual，重新綁定 EnemyAI 的 Renderer）。回傳更新數量。</summary>
        private static int RebuildLookFrameEnemies(EnemyData data)
        {
            if (data.LookFrames == null || data.LookFrames.Length < 5 || data.LookFrames[0] == null) return 0;
            int count = 0;
            foreach (var status in Object.FindObjectsByType<EnemyStatus>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (new SerializedObject(status).FindProperty("_data").objectReferenceValue != data) continue;
                var ai = status.GetComponent<EnemyAI>();
                var old = status.transform.Find("Visual");
                int order = 10;
                if (old != null)
                {
                    var oldSr = old.GetComponent<SpriteRenderer>();
                    if (oldSr != null) order = oldSr.sortingOrder;
                    Object.DestroyImmediate(old.gameObject);
                }
                var sr = MakeLookFrameVisual(status.transform, data, order);
                if (ai != null) Wire(ai, ("_renderer", sr));
                count++;
            }
            return count;
        }

        /// <summary>建立整張圖追視的外觀（Visual 子物件，高度 = LookFramesHeight）。</summary>
        private static SpriteRenderer MakeLookFrameVisual(Transform parent, EnemyData data, int order)
        {
            var frames = data.LookFrames;
            var sr = MakeSprite(Child(parent, "Visual", Vector2.zero).gameObject, frames[0], Color.white, order);
            float scale = data.LookFramesHeight / frames[0].bounds.size.y;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            sr.gameObject.AddComponent<EnemyLookFrames>().Init(frames);
            return sr;
        }

        /// <summary>在玩家頭上放海兔（已存在則只更新畫格）。回傳是否有找到玩家。</summary>
        private static bool EnsureSeaHare(Player player)
        {
            if (player == null) return false;
            var walk = LoadSeaHareFrames("walk", SeaHareWalkCount);
            var jump = LoadSeaHareFrames("jump", SeaHareJumpCount);
            if (walk == null || jump == null) return true;

            var hare = player.GetComponentInChildren<SeaHare>(true);
            if (hare == null)
            {
                var go = Child(player.transform, "SeaHare", new Vector2(0f, 0.62f)).gameObject;
                MakeSprite(go, (Sprite)walk[0], Color.white, 16); // 畫在玩家（15）前面
                hare = go.AddComponent<SeaHare>();
            }
            var mirror = player.transform.Find("Sprite");
            Wire(hare, ("_idleFrames", walk), ("_jumpFrames", jump),
                ("_mirrorAlpha", mirror != null ? mirror.GetComponent<SpriteRenderer>() : null));
            return true;
        }

        /// <summary>載入海兔畫格（pivot 底部置中），缺圖回傳 null。</summary>
        private static Object[] LoadSeaHareFrames(string name, int count)
        {
            var frames = new Object[count];
            for (int i = 0; i < count; i++)
            {
                string path = $"{SeaHareArtDir}/seahare_{name}_{i}.png";
                ConfigureSprite(path, 100, SpriteAlignment.BottomCenter, 512, compressed: true);
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (frames[i] != null) continue;
                Debug.LogWarning("[DrownedDream] 找不到海兔畫格：" + path);
                return null;
            }
            return frames;
        }

        /// <summary>綁定結局插圖（GameFlow._endingPages 前 5 頁）與結局 Quit 按鈕圖。回傳是否兩者都找到。</summary>
        private static bool WireEnding(GameFlow flow, StoryPanel story)
        {
            if (story != null) Wire(story, ("_quitSprite", UISprite(QuitButtonPath, 1024)));
            if (flow == null) return false;
            var so = new SerializedObject(flow);
            var pages = so.FindProperty("_endingPages");
            for (int i = 0; i < EndingImageCount && pages != null && i < pages.arraySize; i++)
            {
                var sprite = UISprite(string.Format(EndingImagePathFormat, i), 2048);
                pages.GetArrayElementAtIndex(i).FindPropertyRelative("_image").objectReferenceValue = sprite;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return story != null;
        }

        /// <summary>把貼圖最大尺寸壓到 maxSize（ImportPivotSprite 不管尺寸）。</summary>
        private static void ShrinkTexture(string path, int maxSize)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            if (importer.maxTextureSize == maxSize && importer.textureCompression == TextureImporterCompression.Compressed) return;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
    }
}
