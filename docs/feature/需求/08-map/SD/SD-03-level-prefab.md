# SD-03 — 由下到上的地圖：地形圖 + 關卡 Prefab

對應需求：[SA/08-map.md](../SA/08-map.md)「新地圖流程」（2026-10-04）

## 組成

| 層 | 來源 | 設定位置 |
|---|---|---|
| 遠景背景 | `Assets/Art/Background/background.png`（沿用） | `MapConfig.FarBackground` |
| 地形 | 美術繪製的地圖圖（只有地形）；可通行處透明 | `MapConfig.MapTexture` |
| 碰撞 | 地形圖**不透明處 = 牆 / 地板**（每 `MaskCellPixels` 取樣一次，透明度 > `AlphaThreshold`） | 勾選 `MapConfig.CollisionFromMapAlpha` |
| 平台 | `Assets/Prefabs/float/float1~6`（隊友製作） | 關卡 Prefab 的 `Platforms` 底下 |
| 怪物 / 寶箱 / 存檔點 / Boss / 玩家起點 | 零件 Prefab | 關卡 Prefab |

勾選 `CollisionFromMapAlpha` 時：不再自動貼地形圖塊（美術圖本身就是地形），也不使用碰撞遮罩圖。

## 關卡 Prefab

- 選單 **Drowned Dream/Create Level Template** 產生：
  - `Assets/Prefabs/Level/Level.prefab`：範本（玩家起點在最下方中央、6 個平台由下往上之字形、各一隻怪、三色寶箱、存檔點、Boss 區在地圖正中央，2026-10-04 改）。
  - `Assets/Prefabs/Level/Parts/`：`Enemy_Fish`、`Enemy_Tentacle`、`Enemy_Eye`、`Checkpoint`、`BossArena`（Boss + 封印祭壇 + BossArea，已互相綁定）、`PlayerSpawn`。已存在的零件不覆蓋。
- 範本的座標依目前 `MapConfig` 的地圖尺寸計算，只是起點，用 Prefab 模式自由搬動 / 刪除 / 複製。
- 寶箱用 `Assets/Prefabs/Chests/Chest_*`。

## 使用流程

1. 美術交地形圖 → 放進專案，`MapConfig.MapTexture` 指向它、勾選 `CollisionFromMapAlpha`、依尺寸設定 `PixelsPerUnit` 與 `Columns / Rows`。
2. 執行 Create Level Template（或沿用已有的 Level.prefab），在 Prefab 模式擺好平台與內容。
3. 把 Level.prefab 拖到 `MapConfig.LevelPrefab`。
4. 執行 **Build Prototype Scene**：放入地形 + 碰撞 + 關卡 Prefab，玩家放在 `PlayerSpawnPoint`。
5. `MapConfig.LevelPrefab` 留空 = 回到原本的程式配置（4×4 佔位地圖）。

## 平台 Layer

隊友的 `float1~6` 在 Default Layer，玩家站不上去。不改 Prefab 本身，而是在關卡 Prefab 的 `Platforms` 物件掛 `World/PlatformGroup`：執行時把底下所有物件設成 Ground Layer，並加上 `PlatformEffector2D` 變成**單向平台**（可從下方跳穿；`_oneWay` 可關）。另外 float1~6 的 Tilemap 上掛了 **Dynamic 剛體**（會受重力掉落、被推走），`PlatformGroup` 執行時一律改成 Static。原型地圖也用同一套（2026-10-04）。**新增平台時放在 `Platforms` 底下即可。**

## 敵人在手擺關卡的移動

手擺的怪物沒有設定活動範圍（`EnemyAI._minX / _maxX` = 0 表示不限），改由移動時往前方發射線檢查地形：前方有牆或平台側面就停下 / 折返。需要時仍可在 Inspector 設定活動範圍。
