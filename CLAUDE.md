# CLAUDE.md

## 專案概要

遊戲名稱：**The Drowned Dream**（Repo：`The-Drowned-Dream`）。
36 小時 Game Jam 作品，Jam 主題 **Breath**。
橫向捲軸 + 類銀河戰士（Metroidvania）互連地圖的海底洞窟探險遊戲，克蘇魯風格。
本團隊負責軟體開發；企劃 / 美術 / 音效由其他成員負責。

## 鐵則（每次動手前必讀）

1. **先讀 `docs/core/`**。所有程式碼必須遵守其中的架構、命名、資料夾與模組規範。core 與本檔衝突時，以 core 為準。
2. **需求以 `docs/feature/需求/*/SA/` 為準**。實作某功能前先讀對應 feature 文件；需求有變動時同步更新文件。
3. feature 文件中標記 `[待確認]` 的項目，不得自行假設定案——先用可調參數（ScriptableObject / SerializeField）實作預設值，並回報需要誰決定。
4. 新增系統或改動架構時，先更新 `docs/core/`，再寫程式。
5. 不可以隨意commit程式碼
6. 一律用繁體中文回覆
7. 必須寫註解，以一行為主，function和變數都需要

## 開發環境

| | 本機 | 比賽會場 |
|---|---|---|
| OS | macOS 27 | Windows 11 |
| Unity | 6.3 LTS (6000.3.25f1) | 6.3 LTS (6000.3.25f1) |
| Editor | VS Code + Claude Code | VS Code |

跨平台注意：
- 路徑一律用 `/` 或 `Path.Combine`，禁止寫死 `\`。
- 檔名大小寫一致（macOS 不分大小寫，Windows/Git 會出問題）。
- 不使用任何僅限 macOS 的工具或 shell 指令作為建置流程的一部分。
- Unity 版本鎖定 6.3 LTS (6000.3.25f1)，不得升級。

## 程式規範（core 未定義前的預設）

- 語言：程式碼、識別字、commit 訊息用英文；文件與註解可用繁體中文。
- C# 命名：類別/方法/屬性 `PascalCase`，私有欄位 `_camelCase`，序列化欄位用 `[SerializeField] private`。
- 數值（氧氣、SAN、CD、傷害…）一律放 ScriptableObject 或 Inspector 可調欄位，方便企劃現場調整，禁止寫死。
- Jam 優先：能動 > 完美。避免過度抽象，但系統間以事件 / 介面解耦，方便多人並行開發。
- 每次改動盡量小、可單獨測試，避免大型 scene / prefab 合併衝突。

## 文件結構

```
docs/
  core/          系統核心程式框架說明（架構、模組、命名、資料流）
  feature/
    需求/
      00-overview/       遊戲概要、優先級、操作、原型架構、共通素材規格
      01-story/ … 09-*/  每個需求一個資料夾，底下固定四個子資料夾：
        原始需求/          企劃原文、口頭決議（原樣保存，不改寫）
        SA/                系統分析：整理後的需求規格（含 [待確認]）
        SD/                系統設計：實作方式、技術評估、對應的程式
        assets/            美術 / 音樂素材規格與交付清單
```
