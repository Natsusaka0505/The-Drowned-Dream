# Feature — 需求總覽

## 遊戲概要

| 項目 | 內容 |
|---|---|
| 遊戲名稱 | **The Drowned Dream**（Repo / 專案資料夾：`The-Drowned-Dream`） |
| Jam 主題 | Breath |
| 類型 | 橫向捲軸、海底洞窟探險 |
| 地圖 | 互連獨立地圖（參考銀河戰士） |
| 風格 | 克蘇魯 |
| 核心循環 | 探索洞窟 → 管理氧氣與 SAN → 躲避/擊殺生物 → 收集封印道具 → 封印 Boss |

文件結構說明見 [需求/README.md](../../README.md)。

## 需求文件索引

| # | 文件 | 內容 |
|---|---|---|
| 01 | [01-story](../../01-story/SA/01-story.md) | 角色與故事、結局 |
| 02 | [02-oxygen-breath](../../02-oxygen-breath/SA/02-oxygen-breath.md) | 氧氣、呼吸、憋氣（隱形） |
| 03 | [03-sanity](../../03-sanity/SA/03-sanity.md) | SAN 值系統 |
| 04 | [04-weapon-harpoon](../../04-weapon-harpoon/SA/04-weapon-harpoon.md) | 魚槍武器 |
| 05 | [05-inventory-items](../../05-inventory-items/SA/05-inventory-items.md) | 背包與道具 |
| 06 | [06-enemies](../../06-enemies/SA/06-enemies.md) | 場景生物 |
| 07 | [07-boss-seal](../../07-boss-seal/SA/07-boss-seal.md) | Boss 與封印 |
| 08 | [08-map](../../08-map/SA/08-map.md) | 地圖與場景 |
| 09 | [09-death-respawn](../../09-death-respawn/SA/09-death-respawn.md) | 死亡與復活 |

## 標記說明

- `[待確認]`：原始需求未明確，需企劃決定。實作時用可調參數給預設值。
- 所有數值皆為**可調參數**，文件內數字僅為建議預設值。

## 開發優先級（36 小時建議）

| 優先 | 項目 |
|---|---|
| P0 | 角色移動（平台跳躍）、HP、氧氣/呼吸、憋氣隱形、魚槍發射與回收、基本敵人、SAN 值、死亡復活 |
| P1 | 道具（回復道具、封印道具）、Boss 與封印、結局 |
| P2 | SAN 低時地圖變化、方向鍵錯亂、憋氣的地圖特殊 bonus、多區域地圖 |

## 操作（已確認）

| 動作 | 鍵盤 | 手把 |
|---|---|---|
| 移動 | A / D、← / → | 左搖桿、十字鍵 |
| 跳躍 | W / ↑ | South |
| 發射魚叉（朝面向） | Space | West |
| 往下穿過平台 | S / ↓（2026-10-04 新增；站在單向浮台上按住即穿過往下掉，實心地板不受影響） | 十字鍵下 |
| 憋氣 / 提早結束 | Q（2026-10-04 改） | RB |
| 互動（祭壇封印） | E `[待確認]` | North |
| 背包 | Tab / I `[待確認]` | Select |

移動、跳躍、發射為企劃指定；其餘按鍵尚未指定，為暫定值。
