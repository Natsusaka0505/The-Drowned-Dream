# SD-02 — 音效與環境音

對應：[assets/README.md](../assets/README.md) 的「音樂 / 音效清單」、[原始需求 2026-10-03 音效](../原始需求/2026-10-03-音效.md)

## 架構

| 程式 | 職責 |
|---|---|
| `Data/AudioConfig`（`Assets/Data/Config/AudioConfig.asset`） | 每個音效的 clip、音量、音高隨機、冷卻；環境音層的音量；觸發門檻。企劃可在 Inspector 調 |
| `Audio/GameAudio` | 單次音效（8 聲道輪流）+ 環境音疊層；**只訂閱事件**，不被其他系統直接呼叫 |
| `Audio/AmbientEmitter` | 有位置感的循環音：依玩家距離調音量（2D 計算，不受攝影機 z 影響） |
| `Audio/BgmPlayer` | BGM；`Duck` 屬性讓 BGM 暫時壓低（憋氣時） |

## 單次音效

| 音效 | 檔案（原檔） | 觸發事件 |
|---|---|---|
| 跳躍 | `SFX/sfx_jump.wav`（跳.mp3） | `PlayerMove.Jumped` |
| 落地 | `SFX/sfx_land.wav`（落地.mp3） | `PlayerMove.Landed`，下落速度 ≥ `LandMinFallSpeed` 才播 |
| 發射魚叉 | `SFX/sfx_harpoon_throw.wav`（Swinging_a_heavy_wea…） | `PlayerAttack.Thrown` |
| 魚叉命中敵人 | `SFX/sfx_harpoon_hit.wav`（刀刺肉mp3.mp3） | `GameEvents.EnemyHit`（未死） |
| 擊殺 | 借用刀刺肉（音高 0.8） | `GameEvents.EnemyHit`（擊殺那一下） |
| 使用回復道具 | `SFX/sfx_eat.wav`（吃3.mp3） | `GameEvents.RecoveryUsed` |
| 開始憋氣 | `SFX/sfx_breath_hold.wav`（sound_in_underwater…） | `PlayerBreath.StateChanged` → Holding |
| Boss 咆哮 | `SFX/sfx_boss_roar.wav`（boss .mp3） | `GameEvents.BossRevealed`（第一次進 Boss 房特寫）、`GameEvents.BossActivated`；冷卻 6 秒避免連播 |
| 封印完成 | 借用 Boss 咆哮（音高 0.7） | `GameEvents.BossSealed` |
| 低語 | `SFX/sfx_whisper.wav`（Eerie,_echoing_whisp…） | `PlayerConfusion.WarningStarted`（精神錯亂預告） |
| 驚嚇 | `SFX/sfx_jumpscare.wav`（jump scare.mp3） | `PlayerStatus.SanityStageChanged` 往更低分段時（幻覺出現） |

## 環境音疊層（循環）

| 層 | 檔案（原檔） | 何時有聲音 |
|---|---|---|
| 洞窟風聲 | `Ambience/amb_cave_wind.wav`（恐怖風聲.mp3，截 60 秒） | 開場以外一直墊在 BGM 下 |
| 低 SAN | `Ambience/amb_low_sanity.wav`（恐怖音效.mp3） | SAN 低於 `LowSanityStartRatio`（75%）開始淡入，SAN 0 最大聲 |
| 憋氣中 | `Ambience/amb_breath_hold.wav`（恐怖風聲空靈.mp3） | 憋氣時淡入，同時 BGM 壓到 `MusicDuckWhileHolding` |
| Boss 房 | `Ambience/amb_boss_hall.wav`（恐怖背景音空靈恐怖.mp3） | 玩家在 Boss 房內 |
| 腳步（涉水） | `SFX/sfx_footsteps_water_loop.wav`（涉水.mp3） | 站在地上且水平速度 ≥ `FootstepMinSpeed` |
| 水聲 | `Ambience/amb_water.wav`（水聲.mp3） | 憋氣屏障上的 `AmbientEmitter`，靠近才聽得到 |

## 素材前處理（`Tools/process_audio.py`）

- 原檔改英文檔名存在 `ArtSource/Audio/`（中文、空白、`#`、逗號在 Windows 易出錯）。
- 單次音效：去掉開頭空白 → WAV（減少按鍵到出聲的延遲）。
- 循環音：切掉淡入 / 淡出 → 頭尾等功率交叉淡化成無縫循環 → WAV（MP3 頭尾有編碼留白，直接循環會有斷點）。
- 解碼用 macOS `afconvert`，沒有時改用 `ffmpeg`（Windows 可裝 ffmpeg）。只在換素材時執行，不屬於建置流程。

## 匯入設定（場景產生器自動套用）

| 類型 | Load Type | 壓縮 |
|---|---|---|
| 單次音效 | Decompress On Load | ADPCM |
| 循環音 | Compressed In Memory | Vorbis |

## 尚無素材（`AudioConfig` 欄位留空即不播放）

呼吸循環、憋氣結束、撿回魚叉、受傷、窒息、死亡、魚怪衝撞、觸手攻擊、深淵之眼低鳴、心跳、存檔點啟用、Boss 戰 / 結局 BGM。
