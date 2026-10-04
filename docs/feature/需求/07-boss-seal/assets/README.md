# 07-boss-seal — 素材規格

（尚無）

## 原始素材副本（`source/`）

2026-10-04 從 `ArtSource/` 複製，**僅供查閱**。程式、`Tools/` 腳本與其他文件的引用一律以原位置 `ArtSource/` 為準；換素材時改 `ArtSource/` 原檔，再同步這裡的副本。音效路徑相對 `Assets/Audio/`。

| 副本 | 原檔（引用位置） | 用途 |
|---|---|---|
| [`raw_boss.mp3`](source/raw_boss.mp3) | `ArtSource/Audio/raw_boss.mp3` | Boss 咆哮 / 封印完成 → `SFX/sfx_boss_roar.wav` |
| [`raw_boss_homing.mp3`](source/raw_boss_homing.mp3) | `ArtSource/Audio/raw_boss_homing.mp3` | Boss 追蹤彈 → `SFX/sfx_boss_homing.wav` |
| [`raw_boss_lightning.mp3`](source/raw_boss_lightning.mp3) | `ArtSource/Audio/raw_boss_lightning.mp3` | Boss 落雷 → `SFX/sfx_boss_lightning.wav` |
| [`raw_ethereal_horror_bg.mp3`](source/raw_ethereal_horror_bg.mp3) | `ArtSource/Audio/raw_ethereal_horror_bg.mp3` | Boss 房環境音 → `Ambience/amb_boss_hall.wav` |
| [`raw_seal_chain.mp4`](source/raw_seal_chain.mp4) | `ArtSource/Audio/raw_seal_chain.mp4` | 啟動封印祭壇 → `SFX/sfx_seal_chain.wav` |

## 遊戲圖片副本（`art/`）

2026-10-04 從 `Assets/Art/` 複製（保留原子資料夾結構），**僅供查閱**。Unity 場景、程式與其他文件的引用一律以原位置 `Assets/Art/` 為準；換圖時改 `Assets/Art/` 原檔，再同步這裡的副本。

| 副本 | 原檔（引用位置） | 張數 | 用途 |
|---|---|---|---|
| [`art/Boss/*.png`](art/Boss/) | `Assets/Art/Boss/*.png` | 39 | Boss 待機畫格、岩石、鎖鏈、光暈 |
| [`art/Enemies/projectile_*.png`](art/Enemies/) | `Assets/Art/Enemies/projectile_*.png` | 5 | Boss 彈幕畫格（遊戲用 256px） |
| [`art/Weapon/Bullet/*.png`](art/Weapon/Bullet/) | `Assets/Art/Weapon/Bullet/*.png` | 5 | Boss 子彈原圖（每張約 6MB） |
| [`art/Props/altar.png`](art/Props/) | `Assets/Art/Props/altar.png` | 1 | 封印祭壇 |
