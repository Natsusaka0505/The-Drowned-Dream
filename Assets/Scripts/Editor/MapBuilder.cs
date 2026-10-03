using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DrownedDream.EditorTools
{
    /// <summary>
    /// 由 MapConfig 產生地圖：背景 Sprite + 碰撞 Tilemap（由遮罩圖取樣）+ 4×4 Room 邊界。
    /// 美術更新地圖 / 遮罩後，用選單 Drowned Dream/Rebuild Map 只重建地圖，不動其他物件。
    /// </summary>
    internal static class MapBuilder
    {
        /// <summary>地圖根物件名稱。</summary>
        public const string RootName = "Map";
        /// <summary>碰撞 Tile 資產路徑。</summary>
        private const string TilePath = "Assets/Data/Map/CollisionTile.asset";

        /// <summary>選單：在目前場景重建地圖（保留其他物件）。</summary>
        [MenuItem("Drowned Dream/Rebuild Map")]
        public static void RebuildInOpenScene()
        {
            var config = AssetDatabase.LoadAssetAtPath<MapConfig>(PrototypeSceneBuilder.MapConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("重建地圖", "找不到 MapConfig，請先執行 Build Prototype Scene。", "OK");
                return;
            }

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            int ground = EditorBuildUtil.EnsureLayer("Ground", 6);
            var square = EditorBuildUtil.EnsureShapeSprite(PrototypeSceneBuilder.ArtDir, "Square", circle: false);
            Build(config, ground, square);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[DrownedDream] 地圖重建完成");
        }

        /// <summary>產生地圖根物件，回傳 4×4 Room（索引 [col, row]）。</summary>
        public static Room[,] Build(MapConfig config, int groundLayer, Sprite tileSprite)
        {
            var root = new GameObject(RootName);
            var grid = root.AddComponent<Grid>();
            float cellUnits = config.MaskCellPixels / (float)config.PixelsPerUnit;
            grid.cellSize = new Vector3(cellUnits, cellUnits, 0f);

            BuildFarBackground(config, root.transform);
            BuildBackground(config, root.transform);
            BuildCollision(config, root.transform, groundLayer, tileSprite);
            return BuildRooms(config, root.transform);
        }

        /// <summary>地圖圖片設為 Sprite（左下角對齊原點）並放到背景。</summary>
        private static void BuildBackground(MapConfig config, Transform root)
        {
            if (config.MapTexture == null) return;
            string path = AssetDatabase.GetAssetPath(config.MapTexture);
            EditorBuildUtil.ConfigureSprite(path, config.PixelsPerUnit, SpriteAlignment.BottomLeft);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            var bg = new GameObject("Background");
            bg.transform.SetParent(root, false);
            EditorBuildUtil.MakeSprite(bg, sprite, Color.white, -10);
        }

        /// <summary>遠景背景：不受光、排在最後面、視差跟隨攝影機（F-MAP-09）。</summary>
        private static void BuildFarBackground(MapConfig config, Transform root)
        {
            if (config.FarBackground == null) return;
            string path = AssetDatabase.GetAssetPath(config.FarBackground);
            EditorBuildUtil.ConfigureSprite(path, 100, SpriteAlignment.Center, 4096, compressed: true);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            var go = new GameObject("FarBackground");
            go.transform.SetParent(root, false);
            var size = MapSizeUnits(config);
            go.transform.position = new Vector3(size.x / 2f, size.y / 2f, 0f);
            var sr = EditorBuildUtil.MakeSprite(go, sprite, Color.white, -100);
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(EditorBuildUtil.SpriteUnlitMaterialPath);
            if (unlit != null) sr.sharedMaterial = unlit;
            var parallax = go.AddComponent<ParallaxBackground>();
            EditorBuildUtil.Wire(parallax, ("_mapMin", Vector2.zero), ("_mapSize", size), ("_follow", config.ParallaxFollow));
        }

        /// <summary>讀取遮罩圖，暗色格放碰撞 Tile，合併成 CompositeCollider2D。</summary>
        private static void BuildCollision(MapConfig config, Transform root, int groundLayer, Sprite tileSprite)
        {
            var go = new GameObject("Collision") { layer = groundLayer };
            go.transform.SetParent(root, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.enabled = false; // 碰撞層不顯示；除錯時可在 Inspector 打開
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var tileCollider = go.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            if (config.CollisionMask == null)
            {
                Debug.LogError("[DrownedDream] MapConfig 沒有碰撞遮罩圖");
                return;
            }

            var tile = EnsureTile(tileSprite);
            var mask = ReadTexture(config.CollisionMask);
            int cell = config.MaskCellPixels;
            int cellsX = mask.width / cell;
            int cellsY = mask.height / cell;
            var positions = new List<Vector3Int>();

            for (int cy = 0; cy < cellsY; cy++)
            {
                for (int cx = 0; cx < cellsX; cx++)
                {
                    var c = mask.GetPixel(cx * cell + cell / 2, cy * cell + cell / 2);
                    if (c.grayscale < config.WallThreshold && c.a > 0.5f) positions.Add(new Vector3Int(cx, cy, 0));
                }
            }
            Object.DestroyImmediate(mask);

            var tiles = new TileBase[positions.Count];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = tile;
            tilemap.SetTiles(positions.ToArray(), tiles);
            composite.GenerateGeometry();
        }

        /// <summary>依 4×4 切分建立 Room 邊界。</summary>
        private static Room[,] BuildRooms(MapConfig config, Transform root)
        {
            var rooms = new Room[config.Columns, config.Rows];
            var size = MapSizeUnits(config);
            float w = size.x / config.Columns;
            float h = size.y / config.Rows;

            for (int c = 0; c < config.Columns; c++)
            {
                for (int r = 0; r < config.Rows; r++)
                {
                    var go = new GameObject($"Room_{c}_{r}") { layer = 2 }; // Ignore Raycast
                    go.transform.SetParent(root, false);
                    var box = go.AddComponent<BoxCollider2D>();
                    box.isTrigger = true;
                    box.offset = new Vector2(c * w + w / 2f, r * h + h / 2f);
                    box.size = new Vector2(w, h);
                    var room = go.AddComponent<Room>();
                    EditorBuildUtil.Wire(room, ("_displayName", $"區塊 {c}-{r}"));
                    rooms[c, r] = room;
                }
            }
            return rooms;
        }

        /// <summary>地圖世界尺寸（以遮罩或地圖圖的像素換算）。</summary>
        private static Vector2 MapSizeUnits(MapConfig config)
        {
            var tex = config.CollisionMask != null ? config.CollisionMask : config.MapTexture;
            if (tex == null) return new Vector2(64f, 64f);
            return new Vector2(tex.width, tex.height) / config.PixelsPerUnit;
        }

        /// <summary>直接從檔案讀圖（不需開 Read/Write）。</summary>
        private static Texture2D ReadTexture(Texture2D asset)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(asset)));
            return tex;
        }

        /// <summary>載入或建立碰撞用 Tile。</summary>
        private static Tile EnsureTile(Sprite sprite)
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(TilePath);
            if (tile != null) return tile;
            EditorBuildUtil.EnsureFolder(Path.GetDirectoryName(TilePath)?.Replace('\\', '/'));
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = new Color(1f, 0f, 0f, 0.4f);
            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(tile, TilePath);
            EditorUtility.SetDirty(tile);
            return tile;
        }
    }
}
