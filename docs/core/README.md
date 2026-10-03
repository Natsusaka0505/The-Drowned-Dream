# Core — 系統核心程式框架

> 依據：[原始需求 2026-10-03 script 分類](../feature/需求/00-overview/原始需求/2026-10-03-script分類.md)

## 架構：Status + Action

每個遊戲物件類別拆成兩種腳本：

| 種類 | 職責 | 規則 |
|---|---|---|
| **Status** | 保存執行期數值（狀態），提供修改方法與變動事件 | 數值只能透過 Status 的方法修改；變動時發事件；被動計時（例如氧氣自然消耗）也在 Status 內 |
| **Action** | 行為：讀輸入 / 判斷 / 呼叫 Status 修改數值 | Action 之間不直接改彼此的內部欄位；需要協作時呼叫對方的公開方法或訂閱事件 |

可調整的預設值（上限、速度、範圍…）放 ScriptableObject（`Assets/Data/`），Status 在初始化時讀取。

## 類別對照

| 類別 | Status | Action |
|---|---|---|
| player | `PlayerStatus`：氧氣、目前 SAN、SAN 最大值、HP、憋氣狀態、呼吸 CD、移動速度、魚叉數、封印道具數 | `PlayerMove`（左右移動 + 跳躍）、`PlayerAttack`（投擲 / 撿魚叉）、`PlayerBreath`（憋氣潛行）、`PlayerPickup`（撿道具）、`PlayerEnding`（是否觸發結局） |
| enemy | `EnemyStatus`：被攻擊次數、移動速度、偵測範圍、攻擊範圍、移動狀態、是否偵測到玩家 | `EnemyAI`（自動左右移動、偵測玩家、攻擊判斷） |
| area | `BossArea`：是否第一次進 Boss 房 | `BossArea`：偵測玩家是否進 Boss 房 |
| 回復道具 | `RecoveryItem`：SAN 回復值、HP 回復值 | `RecoveryItem.Apply`：玩家數值回復 |
| 武器 | `Harpoon`：是否接觸場地、拋物線下墜（重力）、飛行速度 | `Harpoon`：接觸 enemy、接觸場地 |
| camera | `GameCamera`：目標（GameObject）、地圖範圍 | `GameCamera`：跟隨玩家（限制在地圖內）、切換到 Boss 廳（特寫） |

area / 回復道具 / 武器 / camera 規模小，Status 與 Action 寫在同一個腳本，以 `#region Status` / `#region Action` 分段。

分類以外但需求（SA）要求的系統：`PlayerRespawn`（死亡復活）、`PlayerConfusion`（低 SAN 方向錯亂）、`FearSource`（恐懼範圍）、`BossController`（Boss）、`Checkpoint`、`BreathGate`、`SanityStageObject`、`GameFlow`、`BgmPlayer`（背景音樂）、UI。

## 資料夾

```
Assets/Scripts/
  Core/      GameFlow、GameEvents、IDamageable
  Data/      ScriptableObject 定義（Config、EnemyData、MapConfig、TerrainTileSet、CaveGenConfig）
  Player/    PlayerStatus + 各 Action、Player（入口）、PlayerInputReader
  Enemy/     EnemyStatus、EnemyAI、FearSource、BossController、BossProjectile
  Area/      BossArea、SealAltar
  Item/      PickupItem（基底）、RecoveryItem、SealItem
  Weapon/    Harpoon
  Camera/    GameCamera
  World/     Checkpoint、BreathBonusZone、BreathGate、SanityStageObject
  Audio/     BgmPlayer
  UI/        HUD、InventoryPanel、StoryPanel、SanityScreenEffects、UIFactory
  Editor/    場景 / 地圖產生器（CaveGenerator 隨機洞窟、MapBuilder 碰撞 + 地形自動貼圖）
```

## 型別

- Status 的數值（氧氣、SAN、HP、CD、速度、範圍）一律 **double**。
- 計數（魚叉數、封印數、被攻擊次數）用 **int**；狀態旗標用 **bool**。
- 與 Unity API 互動（位置、速度、`Time.deltaTime`）時在呼叫端轉型：`(float)status.MoveSpeed`。
- ScriptableObject 的設定值維持 float（Inspector 友善），讀進 Status 時自動轉為 double。

## 溝通方式

- 玩家入口：`Player.Instance`，可取得 Status 與各 Action。
- 跨系統事件：`GameEvents`（提示訊息、死亡、復活、封印、流程狀態切換）。`BgmPlayer` 訂閱 `GameStateChanged`：開場放開場曲，進入遊玩後淡出 → 換探索曲。
- Status 變動事件：例如 `PlayerStatus.HpChanged`，UI 訂閱顯示。
- 可被魚叉命中的對象實作 `IDamageable.TakeHit()`。

## 命名與註解

- 類別 / 方法 / 屬性 `PascalCase`，私有欄位 `_camelCase`，序列化欄位 `[SerializeField] private`。
- **每個 function 與變數都要有一行註解**（`/// <summary>…</summary>`）。

## 場景

- 場景由 Editor 工具產生（`Drowned Dream → Build Prototype Scene`），避免多人同時改 scene。
- 音樂素材放 `Assets/Audio/`（BGM 在 `Assets/Audio/BGM/`），場景產生器會把 BGM 設為串流 + Vorbis 並綁到 `BGM` 物件的 `BgmPlayer`。
- 專案根目錄（Unity `Assets/` 外）：`ArtSource/` 放美術原始檔（不匯入 Unity），`Tools/` 放離線處理腳本（Python，跨平台，不屬於建置流程）。
- 地圖由 `CaveGenerator` 依 `CaveGenConfig` 種子隨機生成連通洞窟（128×96 單位，不分區塊），再由 `MapBuilder` 依碰撞遮罩產生碰撞與地形 Tilemap，見 [08-map SD-03](../feature/需求/08-map/SD/SD-03-cave-generation.md)、[SD-02](../feature/需求/08-map/SD/SD-02-map-tilemap.md)。
