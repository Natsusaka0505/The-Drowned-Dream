using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>
    /// 背包檢視（F-INV-01/03）。開啟時暫停遊戲（時間停止，不會受到攻擊）。
    /// 左側：道具圖示與數量；右側：小地圖（只顯示去過的區塊與其中的台階、未開寶箱依內容物上色、最後的封印道具閃爍標示）；左下：精靈。
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        [Header("圖示")]
        /// <summary>魚叉圖示。</summary>
        [SerializeField] private Sprite _harpoonIcon;
        /// <summary>邪神雕像（封印道具）圖示。</summary>
        [SerializeField] private Sprite _sealIcon;
        /// <summary>繃帶（回 HP）圖示。</summary>
        [SerializeField] private Sprite _hpIcon;
        /// <summary>藥丸（回 SAN）圖示。</summary>
        [SerializeField] private Sprite _sanityIcon;
        /// <summary>精靈圖。</summary>
        [SerializeField] private Sprite _elfSprite;

        [Header("小地圖顏色")]
        /// <summary>去過的區塊。</summary>
        [SerializeField] private Color _roomColor = new Color(0.22f, 0.34f, 0.48f, 1f);
        /// <summary>台階（浮台）。</summary>
        [SerializeField] private Color _platformColor = new Color(0.75f, 0.85f, 0.7f, 1f);
        /// <summary>繃帶寶箱（灰）。</summary>
        [SerializeField] private Color _hpChestColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        /// <summary>藥丸寶箱（藍灰）。</summary>
        [SerializeField] private Color _sanityChestColor = new Color(0.45f, 0.6f, 0.95f, 1f);
        /// <summary>邪神雕像寶箱（黑寶箱，以紫色標示）。</summary>
        [SerializeField] private Color _sealChestColor = new Color(0.7f, 0.3f, 0.95f, 1f);
        /// <summary>最後的封印道具。</summary>
        [SerializeField] private Color _finalSealColor = new Color(1f, 0.35f, 0.75f, 1f);
        /// <summary>Boss（進過 Boss 房後顯示）。</summary>
        [SerializeField] private Color _bossColor = new Color(0.9f, 0.12f, 0.12f, 1f);
        /// <summary>已封印的 Boss。</summary>
        [SerializeField] private Color _sealedBossColor = new Color(0.45f, 0.45f, 0.5f, 1f);

        [Header("精靈台詞（打開背包時隨機一句）")]
        /// <summary>一般台詞。</summary>
        [SerializeField] private string[] _elfLines =
        {
            "地圖上亮起來的地方都是你去過的喔！",
            "顏色不同的小方塊是還沒打開的寶箱～",
            "雕像要集滿才能封印那傢伙，加油！",
            "在這裡休息一下也沒關係，時間是停住的。",
        };
        /// <summary>最後的封印道具出現後的台詞。</summary>
        [SerializeField] private string _finalSealLine = "最後一尊雕像在地圖上閃爍的地方！快去拿！";

        /// <summary>小地圖區域大小。</summary>
        private const float MapSize = 560f;

        /// <summary>面板根物件。</summary>
        private GameObject _panel;
        /// <summary>魚叉數量文字。</summary>
        private Text _harpoonText;
        /// <summary>封印道具數量文字。</summary>
        private Text _sealText;
        /// <summary>小地圖內容（每次開啟重畫）。</summary>
        private RectTransform _mapContent;
        /// <summary>精靈台詞。</summary>
        private Text _elfText;
        /// <summary>最後封印道具標記（閃爍）。</summary>
        private readonly List<Image> _blinkMarkers = new List<Image>();
        /// <summary>玩家快取。</summary>
        private Player _player;

        /// <summary>建立面板（預設隱藏）。</summary>
        private void Awake()
        {
            var bg = UIFactory.Stretch("InventoryPanel", transform);
            UIFactory.Image(bg, new Color(0.02f, 0.04f, 0.08f, 0.92f));
            _panel = bg.gameObject;

            var title = UIFactory.Rect("Title", bg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(800f, 60f));
            UIFactory.Text(title, "背包", 44, TextAnchor.MiddleCenter, Color.white);

            BuildItemList(bg);
            BuildMap(bg);
            BuildElf(bg);

            var hint = UIFactory.Rect("Hint", bg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(800f, 40f));
            UIFactory.Text(hint, "Tab 關閉（背包開啟時遊戲暫停）", 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.6f));

            _panel.SetActive(false);
        }

        /// <summary>左側道具清單：圖示 + 名稱數量 + 說明，下方為小地圖圖例。</summary>
        private void BuildItemList(RectTransform bg)
        {
            var list = UIFactory.Rect("Items", bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400f, 60f), new Vector2(640f, 600f));
            _harpoonText = ItemRow(list, 0f, _harpoonIcon, "［武器］魚叉", "撿回插在牆上的魚叉可補充。");
            _sealText = ItemRow(list, -150f, _sealIcon, "［關鍵］邪神雕像", "集齊即可在祭壇封印邪神。");

            var legendTitle = UIFactory.Rect("LegendTitle", list, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -320f), new Vector2(600f, 36f));
            UIFactory.Text(legendTitle, "小地圖：未開啟的寶箱", 24, TextAnchor.MiddleLeft, new Color(0.8f, 0.85f, 0.9f));
            LegendRow(list, -370f, _hpIcon, _hpChestColor, "灰寶箱 —— 海草繃帶（回 HP）");
            LegendRow(list, -420f, _sanityIcon, _sanityChestColor, "藍灰寶箱 —— 鎮靜藥丸（回 SAN）");
            LegendRow(list, -470f, _sealIcon, _sealChestColor, "黑寶箱 —— 邪神雕像");
            LegendRow(list, -520f, _sealIcon, _finalSealColor, "最後的邪神雕像（閃爍）");
            LegendRow(list, -570f, null, _bossColor, "邪神（進過 Boss 房後顯示）");
        }

        /// <summary>一列道具：左邊大圖示，右邊名稱 / 數量與說明，回傳數量文字。</summary>
        private static Text ItemRow(RectTransform parent, float y, Sprite icon, string name, string desc)
        {
            var row = UIFactory.Rect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(620f, 130f));
            var frame = UIFactory.Rect("IconFrame", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(120f, 120f));
            UIFactory.Image(frame, new Color(1f, 1f, 1f, 0.08f));
            var iconRt = UIFactory.Stretch("Icon", frame);
            iconRt.offsetMin = new Vector2(10f, 10f);
            iconRt.offsetMax = new Vector2(-10f, -10f);
            var img = UIFactory.Image(iconRt, Color.white);
            img.sprite = icon;
            img.preserveAspect = true;
            img.enabled = icon != null;

            var countRt = UIFactory.Rect("Count", row, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -10f), new Vector2(480f, 50f));
            var count = UIFactory.Text(countRt, name, 30, TextAnchor.MiddleLeft, Color.white);
            var descRt = UIFactory.Rect("Desc", row, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -62f), new Vector2(480f, 50f));
            UIFactory.Text(descRt, desc, 22, TextAnchor.UpperLeft, new Color(0.75f, 0.8f, 0.85f));
            return count;
        }

        /// <summary>一列圖例：小圖示 + 色塊 + 說明。</summary>
        private static void LegendRow(RectTransform parent, float y, Sprite icon, Color color, string label)
        {
            var row = UIFactory.Rect(label, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(620f, 44f));
            var iconRt = UIFactory.Rect("Icon", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            var img = UIFactory.Image(iconRt, Color.white);
            img.sprite = icon;
            img.preserveAspect = true;
            img.enabled = icon != null;
            var swatch = UIFactory.Rect("Color", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(52f, 0f), new Vector2(22f, 22f));
            UIFactory.Image(swatch, color);
            var textRt = UIFactory.Rect("Label", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(88f, 0f), new Vector2(520f, 40f));
            UIFactory.Text(textRt, label, 22, TextAnchor.MiddleLeft, new Color(0.88f, 0.9f, 0.92f));
        }

        /// <summary>右側小地圖外框。</summary>
        private void BuildMap(RectTransform bg)
        {
            var frame = UIFactory.Rect("Map", bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(340f, 40f), new Vector2(MapSize + 16f, MapSize + 16f));
            UIFactory.Image(frame, new Color(0.6f, 0.75f, 0.85f, 0.5f));
            var inner = UIFactory.Stretch("Inner", frame);
            inner.offsetMin = new Vector2(8f, 8f);
            inner.offsetMax = new Vector2(-8f, -8f);
            UIFactory.Image(inner, new Color(0.01f, 0.02f, 0.04f, 1f));
            _mapContent = inner;

            var label = UIFactory.Rect("MapLabel", frame, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(400f, 36f));
            UIFactory.Text(label, "地圖", 26, TextAnchor.MiddleCenter, new Color(0.85f, 0.9f, 0.95f));
        }

        /// <summary>左下精靈 + 台詞。</summary>
        private void BuildElf(RectTransform bg)
        {
            var root = UIFactory.Rect("Elf", bg, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(60f, 70f), new Vector2(900f, 150f));
            var elfRt = UIFactory.Rect("Portrait", root, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(150f, 150f));
            var img = UIFactory.Image(elfRt, Color.white);
            img.sprite = _elfSprite;
            img.preserveAspect = true;
            img.enabled = _elfSprite != null;
            var textRt = UIFactory.Rect("Line", root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(165f, 40f), new Vector2(720f, 70f));
            _elfText = UIFactory.Text(textRt, "", 24, TextAnchor.MiddleLeft, new Color(0.92f, 0.97f, 1f));
        }

        /// <summary>快取玩家。</summary>
        private void Start()
        {
            _player = Player.Instance;
        }

        /// <summary>背包鍵切換開關並暫停遊戲；開啟中讓最後封印道具標記閃爍。</summary>
        private void Update()
        {
            if (_panel.activeSelf)
            {
                float a = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
                foreach (var m in _blinkMarkers) if (m != null) m.color = new Color(_finalSealColor.r, _finalSealColor.g, _finalSealColor.b, a);
            }

            if (_player == null || !_player.Input.InventoryPressed) return;
            var state = GameFlow.State;
            if (state != GameState.Playing && state != GameState.Paused) return;

            bool open = !_panel.activeSelf;
            _panel.SetActive(open);
            if (GameFlow.Instance != null) GameFlow.Instance.SetPaused(open);
            if (open) Refresh();
        }

        /// <summary>更新數量、重畫小地圖、換一句精靈台詞。</summary>
        private void Refresh()
        {
            var s = _player.Status;
            _harpoonText.text = $"［武器］魚叉　{s.HarpoonCount} / {s.HarpoonMax}";
            _sealText.text = $"［關鍵］邪神雕像　{s.SealCount} / {s.RequiredSeals}";
            RebuildMap();

            var final = FindFinalSeal();
            _elfText.text = final != null ? _finalSealLine
                : _elfLines != null && _elfLines.Length > 0 ? _elfLines[Random.Range(0, _elfLines.Length)] : "";
        }

        /// <summary>重畫小地圖：去過的區塊、去過區塊內未開的寶箱、最後的封印道具（不論是否去過）、玩家位置。</summary>
        private void RebuildMap()
        {
            for (int i = _mapContent.childCount - 1; i >= 0; i--) Destroy(_mapContent.GetChild(i).gameObject);
            _blinkMarkers.Clear();
            if (!Room.TryGetWorldBounds(out var world)) return;

            float scale = Mathf.Min(MapSize / world.size.x, MapSize / world.size.y);
            Vector2 offset = new Vector2((MapSize - world.size.x * scale) / 2f, (MapSize - world.size.y * scale) / 2f);
            Vector2 ToMap(Vector2 p) => offset + ((Vector2)(p - (Vector2)world.min)) * scale;

            foreach (var room in Room.All)
            {
                if (room == null || !room.Visited) continue;
                var b = room.Bounds;
                var rt = MapRect("Room", ToMap(b.min), b.size * scale);
                UIFactory.Image(rt, _roomColor);
                var inner = UIFactory.Stretch("Inner", rt); // 細邊框，區塊之間分得出來
                inner.offsetMin = new Vector2(2f, 2f);
                inner.offsetMax = new Vector2(-2f, -2f);
                UIFactory.Image(inner, _roomColor * 1.25f);
            }

            // 台階：去過的區塊裡的浮台畫成細條（至少 3px 高才看得到）
            foreach (var col in PlatformGroup.AllPlatforms)
            {
                if (col == null) continue;
                var b = col.bounds;
                var room = Room.FindAt(b.center);
                if (room == null || !room.Visited) continue;
                var size = new Vector2(b.size.x * scale, Mathf.Max(3f, b.size.y * scale));
                UIFactory.Image(MapRect("Platform", ToMap(new Vector2(b.min.x, b.max.y)) - new Vector2(0f, size.y), size), _platformColor);
            }

            foreach (var chest in TreasureChest.All)
            {
                if (chest == null || chest.IsOpened) continue;
                Vector2 pos = chest.transform.position;
                if (chest.IsFinalSeal)
                {
                    var ring = MapRect("FinalSeal", ToMap(pos) - new Vector2(14f, 14f), new Vector2(28f, 28f));
                    _blinkMarkers.Add(UIFactory.Image(ring, _finalSealColor));
                    var label = MapRect("FinalSealLabel", ToMap(pos) + new Vector2(-80f, 16f), new Vector2(160f, 30f));
                    UIFactory.Text(label, "最後的雕像", 20, TextAnchor.MiddleCenter, _finalSealColor);
                    continue;
                }
                var room = Room.FindAt(pos);
                if (room == null || !room.Visited) continue;
                var marker = MapRect("Chest", ToMap(pos) - new Vector2(8f, 8f), new Vector2(16f, 16f));
                UIFactory.Image(marker, ChestColor(chest));
            }

            DrawBoss(ToMap, scale);

            // 玩家位置（白點）
            var me = MapRect("Player", ToMap(_player.transform.position) - new Vector2(6f, 6f), new Vector2(12f, 12f));
            UIFactory.Image(me, Color.white);
        }

        /// <summary>Boss 標記：進過 Boss 房後畫出 Boss 範圍與名稱（封印後變灰）。</summary>
        private void DrawBoss(System.Func<Vector2, Vector2> toMap, float scale)
        {
            var boss = BossController.Instance;
            if (boss == null) return;
            var b = boss.Bounds;
            var room = Room.FindAt(b.center);
            if (room == null || !room.Visited) return;

            var color = boss.IsSealed ? _sealedBossColor : _bossColor;
            var size = new Vector2(Mathf.Max(12f, b.size.x * scale), Mathf.Max(12f, b.size.y * scale));
            var rt = MapRect("Boss", toMap(b.center) - size / 2f, size);
            UIFactory.Image(rt, new Color(color.r, color.g, color.b, 0.55f));
            var label = MapRect("BossLabel", toMap(new Vector2(b.center.x, b.max.y)) + new Vector2(-60f, 2f), new Vector2(120f, 28f));
            UIFactory.Text(label, boss.IsSealed ? "邪神（已封印）" : "邪神", 20, TextAnchor.MiddleCenter, color);
        }

        /// <summary>在小地圖內建立左下角錨點的矩形。</summary>
        private RectTransform MapRect(string name, Vector2 pos, Vector2 size) =>
            UIFactory.Rect(name, _mapContent, Vector2.zero, Vector2.zero, Vector2.zero, pos, size);

        /// <summary>依寶箱內容物決定標記顏色。</summary>
        private Color ChestColor(TreasureChest chest)
        {
            var item = chest.ItemPrefab;
            if (item is SealItem) return _sealChestColor;
            if (item is RecoveryItem r) return r.HpRestore > 0d ? _hpChestColor : _sanityChestColor;
            return Color.white;
        }

        /// <summary>找出場上尚未打開的最後封印道具寶箱。</summary>
        private static TreasureChest FindFinalSeal()
        {
            foreach (var c in TreasureChest.All) if (c != null && c.IsFinalSeal && !c.IsOpened) return c;
            return null;
        }
    }
}
