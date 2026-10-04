# The Drowned Dream

36 小時 Game Jam 作品，Jam 主題 **Breath**。
橫向捲軸 + 類銀河戰士（Metroidvania）互連地圖的海底洞窟探險遊戲，克蘇魯風格。

> 主角夢見海底洞窟深處沉睡著邪神的寶藏。在精靈（主角頭上的海兔）的引導下，他一路收集邪神雕像、管理氧氣與理智，
> 最後在祭壇前封印邪神——然後醒來。

## 核心玩法

探索洞窟 → 管理**氧氣**與 **SAN** → 躲避或擊殺生物 → 收集封印道具（邪神雕像）→ 啟動兩座祭壇封印 Boss

- **氧氣 / 憋氣**：憋氣時隱形、敵人看不到你，但氧氣會快速消耗。
- **SAN（理智）**：靠近怪物會下降，太低會出現幻覺、方向錯亂。
- **魚槍**：發射魚叉攻擊，需要回收。
- **死亡與復活**：回到最近的復活點。

## 操作

| 動作 | 鍵盤 | 手把 |
|---|---|---|
| 移動 | A / D、← / → | 左搖桿、十字鍵 |
| 跳躍 | W / ↑ | South |
| 發射魚叉 | Space | West |
| 往下穿過平台 | S / ↓ | 十字鍵下 |
| 憋氣 / 提早結束 | Q | RB |
| 互動（祭壇） | E | North |
| 背包 | Tab / I | Select |

## 開發環境

- Unity **6.3 LTS (6000.3.25f1)**（版本鎖定，勿升級）
- URP 2D、Input System
- 開啟場景：`Assets/Scenes/Prototype.unity`

### Editor 工具（選單 `Drowned Dream/`）

| 選單 | 用途 |
|---|---|
| Build Prototype Scene | 從零重建原型場景（會覆蓋場景，資料資產保留） |
| Apply 10-04 Content Update | 把 10-04 新素材（音效、眼花、海兔、結局插圖）套用到目前場景，不重建 |
| Rebuild Map | 重建地圖 |
| Create Level Template | 建立關卡範本 |

### 音效處理

原始音檔放 `ArtSource/Audio/`，執行 `python3 Tools/process_audio.py`（需要 numpy）轉成 `Assets/Audio/` 下的 WAV。

## 專案結構

```
Assets/
  Scripts/      執行期程式（Player / Enemy / Area / World / UI / Audio / Data / Core）與 Editor 工具
  Data/         ScriptableObject 數值設定（企劃可在 Inspector 調整）
  Art/ Audio/   美術與音效素材
  Scenes/       Prototype.unity
ArtSource/      原始素材（未處理）
Tools/          素材處理腳本
docs/
  core/         程式架構與規範
  feature/需求/ 各功能需求（原始需求 / SA / SD / assets）
```

## 文件

- 需求總覽：[docs/feature/需求/00-overview/SA/README.md](docs/feature/需求/00-overview/SA/README.md)
- 程式架構：[docs/core/README.md](docs/core/README.md)
