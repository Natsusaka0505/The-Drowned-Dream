using UnityEngine;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>
    /// 畫面邊緣的方向箭頭：指向最後的封印道具寶箱（目標在畫面外時顯示，進入畫面或被打開後隱藏）。
    /// </summary>
    public class TargetArrow
    {
        /// <summary>箭頭根物件（位置 / 旋轉）。</summary>
        private readonly RectTransform _arrow;
        /// <summary>箭頭圖。</summary>
        private readonly Image _image;
        /// <summary>距離文字。</summary>
        private readonly Text _label;
        /// <summary>HUD 根物件（算畫面邊緣）。</summary>
        private readonly RectTransform _root;
        /// <summary>箭頭顏色。</summary>
        private readonly Color _color;

        /// <summary>箭頭離畫面邊緣的距離。</summary>
        private const float EdgeMargin = 70f;

        /// <summary>在 HUD 根物件下建立箭頭（預設隱藏）。</summary>
        public TargetArrow(RectTransform root, Color color)
        {
            _root = root;
            _color = color;
            _arrow = UIFactory.Rect("FinalSealArrow", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 60f));
            var tri = UIFactory.Rect("Triangle", _arrow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(46f, 60f));
            _image = UIFactory.Image(tri, color);
            _image.sprite = HazardSprites.Spike; // 尖端朝上的三角形，整體旋轉指向目標
            var labelRt = UIFactory.Rect("Distance", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 30f));
            _label = UIFactory.Text(labelRt, "", 20, TextAnchor.MiddleCenter, color);
            SetVisible(false);
        }

        /// <summary>每幀更新：找最後封印寶箱，畫面外就把箭頭放在邊緣並指向它。</summary>
        public void Tick()
        {
            TreasureChest target = null;
            foreach (var c in TreasureChest.All)
            {
                if (c != null && c.IsFinalSeal && !c.IsOpened) { target = c; break; }
            }
            var cam = Camera.main;
            if (target == null || cam == null)
            {
                SetVisible(false);
                return;
            }

            Vector3 vp = cam.WorldToViewportPoint(target.transform.position);
            bool onScreen = vp.z > 0f && vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.05f && vp.y < 0.95f;
            if (onScreen)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            Vector2 dir = ((Vector2)(target.transform.position - cam.transform.position)).normalized;
            Vector2 half = _root.rect.size / 2f - new Vector2(EdgeMargin, EdgeMargin);
            // 沿方向延伸到畫面邊緣（矩形內最遠點）
            float t = Mathf.Min(Mathf.Abs(dir.x) > 0.001f ? half.x / Mathf.Abs(dir.x) : float.MaxValue,
                                Mathf.Abs(dir.y) > 0.001f ? half.y / Mathf.Abs(dir.y) : float.MaxValue);
            Vector2 pos = dir * t;
            _arrow.anchoredPosition = pos;
            _arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            _label.rectTransform.anchoredPosition = pos - dir * 55f;

            float dist = Vector2.Distance(target.transform.position, cam.transform.position);
            _label.text = $"最後的雕像 {dist:0}m";
            float pulse = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
            _image.color = new Color(_color.r, _color.g, _color.b, pulse);
        }

        /// <summary>顯示 / 隱藏箭頭與文字。</summary>
        private void SetVisible(bool visible)
        {
            _image.enabled = visible;
            _label.enabled = visible;
        }
    }
}
