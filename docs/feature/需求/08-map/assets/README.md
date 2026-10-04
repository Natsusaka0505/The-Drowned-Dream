# 08-map — 地圖素材

共通規格見 [00-overview/assets](../../00-overview/assets/README.md)。

## 圖層（由後到前）

| 圖層 | 檔案 | 說明 |
|---|---|---|
| 遠景 | `Assets/Art/Background/background.png` | 氛圍背景，不碰撞，視差捲動；尺寸不限（目前 4299×3035），程式自動縮放到蓋滿畫面 |
| 地圖 | `map.png` | （選用）岩壁 / 地形美術，**可通行處必須透明**，否則會擋住遠景 |
| 地形 | `Assets/Art/Map/Terrain/*.png` | 依遮罩自動貼的 3×3 石板 + 裝飾，見下方「地形圖塊」 |
| 碰撞 | `map_mask.png` | 不顯示，只用來產生碰撞 |

## 交付檔案（必要）

| 檔案 | 尺寸 | 說明 |
|---|---|---|
| `map.png` | 2048×2048 | 整張地圖美術，4×4 區塊、每塊 512×512 |
| `map_mask.png` | 2048×2048 | 碰撞遮罩：**黑 = 牆 / 地板**、白 = 可通行。規則見 [SD-02](../SD/SD-02-map-tilemap.md#遮罩圖規則給美術) |

## 地形圖塊（2026-10-03，A 方案）

來源：美術交付的地板素材表（JPG，2000×2000），原檔存於 `ArtSource/terrain_sheet.jpg`，用 `Tools/slice_terrain_sheet.py` 切成透明 PNG。

| 檔案 | 用途 | 尺寸 |
|---|---|---|
| `terrain_tl/tc/tr/ml/mc/mr/bl/bc/br.png` | 3×3 地形（t/m/b = 上/中/下，l/c/r = 左/中/右） | 128×128，PPU 256（= 0.5 單位 = 1 碰撞格） |
| `decor_ceiling_stalactite.png` | 天花板垂吊鐘乳石 | 同比例縮放 |
| `decor_side_left.png` / `decor_side_right.png` | 牆面左 / 右側尖刺 | 同比例縮放 |

### 請美術補交 / 確認 `[待確認]`

- **PNG 透明背景**、每個零件一張（目前是程式從 JPG 去背，邊緣與深色陰影會有瑕疵）。
- 3×3 每塊**等大**（目前約 310 px 但不完全一致，切的時候被拉成正方形）。
- 素材表上方深色帶（V 形裂紋）、底部小突起、右欄三角形、巨石、火炬、斷裂石板的用途（地板頂面？斜坡？道具？）——目前未使用。

## 原始素材副本（`source/`）

2026-10-04 從 `ArtSource/` 複製，**僅供查閱**。程式、`Tools/` 腳本與其他文件的引用一律以原位置 `ArtSource/` 為準；換素材時改 `ArtSource/` 原檔，再同步這裡的副本。音效路徑相對 `Assets/Audio/`。

| 副本 | 原檔（引用位置） | 用途 |
|---|---|---|
| [`map_objects.png`](source/map_objects.png) | `ArtSource/map_objects.png` | 地圖物件表 → 寶箱 `Assets/Art/Chests/` |
| [`raw_horror_wind.mp3`](source/raw_horror_wind.mp3) | `ArtSource/Audio/raw_horror_wind.mp3` | 洞窟風聲 → `Ambience/amb_cave_wind.wav` |
| [`terrain_sheet.jpg`](source/terrain_sheet.jpg) | `ArtSource/terrain_sheet.jpg` | 地板素材表 → `Tools/slice_terrain_sheet.py` 切成 `Assets/Art/Map/Terrain/` |

## 遊戲圖片副本（`art/`）

2026-10-04 從 `Assets/Art/` 複製（保留原子資料夾結構），**僅供查閱**。Unity 場景、程式與其他文件的引用一律以原位置 `Assets/Art/` 為準；換圖時改 `Assets/Art/` 原檔，再同步這裡的副本。

| 副本 | 原檔（引用位置） | 張數 | 用途 |
|---|---|---|---|
| [`art/Background/*.png`](art/Background/) | `Assets/Art/Background/*.png` | 3 | 遠景背景 |
| [`art/Map/*.png`](art/Map/) | `Assets/Art/Map/*.png` | 2 | 地圖 / 碰撞遮罩 placeholder |
| [`art/Map/Terrain/*.png`](art/Map/Terrain/) | `Assets/Art/Map/Terrain/*.png` | 12 | 地形 3×3 與裝飾 |
| [`art/Map-rock/*.png`](art/Map-rock/) | `Assets/Art/Map-rock/*.png` | 6 | 岩石 / 牆 |
| [`art/Platforms/*.png`](art/Platforms/) | `Assets/Art/Platforms/*.png` | 19 | 浮動平台 |
| [`art/image/*.png`](art/image/) | `Assets/Art/image/*.png` | 2 | 浮動平台原圖 |
