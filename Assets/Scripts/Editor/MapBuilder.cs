using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DrownedDream.EditorTools
{
    /// <summary>
    /// 由 MapConfig 產生地圖：背景 Sprite + 碰撞 Tilemap（由遮罩圖取樣）+ 地形自動貼圖 + 4×4 Room 邊界。
    /// 美術更新地圖 / 遮罩後，用選單 Drowned Dream/Rebuild Map 只重建地圖，不動其他物件。
    /// </summary>
    internal static class MapBuilder
    {
        /// <summary>地圖根物件名稱。</summary>
        public const string RootName = "Map";
        /// <summary>碰撞 Tile 資產路徑。</summary>
        private const string TilePath = "Assets/Data/Map/CollisionTile.asset";
        /// <summary>地形素材設定資產路徑。</summary>
        public const string TerrainSetPath = "Assets/Data/Map/TerrainTileSet.asset";
        /// <summary>地形 / 裝飾 Tile 資產資料夾。</summary>
        private const string TerrainTileDir = "Assets/Data/Map/Terrain";
        /// <summary>地形切片 PNG 資料夾。</summary>
        public const string TerrainArtDir = "Assets/Art/Map/Terrain";
        /// <summary>3×3 地形檔名後綴（列優先，對應 TerrainTileSet.Terrain 索引）。</summary>
        private static readonly string[] TerrainNames = { "tl", "tc", "tr", "ml", "mc", "mr", "bl", "bc", "br" };
        /// <summary>地形圖塊略放大，蓋掉相鄰圖塊間的細縫。</summary>
        private const float TerrainOverlap = 1.02f;
        /// <summary>地形 Sorting Order。</summary>
        private const int TerrainOrder = -6;
        /// <summary>裝飾 Sorting Order。</summary>
        private const int DecorOrder = -5;
        /// <summary>同一排裝飾之間最少間隔格數（避免擠在一起）。</summary>
        private const int DecorSpacing = 2;

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
            EnsureTerrainSet(config);
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
            var solid = SampleSolid(config, out var platforms);
            BuildCollision(root.transform, groundLayer, tileSprite, solid);
            BuildPlatforms(root.transform, groundLayer, tileSprite, platforms);
            // 地形貼圖：牆與單向平台都要畫出來
            if (solid != null && platforms != null)
            {
                for (int x = 0; x < solid.GetLength(0); x++)
                {
                    for (int y = 0; y < solid.GetLength(1); y++) solid[x, y] |= platforms[x, y];
                }
            }
            // 用美術圖透明度當碰撞時，美術圖本身就是地形，不再自動貼地形圖塊
            if (!config.CollisionFromMapAlpha) BuildTerrain(config, root.transform, solid);
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
            var sr = EditorBuildUtil.MakeSprite(go, sprite, config.FarBackgroundTint, -100);
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(EditorBuildUtil.SpriteUnlitMaterialPath);
            if (unlit != null) sr.sharedMaterial = unlit;
            var parallax = go.AddComponent<ParallaxBackground>();
            EditorBuildUtil.Wire(parallax, ("_mapMin", Vector2.zero), ("_mapSize", size), ("_follow", config.ParallaxFollow));
        }

        /// <summary>
        /// 讀取碰撞來源，回傳每格是否實心（[x, y]，y = 0 為最下排）；沒有來源回傳 null。
        /// 勾選 CollisionFromMapAlpha 時用地圖美術圖的透明度（不透明 = 牆），否則用黑白遮罩圖（黑 = 牆）。
        /// </summary>
        private static bool[,] SampleSolid(MapConfig config, out bool[,] platforms)
        {
            platforms = null;
            bool fromAlpha = config.CollisionFromMapAlpha;
            var source = fromAlpha ? config.MapTexture : config.CollisionMask;
            if (source == null)
            {
                Debug.LogError(fromAlpha ? "[DrownedDream] MapConfig 沒有地圖美術圖，無法用透明度產生碰撞" : "[DrownedDream] MapConfig 沒有碰撞遮罩圖");
                return null;
            }

            var mask = ReadTexture(source);
            int cell = config.MaskCellPixels;
            var solid = new bool[mask.width / cell, mask.height / cell];
            platforms = new bool[solid.GetLength(0), solid.GetLength(1)];
            for (int cy = 0; cy < solid.GetLength(1); cy++)
            {
                for (int cx = 0; cx < solid.GetLength(0); cx++)
                {
                    var c = mask.GetPixel(cx * cell + cell / 2, cy * cell + cell / 2);
                    if (fromAlpha)
                    {
                        solid[cx, cy] = c.a > config.AlphaThreshold;
                        continue;
                    }
                    bool dark = c.grayscale < config.WallThreshold && c.a > 0.5f;
                    bool platform = dark && c.grayscale >= config.PlatformThreshold; // 深灰 = 單向平台
                    solid[cx, cy] = dark && !platform;
                    platforms[cx, cy] = platform;
                }
            }
            Object.DestroyImmediate(mask);
            return solid;
        }

        /// <summary>實心格放碰撞 Tile，合併成 CompositeCollider2D。</summary>
        private static void BuildCollision(Transform root, int groundLayer, Sprite tileSprite, bool[,] solid)
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
            if (solid == null) return;

            var tile = EnsureTile(tileSprite);
            var positions = new List<Vector3Int>();
            for (int cy = 0; cy < solid.GetLength(1); cy++)
            {
                for (int cx = 0; cx < solid.GetLength(0); cx++)
                {
                    if (solid[cx, cy]) positions.Add(new Vector3Int(cx, cy, 0));
                }
            }

            var tiles = new TileBase[positions.Count];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = tile;
            tilemap.SetTiles(positions.ToArray(), tiles);
            composite.GenerateGeometry();
        }

        /// <summary>
        /// 單向平台：另一個 Tilemap，碰撞合併後交給 PlatformEffector2D（只擋從上方落下，可從下方 / 側面穿過）。
        /// 同在 Ground Layer，玩家著地判定與魚叉都會碰到。
        /// </summary>
        private static void BuildPlatforms(Transform root, int groundLayer, Sprite tileSprite, bool[,] platforms)
        {
            if (platforms == null) return;
            var positions = new List<Vector3Int>();
            for (int cy = 0; cy < platforms.GetLength(1); cy++)
            {
                for (int cx = 0; cx < platforms.GetLength(0); cx++)
                {
                    if (platforms[cx, cy]) positions.Add(new Vector3Int(cx, cy, 0));
                }
            }
            if (positions.Count == 0) return;

            var go = new GameObject("OneWayPlatforms") { layer = groundLayer };
            go.transform.SetParent(root, false);
            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().enabled = false; // 只做碰撞，外觀由地形貼圖負責
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var tileCollider = go.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            effector.useSideFriction = false;

            var tile = EnsureTile(tileSprite);
            var tiles = new TileBase[positions.Count];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = tile;
            tilemap.SetTiles(positions.ToArray(), tiles);
            composite.GenerateGeometry();
        }

        // ───────────────────────── 地形自動貼圖（SD-02 A 方案） ─────────────────────────

        /// <summary>MapConfig 沒有地形素材時，用預設切片建立 TerrainTileSet 並綁上（已有則不動）。</summary>
        public static void EnsureTerrainSet(MapConfig config)
        {
            if (config.Terrain != null) return;
            string first = $"{TerrainArtDir}/terrain_{TerrainNames[0]}.png";
            if (!File.Exists(first)) return; // 沒有地形素材就維持只顯示地圖圖

            var set = EditorBuildUtil.Asset<TerrainTileSet>(TerrainSetPath, so =>
            {
                var terrain = new Sprite[TerrainNames.Length];
                for (int i = 0; i < terrain.Length; i++) terrain[i] = LoadSprite($"{TerrainArtDir}/terrain_{TerrainNames[i]}.png");
                EditorBuildUtil.Set(so,
                    ("_terrain", terrain),
                    ("_ceilingDecor", new[] { LoadSprite($"{TerrainArtDir}/decor_ceiling_stalactite.png") }),
                    ("_leftDecor", new[] { LoadSprite($"{TerrainArtDir}/decor_side_left.png") }),
                    ("_rightDecor", new[] { LoadSprite($"{TerrainArtDir}/decor_side_right.png") }));
            });
            EditorBuildUtil.Wire(config, ("_terrain", set));
            EditorUtility.SetDirty(config);
        }

        /// <summary>依實心格四鄰貼 3×3 地形圖塊，並在天花板 / 牆面放裝飾。</summary>
        private static void BuildTerrain(MapConfig config, Transform root, bool[,] solid)
        {
            var set = config.Terrain;
            if (set == null || solid == null || set.GetTerrain(1, 1) == null) return;

            float cellUnits = config.MaskCellPixels / (float)config.PixelsPerUnit;
            float ppu = set.GetTerrain(1, 1).texture.width / cellUnits; // 一張地形圖塊 = 一格
            int w = solid.GetLength(0);
            int h = solid.GetLength(1);
            // 地圖外視為實心（邊界不畫邊框）
            bool Solid(int x, int y) => x < 0 || y < 0 || x >= w || y >= h || solid[x, y];

            // 地形：上方空 → 上排、下方空 → 下排；左方空 → 左欄、右方空 → 右欄
            var terrainTiles = new Tile[9];
            for (int i = 0; i < 9; i++)
            {
                terrainTiles[i] = MakeTile($"Terrain_{TerrainNames[i]}", set.GetTerrain(i / 3, i % 3), ppu, SpriteAlignment.Center,
                    Matrix4x4.Scale(new Vector3(TerrainOverlap, TerrainOverlap, 1f)));
            }
            var terrainPos = new List<Vector3Int>();
            var terrainList = new List<TileBase>();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!solid[x, y]) continue;
                    int row = !Solid(x, y + 1) ? 0 : !Solid(x, y - 1) ? 2 : 1;
                    int col = !Solid(x - 1, y) ? 0 : !Solid(x + 1, y) ? 2 : 1;
                    terrainPos.Add(new Vector3Int(x, y, 0));
                    terrainList.Add(terrainTiles[row * 3 + col]);
                }
            }
            var terrainMap = MakeTilemap(root, "Terrain", TerrainOrder);
            terrainMap.SetTiles(terrainPos.ToArray(), terrainList.ToArray());

            // 裝飾：放在實心格旁邊的空格，用位移讓圖貼齊牆面 / 天花板
            float half = cellUnits / 2f;
            var scale = new Vector3(set.DecorScale, set.DecorScale, 1f);
            var ceiling = MakeDecorTiles("Ceiling", set.CeilingDecor, ppu, SpriteAlignment.TopCenter, Matrix4x4.TRS(new Vector3(0f, half, 0f), Quaternion.identity, scale));
            var left = MakeDecorTiles("Left", set.LeftDecor, ppu, SpriteAlignment.RightCenter, Matrix4x4.TRS(new Vector3(half, 0f, 0f), Quaternion.identity, scale));
            var right = MakeDecorTiles("Right", set.RightDecor, ppu, SpriteAlignment.LeftCenter, Matrix4x4.TRS(new Vector3(-half, 0f, 0f), Quaternion.identity, scale));

            var decorPos = new List<Vector3Int>();
            var decorList = new List<TileBase>();
            void Place(int x, int y, Tile[] pool, int salt)
            {
                decorPos.Add(new Vector3Int(x, y, 0));
                decorList.Add(pool[(int)(Hash(x, y, set.Seed, salt + 100) * pool.Length) % pool.Length]);
            }

            // 天花板：空格上方是實心、且左右上方也是實心（不放在轉角），同一排保持間隔
            if (ceiling.Length > 0)
            {
                for (int y = 0; y < h; y++)
                {
                    int last = int.MinValue;
                    for (int x = 0; x < w; x++)
                    {
                        if (solid[x, y] || !Solid(x, y + 1) || !Solid(x - 1, y + 1) || !Solid(x + 1, y + 1)) continue;
                        if (x - last < DecorSpacing || Hash(x, y, set.Seed, 1) >= set.CeilingChance) continue;
                        Place(x, y, ceiling, 1);
                        last = x;
                    }
                }
            }

            // 牆面：空格旁是實心牆（上下也是牆，避免放在薄平台邊），同一欄保持間隔
            for (int x = 0; x < w; x++)
            {
                int lastL = int.MinValue, lastR = int.MinValue;
                for (int y = 0; y < h; y++)
                {
                    if (solid[x, y]) continue;
                    // 右邊是牆 → 牆面朝左，裝飾往左突出
                    if (left.Length > 0 && Solid(x + 1, y) && Solid(x + 1, y + 1) && Solid(x + 1, y - 1)
                        && y - lastL >= DecorSpacing && Hash(x, y, set.Seed, 2) < set.SideChance)
                    {
                        Place(x, y, left, 2);
                        lastL = y;
                    }
                    // 左邊是牆 → 牆面朝右，裝飾往右突出
                    else if (right.Length > 0 && Solid(x - 1, y) && Solid(x - 1, y + 1) && Solid(x - 1, y - 1)
                        && y - lastR >= DecorSpacing && Hash(x, y, set.Seed, 3) < set.SideChance)
                    {
                        Place(x, y, right, 3);
                        lastR = y;
                    }
                }
            }
            var decorMap = MakeTilemap(root, "Decor", DecorOrder);
            decorMap.SetTiles(decorPos.ToArray(), decorList.ToArray());
        }

        /// <summary>建立只顯示用的 Tilemap（受光材質、指定排序）。</summary>
        private static Tilemap MakeTilemap(Transform root, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(EditorBuildUtil.SpriteLitMaterialPath);
            if (mat != null) renderer.sharedMaterial = mat;
            return tilemap;
        }

        /// <summary>為一組裝飾 Sprite 建立 Tile（略過空的欄位）。</summary>
        private static Tile[] MakeDecorTiles(string prefix, Sprite[] sprites, float ppu, SpriteAlignment pivot, Matrix4x4 transform)
        {
            var list = new List<Tile>();
            if (sprites == null) return list.ToArray();
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null) list.Add(MakeTile($"{prefix}_{i}", sprites[i], ppu, pivot, transform));
            }
            return list.ToArray();
        }

        /// <summary>設定 Sprite 匯入（PPU、pivot）並建立 / 更新對應的 Tile 資產（無碰撞）。</summary>
        private static Tile MakeTile(string name, Sprite sprite, float ppu, SpriteAlignment pivot, Matrix4x4 transform)
        {
            string spritePath = AssetDatabase.GetAssetPath(sprite);
            EditorBuildUtil.ConfigureSprite(spritePath, Mathf.RoundToInt(ppu), pivot);
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath); // 重新匯入後取最新的 Sprite

            string path = $"{TerrainTileDir}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                EditorBuildUtil.EnsureFolder(TerrainTileDir);
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            tile.flags = TileFlags.LockAll;
            tile.transform = transform;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>依格座標 + 種子產生 0~1 的固定亂數（每次重建結果相同）。</summary>
        private static float Hash(int x, int y, int seed, int salt)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093 ^ y * 19349663 ^ seed * 83492791 ^ salt * 2654435761u);
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        /// <summary>載入 PNG 的 Sprite（尚未設成 Sprite 時先設定匯入）。</summary>
        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            EditorBuildUtil.ConfigureSprite(path, 256, SpriteAlignment.Center);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>把矩形範圍內的多個區塊合併成一個 Room（保留左下角那個並放大邊界，其餘刪除）。</summary>
        public static Room MergeRooms(Room[,] rooms, RectInt area)
        {
            var keep = rooms[area.x, area.y];
            var box = keep.GetComponent<BoxCollider2D>();
            // 用 offset / size 計算（編輯模式下剛建立的碰撞框 bounds 可能還沒更新；地圖根物件在原點）
            var last = rooms[area.xMax - 1, area.yMax - 1].GetComponent<BoxCollider2D>();
            var min = box.offset - box.size / 2f;
            var max = last.offset + last.size / 2f;
            box.offset = (min + max) / 2f;
            box.size = max - min;
            for (int c = area.x; c < area.xMax; c++)
            {
                for (int r = area.y; r < area.yMax; r++)
                {
                    if (rooms[c, r] == keep) continue;
                    Object.DestroyImmediate(rooms[c, r].gameObject);
                    rooms[c, r] = keep;
                }
            }
            keep.gameObject.name = $"Room_{area.x}_{area.y}_Merged";
            EditorBuildUtil.Wire(keep, ("_displayName", "Boss 房"));
            return keep;
        }

        /// <summary>依 Columns × Rows 切分建立 Room 邊界。</summary>
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
        public static Vector2 MapSizeUnits(MapConfig config)
        {
            var tex = config.CollisionFromMapAlpha || config.CollisionMask == null ? config.MapTexture : config.CollisionMask;
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
