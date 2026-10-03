# SD-01 — 原型架構

## 原則

- 每個系統一個元件，系統間以 C# 事件 / `GameEvents` 溝通，方便多人並行。
- 所有數值放 ScriptableObject（`Assets/Data/`），企劃可直接在 Inspector 調整。
- 場景由程式產生（`Drowned Dream → Build Prototype Scene`），避免多人改同一個 scene 造成衝突。
- 每個 function 與變數都有一行註解。

## 程式對照

架構規範（Status + Action）見 [docs/core](../../../../core/README.md)。

| script 分類 | Status | Action |
|---|---|---|
| player | `Player/PlayerStatus` | `PlayerMove`、`PlayerAttack`、`PlayerBreath`、`PlayerPickup`、`PlayerEnding` |
| enemy | `Enemy/EnemyStatus` | `Enemy/EnemyAI` |
| area | `Area/BossArea`（IsFirstEntry） | `Area/BossArea`（偵測進出 Boss 房） |
| 回復道具 | `Item/RecoveryItem`（SAN / HP 回復值） | `RecoveryItem.Apply` |
| 武器 | `Weapon/Harpoon`（接觸場地、重力、飛行速度） | `Harpoon`（接觸 enemy / 場地） |
| camera | `Camera/GameCamera`（目標） | `SwitchToPlayer`、`SwitchToBossRoom` |

分類以外的系統：

| 需求 | 程式 |
|---|---|
| 流程 / 劇情 | `Core/GameFlow`、`UI/StoryPanel` |
| 全域事件 | `Core/GameEvents` |
| 玩家入口 / 輸入 | `Player/Player`、`Player/PlayerInputReader` |
| 死亡復活 | `Player/PlayerRespawn`、`World/Checkpoint` |
| 方向錯亂 | `Player/PlayerConfusion` |
| 恐懼範圍 | `Enemy/FearSource` |
| Boss | `Enemy/BossController`、`Enemy/BossProjectile`、`Area/SealAltar` |
| 封印道具 | `Item/SealItem` |
| 地圖 | `Editor/CaveGenerator`（隨機洞窟）、`Editor/MapBuilder`（碰撞 + 地形 Tilemap） |
| 憋氣機關 | `World/BreathBonusZone`、`World/BreathGate` |
| 低 SAN 效果 | `UI/SanityScreenEffects`、`World/SanityStageObject` |
| HUD / 背包 | `UI/HUD`、`UI/InventoryPanel`、`UI/UIFactory` |

## 資料資產

| 路徑 | 內容 |
|---|---|
| `Assets/Data/Config/MovementConfig` | 移動、跳躍、重力 |
| `Assets/Data/Config/VitalsConfig` | HP、氧氣、復活 |
| `Assets/Data/Config/BreathConfig` | 憋氣 |
| `Assets/Data/Config/SanityConfig` | SAN、分段、方向錯亂 |
| `Assets/Data/Config/HarpoonConfig` | 魚槍（拋物線） |
| `Assets/Data/Map/MapConfig` | 碰撞遮罩、PPU、地形素材 |
| `Assets/Data/Map/CaveGenConfig` | 洞窟生成參數（種子、尺寸、物件數量） |
| `Assets/Data/Map/TerrainTileSet` | 地形圖塊、裝飾、背景牆 |
| `Assets/Data/Enemies/*` | 巡游魚怪、觸手、深淵之眼 |
| `Assets/Prefabs/Items/*` | 封印碎片、鎮靜藥丸（SAN 30）、海草繃帶（HP 30） |

重建場景**不會覆蓋**已存在的資產與道具 Prefab。

## Editor 工具

| 選單 | 作用 |
|---|---|
| Drowned Dream → Build Prototype Scene | 依 `CaveGenConfig` 種子生成洞窟，重建 `Assets/Scenes/Prototype.unity`（地圖 + 內容 + 玩家 + UI） |
| Drowned Dream → Rebuild Map | 依現有遮罩只重建目前場景的地形（換地形素材後使用），不重新生成、其他物件不動 |

## 原型關卡配置

2026-10-03 起改為隨機洞窟，敵人 / 道具 / 存檔點由生成器自動擺放，見 [08-map SD-03](../../08-map/SD/SD-03-cave-generation.md)。
