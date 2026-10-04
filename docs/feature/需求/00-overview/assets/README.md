# assets — 共通美術 / 音樂素材規格

美術與音樂的交付規格與清單。實際檔案放在 Unity 專案的 `Assets/Art/`、`Assets/Audio/`。

## 共通規格

| 項目 | 規格 |
|---|---|
| 圖片格式 | PNG（透明背景） |
| 像素密度 | 32 px = 1 單位（PPU 32） |
| 檔名 | 英文小寫 + 底線，例如 `enemy_fish_idle.png`（Windows / macOS 大小寫一致） |
| 音效格式 | WAV（短音效）、OGG（BGM） |

## 地圖

見 [08-map/assets/](../../08-map/assets/README.md)。

## 角色與物件（建議尺寸）

| 素材 | 尺寸（px） | 備註 |
|---|---|---|
| 主角 | 約 24×48 | 2026-10-04 已交付：左 / 右走路各 6 格、左 / 右跳躍各 7 格（原圖 2192×2156，裁成同一框後縮 1/4，放在 `Assets/Art/Player/{walk,jump}_{r,l}_N.png`）。站立暫用走路第 1 格。`動畫.clip` 是 Clip Studio Paint 檔，Unity 無法使用。`[待確認]` 請美術補：站立、下墜、憋氣、受傷、發射；右跳原檔命名為 2~7 + 5a、左跳為 0001~0007，建議統一 |
| 魚叉 | 約 32×8 | 飛行 / 插住 |
| 巡游魚怪 | 約 45×26 | 巡邏、衝撞 |
| 觸手（海蝶） | 約 26×64 | 2026-10-04 程式依美術參考圖繪製的暫用圖：海蝶拍翅 4 格 `Assets/Art/Enemies/seabutterfly_0~3.png`、攻擊觸鬚 `tendril.png`；美術交正式圖後替換同名檔 |
| 深淵之眼（眼球） | 約 38×38 | 2026-10-04 程式繪製暫用圖（分層）：眼白 `eye_base.png`、瞳孔 `eye_iris.png`（追視玩家）、眼皮眨眼 4 格 `eye_lid_0~3.png`，在 `Assets/Art/Enemies/` |
| Boss | 約 144×208 | 2026-10-04：待機動畫已交付 `boss.gif`（1280×720、33 格），拆格放在 `Assets/Art/Boss/`。`[待確認]` 請美術補 PNG 序列圖（全彩、半透明邊緣）；攻擊 / 被封印動畫尚無 |
| 邪神雕像（封印道具）/ 鎮靜藥丸 / 海草繃帶 | 約 20×20 | 場景圖示。2026-10-03 已交付：`Assets/Art/Items/item_idol.png`、`item_pill.png`、`item_bandage.png`（原檔裁掉透明邊並縮圖） |
| 水母（取代紅色魚怪） | — | 2026-10-04 已交付 6 格：`Assets/Art/Enemies/jellyfish_0~5.png`（裁邊縮成 256 寬） |
| 魚叉 | — | 2026-10-04 已交付 `Speargun.png`：轉成水平、槍頭朝右 → `Assets/Art/Weapon/harpoon.png`（512×92），飛行時依方向旋轉 |
| 精靈 | — | 2026-10-04 已交付 `elf.png` → `Assets/Art/UI/Elf/elf.png`，用於提示對話框與背包 |
| 落雷 / 光束特效 | 2026-10-04：使用素材包 FX Lightning II free（`Assets/FX_Kandol_Pack/FX_lightning_II`）：落雷 `Prefabs/fx_lightning_02.prefab`、深淵之眼光束 `Prefabs/fx_lightning_01.prefab`。素材包材質為 built-in 管線，在 URP 下若顯示粉紅色需換 Shader |
| 寶箱（灰 / 藍灰 / 黑，各有關 / 開） | 約 195×176 | 2026-10-03 由 `ArtSource/map_objects.png` 切出並去背：`Assets/Art/Chests/chest_{gray,bluegray,black}_{closed,open}.png`，同色關 / 開同尺寸、底部對齊 |
| 存檔點 | 約 16×38 | 2026-10-04 美術已交圖（燭台 + 眼睛），放 `Assets/Art/Props/checkpoint.png`（裁掉透明邊），遊戲內高 2.2 單位；未啟用偏暗、啟用恢復原色 |
| 封印祭壇 | 約 64×32 | 2026-10-04 美術已交圖（翅膀眼球 + 牙口石台），放 `Assets/Art/Props/altar.png`（裁掉透明邊），遊戲內寬 3 單位 |
| 憋氣屏障 | 16×480（可平鋪） | 開啟時半透明由程式處理 |
| 幻覺物件 | 自由 | 低 SAN 時才出現 |

## UI

