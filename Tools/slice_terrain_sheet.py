# 把地板素材表切成地形 3×3 + 裝飾零件（背景去背成透明）
# 用法：python3 Tools/slice_terrain_sheet.py [輸出資料夾]（需要 Pillow、numpy）
from PIL import Image, ImageFilter
import numpy as np
from collections import deque
import os, sys

SRC = os.path.join(os.path.dirname(__file__), '..', 'ArtSource', 'terrain_sheet.jpg')
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), '..', 'Assets', 'Art', 'Map', 'Terrain')
TILE = 128                       # 輸出地形格邊長（px）
SCALE = TILE / 310.0             # 素材表 → 輸出的縮放比例（地形格原尺寸約 310px）
BG = [(48, 44, 40), (166, 178, 158), (144, 152, 132)]  # 背景色：深底、淺面板、暗面板

img = np.asarray(Image.open(SRC).convert('RGB')).astype(int)

def is_bg(px):
    # 判斷像素是否接近任一背景色
    return min(abs(px - np.array(c)).sum() for c in BG) < 28

def cut(box, flood=True, bg=None):
    # 裁切並從邊框 flood fill 去背（bg 指定只把哪些顏色當背景）
    x0, y0, x1, y1 = box
    sub = img[y0:y1, x0:x1]
    h, w, _ = sub.shape
    bgmask = np.zeros((h, w), bool)
    near = np.zeros((h, w), bool)
    for c in (bg or BG):
        near |= abs(sub - np.array(c)).sum(2) < 28
    if flood:
        q = deque()
        for x in range(w):
            for y in (0, h - 1):
                if near[y, x] and not bgmask[y, x]: bgmask[y, x] = True; q.append((y, x))
        for y in range(h):
            for x in (0, w - 1):
                if near[y, x] and not bgmask[y, x]: bgmask[y, x] = True; q.append((y, x))
        while q:
            y, x = q.popleft()
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < h and 0 <= nx < w and near[ny, nx] and not bgmask[ny, nx]:
                    bgmask[ny, nx] = True; q.append((ny, nx))
    alpha = Image.fromarray(np.where(bgmask, 0, 255).astype('uint8')).filter(ImageFilter.GaussianBlur(1.2))
    rgba = Image.fromarray(sub.astype('uint8')).convert('RGBA')
    rgba.putalpha(alpha)
    return rgba

def save(im, name, size=None):
    # 縮放後存檔
    size = size or (max(1, round(im.width * SCALE)), max(1, round(im.height * SCALE)))
    im.resize(size, Image.LANCZOS).save(os.path.join(OUT, name + '.png'))

os.makedirs(OUT, exist_ok=True)

# 地形 3×3（無背景：整格都是石板，flood 只吃掉外圍面板色）
xs = [330, 642, 962, 1256]
ys = [330, 620, 940, 1250]
names = [['tl', 'tc', 'tr'], ['ml', 'mc', 'mr'], ['bl', 'bc', 'br']]
for r in range(3):
    for c in range(3):
        save(cut((xs[c], ys[r], xs[c + 1], ys[r + 1])), 'terrain_' + names[r][c], (TILE, TILE))

# 裝飾
# 天花板垂吊：從深色底下方開始裁（頂端切平貼齊天花板），只把淺色面板當背景，保留深色陰影
save(cut((1622, 1328, 1862, 1520), bg=[(144, 152, 132), (166, 178, 158)]), 'decor_ceiling_stalactite')
save(cut((140, 320, 312, 625)), 'decor_side_left')                 # 左側尖刺（貼齊地形左緣）
save(cut((1614, 320, 1730, 615)), 'decor_side_right')              # 右側尖刺（貼齊地形右緣）
