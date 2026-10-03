using UnityEngine;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>
    /// 有框的數值條（HP / SAN）：框圖在底、填充條依比例由右往左縮短，數字疊在上面。
    /// 減少時填充條閃白，後方殘影條停一下再慢慢縮回；增加時填充條平滑長回。
    /// </summary>
    public class UIFrameBar
    {
        /// <summary>填充條相對框的內縮（框圖 1872×297、填充圖 1788×209 → 左右 42 / 上下 44 像素）。</summary>
        private static readonly Vector2 FillInsetRatio = new Vector2(42f / 1872f, 44f / 297f);

        /// <summary>殘影開始縮回前的停留秒數。</summary>
        private const float GhostDelay = 0.35f;
        /// <summary>殘影每秒縮回的比例。</summary>
        private const float GhostSpeed = 0.6f;
        /// <summary>增加時填充條每秒長回的比例。</summary>
        private const float GrowSpeed = 1.2f;
        /// <summary>減少時閃白秒數。</summary>
        private const float FlashTime = 0.15f;

        /// <summary>填充條。</summary>
        private readonly Image _fill;
        /// <summary>減少時的殘影條（在填充條後方）。</summary>
        private readonly Image _ghost;
        /// <summary>「失去的最大值」遮罩（可為空）。</summary>
        private readonly RectTransform _cap;
        /// <summary>數值文字。</summary>
        private readonly Text _label;
        /// <summary>填充條原色。</summary>
        private readonly Color _fillColor;

        /// <summary>目標比例。</summary>
        private float _target = -1f;
        /// <summary>填充條目前顯示比例。</summary>
        private float _shown;
        /// <summary>殘影目前比例。</summary>
        private float _ghostShown;
        /// <summary>殘影停留倒數。</summary>
        private float _ghostDelay;
        /// <summary>閃白倒數。</summary>
        private float _flash;

        /// <summary>建立數值條（左上角錨點；沒有圖時以純色代替）。</summary>
        public UIFrameBar(Transform parent, string name, Vector2 pos, Vector2 size, Sprite frame, Sprite fill, Color fallbackColor, bool withCap = false)
        {
            var root = UIFactory.Rect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);
            var frameImage = UIFactory.Image(root, frame != null ? Color.white : new Color(0f, 0f, 0f, 0.6f));
            frameImage.sprite = frame;

            var area = UIFactory.Stretch("FillArea", root);
            var inset = new Vector2(size.x * FillInsetRatio.x, size.y * FillInsetRatio.y);
            area.offsetMin = inset;
            area.offsetMax = -inset;

            _fillColor = fill != null ? Color.white : fallbackColor;
            _ghost = MakeFill("Ghost", area, fill, new Color(1f, 0.95f, 0.85f, 0.85f));
            _fill = MakeFill("Fill", area, fill, _fillColor);

            if (withCap)
            {
                _cap = UIFactory.Stretch("Lost", area);
                UIFactory.Image(_cap, new Color(0.1f, 0.02f, 0.05f, 0.85f));
                _cap.anchorMin = new Vector2(1f, 0f);
            }

            var labelRt = UIFactory.Stretch("Label", area);
            labelRt.offsetMin = new Vector2(12f, 0f);
            _label = UIFactory.Text(labelRt, "", 20, TextAnchor.MiddleLeft, Color.white);
        }

        /// <summary>建立水平填充的 Image（由左往右填，比例減少時右側縮短）。</summary>
        private static Image MakeFill(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var img = UIFactory.Image(UIFactory.Stretch(name, parent), color);
            img.sprite = sprite;
            img.type = UnityEngine.UI.Image.Type.Filled;
            img.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            img.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            return img;
        }

        /// <summary>設定比例與文字（第一次直接到位，之後減少播放動畫）。</summary>
        public void Set(float ratio, string label)
        {
            ratio = Mathf.Clamp01(ratio);
            _label.text = label;
            if (_target < 0f)
            {
                _target = _shown = _ghostShown = ratio;
                Apply();
                return;
            }

            if (ratio < _target - 0.0001f)
            {
                // 減少：填充條直接縮短並閃白，殘影留在原處稍後跟上
                _ghostShown = Mathf.Max(_ghostShown, _shown);
                _shown = ratio;
                _ghostDelay = GhostDelay;
                _flash = FlashTime;
            }
            _target = ratio;
            Apply();
        }

        /// <summary>設定最大值上限比例（右側以暗色表示失去的部分）。</summary>
        public void SetCap(float capRatio)
        {
            if (_cap != null) _cap.anchorMin = new Vector2(Mathf.Clamp01(capRatio), 0f);
        }

        /// <summary>每幀更新動畫（不受 timeScale 影響）。</summary>
        public void Tick(float dt)
        {
            if (_target < 0f) return;
            if (_shown < _target) _shown = Mathf.MoveTowards(_shown, _target, GrowSpeed * dt);

            if (_ghostDelay > 0f) _ghostDelay -= dt;
            else _ghostShown = Mathf.MoveTowards(_ghostShown, _shown, GhostSpeed * dt);
            if (_ghostShown < _shown) _ghostShown = _shown;

            if (_flash > 0f) _flash -= dt;
            Apply();
        }

        /// <summary>把目前比例與閃白套用到圖片。</summary>
        private void Apply()
        {
            _fill.fillAmount = _shown;
            _ghost.fillAmount = _ghostShown;
            _fill.color = _flash > 0f ? Color.Lerp(_fillColor, Color.white, 0.7f) : _fillColor;
        }
    }

    /// <summary>
    /// 氧氣泡泡列：最多 N 顆，依氧氣百分比顯示（最後一顆依零頭縮小）。
    /// 泡泡消失時放大淡出（破掉），恢復時從小彈回。
    /// </summary>
    public class UIBubbleRow
    {
        /// <summary>零頭泡泡的最小縮放（避免太小看不到）。</summary>
        private const float MinPartialScale = 0.35f;
        /// <summary>破掉動畫秒數。</summary>
        private const float PopTime = 0.25f;
        /// <summary>縮放平滑速度。</summary>
        private const float ScaleSpeed = 4f;

        /// <summary>各泡泡圖片。</summary>
        private readonly Image[] _bubbles;
        /// <summary>各泡泡目標縮放（0 = 不顯示）。</summary>
        private readonly float[] _targetScale;
        /// <summary>各泡泡破掉動畫剩餘秒數。</summary>
        private readonly float[] _popTimer;
        /// <summary>是否已收到第一次數值（第一次直接到位）。</summary>
        private bool _initialized;

        /// <summary>建立泡泡列（左上角錨點，由左往右排）。</summary>
        public UIBubbleRow(Transform parent, string name, Vector2 pos, float bubbleSize, float spacing, Sprite[] sprites, int count)
        {
            var root = UIFactory.Rect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), pos,
                new Vector2(count * bubbleSize + (count - 1) * spacing, bubbleSize));
            _bubbles = new Image[count];
            _targetScale = new float[count];
            _popTimer = new float[count];
            for (int i = 0; i < count; i++)
            {
                var rt = UIFactory.Rect($"Bubble{i + 1}", root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(i * (bubbleSize + spacing) + bubbleSize / 2f, 0f), new Vector2(bubbleSize, bubbleSize));
                var sprite = sprites != null && sprites.Length > 0 ? sprites[i % sprites.Length] : null;
                _bubbles[i] = UIFactory.Image(rt, sprite != null ? Color.white : new Color(0.6f, 0.9f, 1f));
                _bubbles[i].sprite = sprite;
                _bubbles[i].preserveAspect = true;
            }
        }

        /// <summary>設定氧氣比例（0~1），算出每顆泡泡的目標大小；消失的泡泡播放破掉動畫。</summary>
        public void Set(float ratio)
        {
            float units = Mathf.Clamp01(ratio) * _bubbles.Length;
            for (int i = 0; i < _bubbles.Length; i++)
            {
                float part = Mathf.Clamp01(units - i);
                float scale = part <= 0.001f ? 0f : Mathf.Lerp(MinPartialScale, 1f, part);
                if (_initialized && scale <= 0f && _targetScale[i] > 0f) _popTimer[i] = PopTime;
                _targetScale[i] = scale;
                if (!_initialized) SetScale(i, scale, 1f);
            }
            _initialized = true;
        }

        /// <summary>每幀更新縮放與破掉動畫（不受 timeScale 影響）。</summary>
        public void Tick(float dt)
        {
            for (int i = 0; i < _bubbles.Length; i++)
            {
                if (_popTimer[i] > 0f)
                {
                    // 破掉：放大並淡出
                    _popTimer[i] -= dt;
                    float k = 1f - Mathf.Clamp01(_popTimer[i] / PopTime);
                    if (_popTimer[i] <= 0f) SetScale(i, 0f, 0f); // 破完歸零，恢復時從小長回
                    else SetScale(i, Mathf.Lerp(1f, 1.5f, k), 1f - k);
                    continue;
                }
                float current = _bubbles[i].rectTransform.localScale.x;
                float target = _targetScale[i];
                // 平滑縮放到目標大小（恢復時從小長回）
                SetScale(i, Mathf.MoveTowards(current, target, ScaleSpeed * dt), target > 0f ? 1f : 0f);
            }
        }

        /// <summary>設定泡泡縮放與透明度（縮放 0 時隱藏）。</summary>
        private void SetScale(int i, float scale, float alpha)
        {
            var img = _bubbles[i];
            img.rectTransform.localScale = Vector3.one * scale;
            var c = img.color;
            c.a = alpha;
            img.color = c;
            img.enabled = scale > 0.001f && alpha > 0.001f;
        }
    }
}