| 素材 | 備註 |
|---|---|
| HP / SAN 框與填充條 | 2026-10-04 已交付：`Assets/Art/UI/HUD/hud_{hp,san}_{frame,fill}.png`（原檔 `hp框`、`框san`、`hp`、`hp san oxygen` 縮為一半） |
| 氧氣泡泡 ×10 | 2026-10-04 已交付：`Assets/Art/UI/HUD/hud_bubble_01~10.png`（原檔 `泡泡1~10`） |
| 玩家頭像 + 頭相框 | 2026-10-04：框 `Assets/Art/UI/HUD/hud_portrait_frame.png`（原檔 `頭相框`）；頭像 `hud_portrait.png` 暫時從封面裁主角臉，`[待確認]` 美術交正式頭像 |
| 名字框 | 已收到原檔（`圖片暫存/`），尚未使用 |
| 憋氣條 | 不再顯示（改輸出 Console） |
| 開場 / 結局插圖 | `[待確認]` 呈現形式 |
| 封面 | 2026-10-03 已交付：`Assets/Art/UI/title_cover.png`（3840×2160）、`btn_start.png`、`btn_quit.png`（按鈕原檔裁掉透明邊） |

## 音樂 / 音效清單

| 類別 | 項目 |
|---|---|
| BGM | 探索、Boss、結局 |

### 已交付

| 檔案 | 用途 | 規格 | 備註 |
|---|---|---|---|
| `Assets/Audio/BGM/bgm_intro.wav` | 開場 BGM（原檔 `Project 2_2.wav`），開場文字演出期間播放 | WAV 48kHz / 24-bit / 立體聲，16 秒，循環 | `[待確認]` 開場文字比 16 秒長時要循環還是只放一次（目前循環） |
| `Assets/Audio/BGM/bgm_explore.wav` | 探索 BGM，進入場景（開場結束）後播放（原檔 `Project 2_1.wav`；2026-10-03 由 `Project 2.wav` 換成） | WAV 48kHz / 24-bit / 立體聲，8 秒，循環 | 匯入時轉 Vorbis 串流；`[待確認]` 是否也用於 Boss / 結局 |
| 玩家 | 呼吸（循環）、憋氣開始 / 結束、發射魚叉、撿回魚叉、受傷、窒息、死亡 |
| 敵人 | 魚怪衝撞、觸手攻擊、深淵之眼低鳴、擊殺 |
| SAN | 低語（方向錯亂預告）、心跳（低 SAN） |
| 系統 | 拾取道具、使用道具、存檔點啟用、封印完成 |

### 已交付：音效 / 環境音（2026-10-03）

原檔改英文檔名存於 `ArtSource/Audio/`，用 `Tools/process_audio.py` 處理成 WAV（單次音效去開頭空白；循環音切淡入淡出 + 無縫交叉淡化）。用途與觸發時機見 [SD-02 音效](../SD/SD-02-audio.md)。

| 遊戲用檔案 | 原檔 | 用途 |
|---|---|---|
| `SFX/sfx_jump.wav` | 跳.mp3 | 跳躍 |
| `SFX/sfx_land.wav` | 落地.mp3 | 落地 |
| `SFX/sfx_footsteps_water_loop.wav` | 涉水.mp3 | 腳步（循環） |
| `SFX/sfx_harpoon_throw.wav` | Swinging_a_heavy_wea_#4-1791020701576.mp3 | 發射魚叉 |
| `SFX/sfx_harpoon_hit.wav` | 刀刺肉mp3.mp3 | 魚叉命中；擊殺借用 |
| `SFX/sfx_eat.wav` | 吃3.mp3 | 使用回復道具 |
| `SFX/sfx_breath_hold.wav` | sound_in_underwater__#4-1791020656905.mp3 | 開始憋氣 |
| `SFX/sfx_boss_roar.wav` | boss .mp3 | Boss 咆哮；封印完成借用 |
| `SFX/sfx_whisper.wav` | Eerie,_echoing_whisp_#4-1791020970472.mp3 | 精神錯亂預告（低語） |
| `SFX/sfx_jumpscare.wav` | jump scare.mp3 | SAN 掉進更低分段 |
| `Ambience/amb_cave_wind.wav` | 恐怖風聲.mp3（截 60 秒） | 洞窟基底環境音 |
| `Ambience/amb_low_sanity.wav` | 恐怖音效.mp3 | 低 SAN 環境音 |
| `Ambience/amb_breath_hold.wav` | 恐怖風聲空靈.mp3 | 憋氣中的空靈音 |
| `Ambience/amb_boss_hall.wav` | 恐怖背景音空靈恐怖.mp3 | Boss 房環境音 |
| `Ambience/amb_water.wav` | 水聲.mp3 | 憋氣屏障水聲（位置音效） |

`[待確認]` 用途為開發依檔名與波形判讀，請音效試聽確認。

