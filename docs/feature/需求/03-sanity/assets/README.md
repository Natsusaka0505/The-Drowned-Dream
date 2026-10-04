# 03-sanity — 素材規格

（尚無）

## 原始素材副本（`source/`）

2026-10-04 從 `ArtSource/` 複製，**僅供查閱**。程式、`Tools/` 腳本與其他文件的引用一律以原位置 `ArtSource/` 為準；換素材時改 `ArtSource/` 原檔，再同步這裡的副本。音效路徑相對 `Assets/Audio/`。

| 副本 | 原檔（引用位置） | 用途 |
|---|---|---|
| [`raw_eerie_whisper.mp3`](source/raw_eerie_whisper.mp3) | `ArtSource/Audio/raw_eerie_whisper.mp3` | 精神錯亂預告低語 → `SFX/sfx_whisper.wav` |
| [`raw_horror_drone.mp3`](source/raw_horror_drone.mp3) | `ArtSource/Audio/raw_horror_drone.mp3` | 低 SAN 環境音 → `Ambience/amb_low_sanity.wav` |
| [`raw_jump_scare.mp3`](source/raw_jump_scare.mp3) | `ArtSource/Audio/raw_jump_scare.mp3` | SAN 掉段驚嚇 → `SFX/sfx_jumpscare.wav` |

## 遊戲圖片副本（`art/`）

2026-10-04 從 `Assets/Art/` 複製（保留原子資料夾結構），**僅供查閱**。Unity 場景、程式與其他文件的引用一律以原位置 `Assets/Art/` 為準；換圖時改 `Assets/Art/` 原檔，再同步這裡的副本。

| 副本 | 原檔（引用位置） | 張數 | 用途 |
|---|---|---|---|
| [`art/UI/HUD/hud_san_*.png`](art/UI/HUD/) | `Assets/Art/UI/HUD/hud_san_*.png` | 2 | SAN 框與填充條 |
