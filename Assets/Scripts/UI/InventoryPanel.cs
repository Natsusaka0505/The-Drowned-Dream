using UnityEngine;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>背包檢視（F-INV-01/03）。開啟時暫停遊戲。</summary>
    public class InventoryPanel : MonoBehaviour
    {
        /// <summary>面板根物件。</summary>
        private GameObject _panel;
        /// <summary>道具清單文字。</summary>
        private Text _content;
        /// <summary>玩家快取。</summary>
        private Player _player;

        /// <summary>建立面板（預設隱藏）。</summary>
        private void Awake()
        {
            var bg = UIFactory.Stretch("InventoryPanel", transform);
            UIFactory.Image(bg, new Color(0.02f, 0.04f, 0.08f, 0.88f));
            _panel = bg.gameObject;

            var title = UIFactory.Rect("Title", bg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(800f, 60f));
            UIFactory.Text(title, "背包", 44, TextAnchor.MiddleCenter, Color.white);

            var body = UIFactory.Rect("Content", bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1100f, 700f));
            _content = UIFactory.Text(body, "", 26, TextAnchor.UpperLeft, new Color(0.9f, 0.9f, 0.85f));

            var hint = UIFactory.Rect("Hint", bg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(800f, 40f));
            UIFactory.Text(hint, "Tab 關閉", 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.6f));

            _panel.SetActive(false);
        }

        /// <summary>快取玩家。</summary>
        private void Start()
        {
            _player = Player.Instance;
        }

        /// <summary>背包鍵切換開關並暫停遊戲。</summary>
        private void Update()
        {
            if (_player == null || !_player.Input.InventoryPressed) return;
            var state = GameFlow.State;
            if (state != GameState.Playing && state != GameState.Paused) return;

            bool open = !_panel.activeSelf;
            _panel.SetActive(open);
            if (GameFlow.Instance != null) GameFlow.Instance.SetPaused(open);
            if (open) Refresh();
        }

        /// <summary>重新產生道具清單文字（武器與封印道具分開）。</summary>
        private void Refresh()
        {
            var s = _player.Status;
            _content.text =
                $"［武器］魚叉　{s.HarpoonCount} / {s.HarpoonMax}\n      撿回插在牆上的魚叉可補充。\n\n" +
                $"［關鍵］邪神雕像　{s.SealCount} / {s.RequiredSeals}\n      刻有古老符文的邪神雕像。集齊即可在祭壇封印邪神。";
        }
    }
}
