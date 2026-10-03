using UnityEngine;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>
    /// 遊戲 HUD：HP / 氧氣 / SAN / 憋氣 / 魚叉 / 封印 / 消耗品 / 提示訊息。
    /// （F-OXY-04、F-BRE-06、F-SAN-01、F-WPN-05、F-INV-10）
    /// </summary>
    public class HUD : MonoBehaviour
    {
        /// <summary>HP 條。</summary>
        private UIBar _hp;
        /// <summary>氧氣條。</summary>
        private UIBar _oxygen;
        /// <summary>SAN 條。</summary>
        private UIBar _sanity;
        /// <summary>憋氣條。</summary>
        private UIBar _breath;
        /// <summary>魚叉數文字。</summary>
        private Text _harpoonText;
        /// <summary>封印進度文字。</summary>
        private Text _sealText;
        /// <summary>提示訊息文字。</summary>
        private Text _messageText;
        /// <summary>提示訊息剩餘秒數。</summary>
        private float _messageTimer;
        /// <summary>玩家快取。</summary>
        private Player _player;

        /// <summary>建立 HUD 元件。</summary>
        private void Awake()
        {
            var root = UIFactory.Stretch("HUD", transform);

            _hp = new UIBar(root, "HP", new Vector2(30f, -30f), new Vector2(360f, 32f), new Color(0.85f, 0.2f, 0.25f));
            _oxygen = new UIBar(root, "Oxygen", new Vector2(30f, -70f), new Vector2(360f, 32f), new Color(0.2f, 0.7f, 0.95f));
            _sanity = new UIBar(root, "Sanity", new Vector2(30f, -110f), new Vector2(360f, 32f), new Color(0.6f, 0.35f, 0.85f), withCap: true);
            _breath = new UIBar(root, "Breath", new Vector2(30f, -150f), new Vector2(360f, 26f), new Color(0.7f, 0.95f, 1f));

            var harpoonRt = UIFactory.Rect("Harpoons", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(500f, 40f));
            _harpoonText = UIFactory.Text(harpoonRt, "", 26, TextAnchor.UpperRight, Color.white);

            var sealRt = UIFactory.Rect("Seals", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -75f), new Vector2(600f, 40f));
            _sealText = UIFactory.Text(sealRt, "", 24, TextAnchor.UpperRight, new Color(1f, 0.9f, 0.6f));

            var msgRt = UIFactory.Rect("Message", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1400f, 60f));
            _messageText = UIFactory.Text(msgRt, "", 30, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.8f));

            var helpRt = UIFactory.Rect("Help", root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 20f), new Vector2(1400f, 30f));
            UIFactory.Text(helpRt, "A/D 移動  W 跳  Space 魚叉  K 憋氣  E 互動  Tab 背包", 18, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.5f));
        }

        /// <summary>訂閱提示訊息。</summary>
        private void OnEnable() => GameEvents.MessageRequested += ShowMessage;

        /// <summary>取消訂閱提示訊息。</summary>
        private void OnDisable() => GameEvents.MessageRequested -= ShowMessage;

        /// <summary>訂閱玩家數值事件並同步初始值。</summary>
        private void Start()
        {
            _player = Player.Instance;
            if (_player == null) return;
            var status = _player.Status;
            status.HpChanged += OnHealth;
            status.OxygenChanged += OnOxygen;
            status.SanityChanged += OnSanity;
            status.HarpoonCountChanged += OnAmmo;
            status.SealCountChanged += OnSeals;
            status.NotifyAll(); // 訂閱前可能已錯過初始事件，主動同步一次
        }

        /// <summary>更新 HP 條。</summary>
        private void OnHealth(double c, double m) => _hp.Set((float)(c / m), $"HP  {c:0} / {m:0}");

        /// <summary>更新氧氣條。</summary>
        private void OnOxygen(double c, double m) => _oxygen.Set((float)(c / m), c <= 0d ? "氧氣  0 —— 窒息中！" : $"氧氣  {c:0} / {m:0}");

        /// <summary>更新 SAN 條（含失去的最大值）。</summary>
        private void OnSanity(double c, double m, double b)
        {
            _sanity.Set((float)(c / b), $"SAN  {c:0} / {m:0}");
            _sanity.SetCap((float)(m / b));
        }

        /// <summary>更新封印進度。</summary>
        private void OnSeals(int count, int required) => _sealText.text = $"封印  {count} / {required}";

        /// <summary>更新魚叉數。</summary>
        private void OnAmmo(int a, int m) => _harpoonText.text = $"魚叉  {new string('■', a)}{new string('□', m - a)}";

        /// <summary>更新憋氣條並淡出提示訊息。</summary>
        private void Update()
        {
            if (_player != null) RefreshBreath();

            if (_messageTimer > 0f)
            {
                _messageTimer -= Time.unscaledDeltaTime;
                var c = _messageText.color;
                c.a = Mathf.Clamp01(_messageTimer / 0.4f);
                _messageText.color = c;
            }
        }

        /// <summary>依憋氣狀態顯示剩餘時間或 CD。</summary>
        private void RefreshBreath()
        {
            var b = _player.Breath;
            double cd = _player.Status.BreathCooldown;
            switch (b.State)
            {
                case BreathState.Ready:
                    _breath.Set(1f, "憋氣  就緒 [K]");
                    break;
                case BreathState.Holding:
                    _breath.Set(b.HoldRemaining / b.MaxHoldTime, $"憋氣中（隱形）  {b.HoldRemaining:0.0}s");
                    break;
                case BreathState.Cooldown:
                    float ratio = b.CooldownTotal > 0f ? 1f - (float)cd / b.CooldownTotal : 1f;
                    _breath.Set(ratio, $"憋氣 CD  {cd:0.0}s");
                    break;
            }
        }

        /// <summary>顯示提示訊息。</summary>
        private void ShowMessage(string text, float duration)
        {
            _messageText.text = text;
            _messageTimer = duration;
            var c = _messageText.color;
            c.a = 1f;
            _messageText.color = c;
        }
    }
}
