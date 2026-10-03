# 把 ArtSource/Audio 的原始音效處理成遊戲用 WAV（見 docs/feature/需求/00-overview/SD/SD-02-audio.md）
# ・單次音效：去掉開頭空白、結尾補 10ms 淡出
# ・循環音：切掉頭尾淡入 / 淡出 → 頭尾等功率交叉淡化成無縫循環
# 用法：python3 Tools/process_audio.py（需要 numpy；解碼用 macOS afconvert，沒有時改用 ffmpeg）
import os, shutil, subprocess, sys, tempfile, wave
import numpy as np

ROOT = os.path.join(os.path.dirname(__file__), '..')        # 專案根目錄
SRC = os.path.join(ROOT, 'ArtSource', 'Audio')               # 原始檔資料夾
SFX = os.path.join(ROOT, 'Assets', 'Audio', 'SFX')           # 單次音效輸出
AMB = os.path.join(ROOT, 'Assets', 'Audio', 'Ambience')      # 環境音輸出

# 單次音效：(原始檔, 輸出檔)
ONESHOTS = [
    ('raw_jump.mp3', 'sfx_jump.wav'),
    ('raw_land.mp3', 'sfx_land.wav'),
    ('raw_swing_heavy_weapon.mp3', 'sfx_harpoon_throw.wav'),
    ('raw_stab_flesh.mp3', 'sfx_harpoon_hit.wav'),
    ('raw_eat.mp3', 'sfx_eat.wav'),
    ('raw_underwater.mp3', 'sfx_breath_hold.wav'),
    ('raw_boss.mp3', 'sfx_boss_roar.wav'),
    ('raw_eerie_whisper.mp3', 'sfx_whisper.wav'),
    ('raw_jump_scare.mp3', 'sfx_jumpscare.wav'),
]

# 循環音：(原始檔, 輸出資料夾, 輸出檔, 取用起點秒, 最長秒數(None = 到結尾), 交叉淡化秒數)
LOOPS = [
    ('raw_wading.mp3', SFX, 'sfx_footsteps_water_loop.wav', 0.0, None, 0.3),
    ('raw_horror_wind.mp3', AMB, 'amb_cave_wind.wav', 10.0, 62.0, 2.0),
    ('raw_horror_drone.mp3', AMB, 'amb_low_sanity.wav', 0.0, None, 0.5),
    ('raw_ethereal_wind.mp3', AMB, 'amb_breath_hold.wav', 0.0, None, 1.0),
    ('raw_ethereal_horror_bg.mp3', AMB, 'amb_boss_hall.wav', 0.0, None, 1.0),
    ('raw_water.mp3', AMB, 'amb_water.wav', 0.0, None, 0.5),
]


def decode(path):
    # 解碼成 16-bit PCM WAV，回傳 (float 陣列 [樣本, 聲道], 取樣率)
    tmp = tempfile.mktemp(suffix='.wav')
    if shutil.which('afconvert'):
        subprocess.run(['afconvert', '-f', 'WAVE', '-d', 'LEI16', path, tmp], check=True)
    elif shutil.which('ffmpeg'):
        subprocess.run(['ffmpeg', '-loglevel', 'error', '-y', '-i', path, '-acodec', 'pcm_s16le', tmp], check=True)
    else:
        sys.exit('找不到 afconvert 或 ffmpeg，無法解碼 MP3')
    with wave.open(tmp) as w:
        ch, sr, n = w.getnchannels(), w.getframerate(), w.getnframes()
        data = np.frombuffer(w.readframes(n), dtype=np.int16).astype(np.float64) / 32768.0
    os.remove(tmp)
    return data.reshape(-1, ch), sr


def write(path, data, sr):
    # 寫成 16-bit PCM WAV
    os.makedirs(os.path.dirname(path), exist_ok=True)
    pcm = (np.clip(data, -1.0, 1.0) * 32767.0).astype(np.int16)
    with wave.open(path, 'wb') as w:
        w.setnchannels(data.shape[1])
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(pcm.tobytes())


def level_db(data, sr, win=0.05):
    # 每 win 秒一格的音量（dB）
    n = max(1, int(sr * win))
    mono = np.abs(data).max(axis=1)
    frames = len(mono) // n
    rms = np.sqrt((mono[:frames * n].reshape(frames, n) ** 2).mean(axis=1))
    return 20 * np.log10(np.maximum(rms, 1e-9)), n


def oneshot(data, sr):
    # 去掉開頭低於 -50 dBFS 的空白（保留 5ms），結尾補 10ms 淡出
    mono = np.abs(data).max(axis=1)
    loud = np.nonzero(mono > 10 ** (-50 / 20))[0]
    if len(loud) == 0:
        return data
    start = max(0, loud[0] - int(0.005 * sr))
    end = loud[-1] + 1
    out = data[start:end].copy()
    fade = min(len(out), int(0.01 * sr))
    out[-fade:] *= np.linspace(1.0, 0.0, fade)[:, None]
    return out


def trim_fades(data, sr):
    # 切掉頭尾比中段音量低 6 dB 以上的淡入 / 淡出
    db, n = level_db(data, sr)
    target = np.median(db) - 6.0
    above = np.nonzero(db >= target)[0]
    if len(above) == 0:
        return data
    return data[above[0] * n:(above[-1] + 1) * n]


def make_loop(data, sr, cross):
    # 等功率交叉淡化：尾端 cross 秒淡出，同時疊上開頭 cross 秒淡入 → 無縫循環
    c = int(cross * sr)
    if c <= 0 or len(data) <= 2 * c:
        return data
    t = np.linspace(0.0, 1.0, c)[:, None]
    head, tail = data[:c], data[-c:]
    mixed = tail * np.cos(t * np.pi / 2) + head * np.sin(t * np.pi / 2)
    return np.concatenate([data[c:-c], mixed])


def main():
    # 依清單處理全部音效
    for src, dst in ONESHOTS:
        data, sr = decode(os.path.join(SRC, src))
        out = oneshot(data, sr)
        write(os.path.join(SFX, dst), out, sr)
        print(f'{dst:32s} {len(out) / sr:6.2f}s  {sr}Hz x{data.shape[1]}')
    for src, folder, dst, offset, length, cross in LOOPS:
        data, sr = decode(os.path.join(SRC, src))
        data = trim_fades(data, sr)
        a = int(offset * sr)
        b = len(data) if length is None else min(len(data), a + int(length * sr))
        out = make_loop(data[a:b], sr, cross)
        write(os.path.join(folder, dst), out, sr)
        print(f'{dst:32s} {len(out) / sr:6.2f}s  {sr}Hz x{data.shape[1]}  (loop, crossfade {cross}s)')


if __name__ == '__main__':
    main()
