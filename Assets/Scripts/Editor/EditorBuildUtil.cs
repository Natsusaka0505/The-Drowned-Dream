using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DrownedDream.EditorTools
{
    /// <summary>Editor 建置工具共用函式：資料夾、Layer、資產、序列化欄位寫入。</summary>
    internal static class EditorBuildUtil
    {
        /// <summary>URP 2D 預設受光 Sprite 材質路徑。</summary>
        public const string SpriteLitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

        /// <summary>遞迴建立資料夾（已存在則略過）。</summary>
        public static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>確保指定名稱的 Layer 存在，回傳 Layer 索引。</summary>
        public static int EnsureLayer(string name, int preferred)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == name) return i;
            }
            for (int i = preferred; i < layers.arraySize; i++)
            {
                var p = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(p.stringValue)) continue;
                p.stringValue = name;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
            throw new InvalidOperationException($"沒有空的 Layer 可放 {name}");
        }

        /// <summary>載入或建立 ScriptableObject 資產；已存在時不覆蓋（保留企劃調整）。</summary>
        public static T Asset<T>(string path, Action<SerializedObject> init = null) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            if (init != null)
            {
                var so = new SerializedObject(asset);
                init(so);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>寫入元件的序列化欄位。</summary>
        public static void Wire(Object target, params (string name, object value)[] values)
        {
            var so = new SerializedObject(target);
            Set(so, values);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>依值的型別寫入 SerializedObject 欄位。</summary>
        public static void Set(SerializedObject so, params (string name, object value)[] values)
        {
            foreach (var (name, value) in values)
            {
                var p = so.FindProperty(name);
                if (p == null)
                {
                    Debug.LogError($"[DrownedDream] 找不到欄位 {name}（{so.targetObject.GetType().Name}）");
                    continue;
                }

                switch (value)
                {
                    case null: p.objectReferenceValue = null; break;
                    case string s: p.stringValue = s; break;
                    case bool b: p.boolValue = b; break;
                    case float f: p.floatValue = f; break;
                    case double db: p.doubleValue = db; break;
                    case Enum e: p.enumValueIndex = Convert.ToInt32(e); break;
                    case LayerMask m: p.intValue = m.value; break;
                    case int i: p.intValue = i; break;
                    case Color c: p.colorValue = c; break;
                    case Vector2 v: p.vector2Value = v; break;
                    case Object[] arr:
                        p.arraySize = arr.Length;
                        for (int k = 0; k < arr.Length; k++) p.GetArrayElementAtIndex(k).objectReferenceValue = arr[k];
                        break;
                    case Object o:
                        p.objectReferenceValue = o;
                        if (o != null && p.objectReferenceValue == null)
                        {
                            Debug.LogError($"[DrownedDream] 欄位 {name} 綁定失敗（{o.name} 型別不符或已銷毀）");
                        }
                        break;
                    default:
                        Debug.LogError($"[DrownedDream] 不支援的型別 {value.GetType()}（{name}）");
                        break;
                }
            }
        }

        /// <summary>產生白色方塊 / 圓形佔位 Sprite（32px = 1 單位）。</summary>
        public static Sprite EnsureShapeSprite(string folder, string name, bool circle)
        {
            string path = $"{folder}/{name}.png";
            if (!File.Exists(path))
            {
                EnsureFolder(folder);
                const int size = 32;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool inside = !circle || Vector2.Distance(new Vector2(x, y), center) <= size / 2f - 0.5f;
                        tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                    }
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                ConfigureSprite(path, size, SpriteAlignment.Center);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>將圖片設定為 Sprite（指定 PPU 與 pivot）。</summary>
        public static void ConfigureSprite(string path, int pixelsPerUnit, SpriteAlignment alignment)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool dirty = importer.textureType != TextureImporterType.Sprite
                         || settings.spritePixelsPerUnit != pixelsPerUnit
                         || settings.spriteAlignment != (int)alignment
                         || importer.maxTextureSize < 2048;
            if (!dirty) return;

            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = pixelsPerUnit;
            settings.spriteAlignment = (int)alignment;
            settings.filterMode = FilterMode.Bilinear;
            importer.SetTextureSettings(settings);
            importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, 2048);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        /// <summary>建立子物件（繼承父物件 Layer）。</summary>
        public static Transform Child(Transform parent, string name, Vector2 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.gameObject.layer = parent.gameObject.layer;
            return t;
        }

        /// <summary>加上 SpriteRenderer（使用受光材質）。</summary>
        public static SpriteRenderer MakeSprite(GameObject go, Sprite sprite, Color color, int order)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(SpriteLitMaterialPath);
            if (mat != null) sr.sharedMaterial = mat;
            return sr;
        }

        /// <summary>單一 Layer 的 LayerMask。</summary>
        public static LayerMask Mask(int layer) => 1 << layer;
    }
}
