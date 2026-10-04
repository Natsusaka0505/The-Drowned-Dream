# 02-oxygen-breath — 素材規格

（尚無）

## 原始素材副本（`source/`）

2026-10-04 從 `ArtSource/` 複製，**僅供查閱**。程式、`Tools/` 腳本與其他文件的引用一律以原位置 `ArtSource/` 為準；換素材時改 `ArtSource/` 原檔，再同步這裡的副本。音效路徑相對 `Assets/Audio/`。

| 副本 | 原檔（引用位置） | 用途 |
|---|---|---|
| [`raw_ethereal_wind.mp3`](source/raw_ethereal_wind.mp3) | `ArtSource/Audio/raw_ethereal_wind.mp3` | 憋氣中空靈音 → `Ambience/amb_breath_hold.wav` |
| [`raw_underwater.mp3`](source/raw_underwater.mp3) | `ArtSource/Audio/raw_underwater.mp3` | 開始憋氣 → `SFX/sfx_breath_hold.wav` |
| [`raw_water.mp3`](source/raw_water.mp3) | `ArtSource/Audio/raw_water.mp3` | 憋氣屏障水聲 → `Ambience/amb_water.wav` |

## 遊戲圖片副本（`art/`）

2026-10-04 從 `Assets/Art/` 複製（保留原子資料夾結構），**僅供查閱**。Unity 場景、程式與其他文件的引用一律以原位置 `Assets/Art/` 為準；換圖時改 `Assets/Art/` 原檔，再同步這裡的副本。

| 副本 | 原檔（引用位置） | 張數 | 用途 |
|---|---|---|---|
| [`art/UI/HUD/hud_bubble_*.png`](art/UI/HUD/) | `Assets/Art/UI/HUD/hud_bubble_*.png` | 10 | 氧氣泡泡 ×10 |
