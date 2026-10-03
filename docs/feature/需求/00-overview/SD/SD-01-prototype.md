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
| 精神錯亂（A/W/D 隨機替換、Space 延遲 / 沒射出） | `Player/PlayerConfusion` |
| 恐懼範圍 | `Enemy/FearSource` |
| Boss | `Enemy/BossController`、`Enemy/BossProjectile`、`Area/SealAltar` |
| 封印道具 | `Item/SealItem` |
| 地圖 | `World/Room`、`Editor/MapBuilder` |
| 憋氣機關 | `World/BreathBonusZone`、`World/BreathGate` |
| 低 SAN 效果 | `UI/SanityScreenEffects`、`World/SanityStageObject` |
| HUD / 背包 | `UI/HUD`、`UI/InventoryPanel`、`UI/UIFactory` |

## 資料資產

| 路徑 | 內容 |
|---|---|
| `Assets/Data/Config/MovementConfig` | 移動、跳躍、重力 |
| `Assets/Data/Config/VitalsConfig` | HP、氧氣、復活 |
| `Assets/Data/Config/BreathConfig` | 憋氣 |
| `Assets/Data/Config/SanityConfig` | SAN、分段、精神錯亂（機率、替換、發射延遲） |
| `Assets/Data/Config/HarpoonConfig` | 魚槍（拋物線） |
| `Assets/Data/Config/AudioConfig` | 音效 / 環境音（clip、音量、觸發門檻），見 [SD-02 音效](SD-02-audio.md) |
| `Assets/Data/Map/MapConfig` | 地圖圖、碰撞遮罩、切分、PPU |
| `Assets/Data/Enemies/*` | 巡游魚怪、觸手、深淵之眼 |
| `Assets/Prefabs/Items/*` | 封印碎片、鎮靜藥丸（SAN 20）、海草繃帶（HP 20） |

重建場景**不會覆蓋**已存在的資產與道具 Prefab。

## Editor 工具

| 選單 | 作用 |
|---|---|
| Drowned Dream → Build Prototype Scene | 重建 `Assets/Scenes/Prototype.unity`（地圖 + 內容 + 玩家 + UI） |
| Drowned Dream → Rebuild Map | 只重建目前場景的地圖（美術換圖後使用），其他物件不動 |

## 原型關卡配置（4×4，座標 = 欄, 列；列 0 在最下面）

路線為蛇行：第 3 列 左→右 ↓ 第 2 列 右→左 ↓ 第 1 列 左→右 ↓ 第 0 列 右→左。

| 列＼欄 | 0 | 1 | 2 | 3 |
|---|---|---|---|---|
| 3 | **起點**、存檔點、藥丸 | 魚怪 | 深淵之眼、**封印 #1**、幻覺 | 海草繃帶、地洞 ↓ |
| 2 | 存檔點、地洞 ↓ | 海草繃帶、深淵之眼、幻覺 | 魚怪、藥丸 | 觸手、階梯 |
| 1 | 階梯、**憋氣屏障** | **封印 #2**、魚怪 | 深淵之眼、海草繃帶 | 存檔點、地洞 ↓ |
| 0 | **Boss**、祭壇 | 存檔點、海草繃帶 | 魚怪、藥丸 | 觸手、**封印 #3**、階梯 |

地洞下方的區塊有之字階梯可以爬回上方。
