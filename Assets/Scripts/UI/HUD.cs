using System;
using UnityEngine;
using UnityEngine.UI;

namespace DrownedDream
{
    /// <summary>
    /// 遊戲 HUD：玩家頭像 / HP 框 / SAN 框 / 氧氣泡泡 / 魚叉 / 封印 / 提示訊息。
    /// 憋氣條不顯示，憋氣時間改由 PlayerBreath 輸出到 Console。（F-OXY-04、F-SAN-01、F-WPN-05、F-INV-10）
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [Header("玩家頭像")]
        /// <summary>頭像圖。</summary>
        [SerializeField] private Sprite _portrait;
        /// <summary>頭像框圖（白底黑框，頭像疊在框內）。</summary>
        [SerializeField] private Sprite _portraitFrame;
        /// <summary>頭像框寬度（高度依框圖比例）。</summary>
        [SerializeField] private float _portraitWidth = 166f;

        [Header("魚叉數量（右上角）")]
        /// <summary>魚叉圖示（每支一個，用掉的變半透明）。</summary>
        [SerializeField] private Sprite _harpoonSprite;

        [Header("精靈提示")]
        /// <summary>精靈圖（進 Boss 房前的提示由精靈說）。</summary>
        [SerializeField] private Sprite _elfSprite;
        /// <summary>精靈對話框顯示時的不透明度（0~1）。</summary>
        [SerializeField, Range(0f, 1f)] private float _elfAlpha = 0.6f;
        /// <summary>一般提示訊息的不透明度（0~1）。</summary>
        [SerializeField, Range(0f, 1f)] private float _messageAlpha = 0.6f;

        [Header("封印道具箭頭")]
        /// <summary>一般封印道具寶箱的箭頭顏色。</summary>
        [SerializeField] private Color _sealArrowColor = new Color(0.7f, 0.3f, 0.95f);
        /// <summary>最後一個封印道具的箭頭顏色。</summary>
        [SerializeField] private Color _finalSealArrowColor = new Color(1f, 0.35f, 0.75f);

        [Header("封印進度（右上角）")]
        /// <summary>是否顯示右上角封印道具數量（2026-10-04 預設關閉，背包仍看得到）。</summary>
        [SerializeField] private bool _showSealCount;

        [Header("HP / SAN 框與填充條")]
        /// <summary>HP 框圖。</summary>
        [SerializeField] private Sprite _hpFrame;
        /// <summary>HP 填充條圖。</summary>
        [SerializeField] private Sprite _hpFill;
        /// <summary>SAN 框圖。</summary>
        [SerializeField] private Sprite _sanFrame;
        /// <summary>SAN 填充條圖。</summary>
        [SerializeField] private Sprite _sanFill;

        [Header("受擊紅閃")]
        /// <summary>玩家受擊時全畫面紅閃的顏色（alpha = 最強時的不透明度）。</summary>
        [SerializeField] private Color _hurtColor = new Color(0.8f, 0f, 0.05f, 0.3f);
        /// <summary>受擊紅閃淡出秒數。</summary>
        [SerializeField] private float _hurtSeconds = 0.4f;

        [Header("氧氣泡泡")]
        /// <summary>泡泡圖（依序用在第 1~N 顆）。</summary>
        [SerializeField] private Sprite[] _bubbleSprites;
        /// <summary>泡泡數量（氧氣滿時的顆數）。</summary>
        [SerializeField] private int _bubbleCount = 10;
        /// <summary>泡泡大小。</summary>
        [SerializeField] private float _bubbleSize = 34f;
        /// <summary>泡泡間距。</summary>
        [SerializeField] private float _bubbleSpacing = 4f;

        /// <summary>HP 條。</summary>
        private UIFrameBar _hp;
        /// <summary>SAN 條。</summary>
        private UIFrameBar _sanity;
        /// <summary>氧氣泡泡列。</summary>
        private UIBubbleRow _oxygen;
        /// <summary>精靈對話框。</summary>
        private ElfDialog _elf;
        /// <summary>封印道具寶箱的方向箭頭（每個未開的黑寶箱一支，不夠時再建立）。</summary>
        private readonly System.Collections.Generic.List<TargetArrow> _sealArrows = new System.Collections.Generic.List<TargetArrow>();
        /// <summary>HUD 根物件（建立箭頭用）。</summary>
        private RectTransform _root;
        /// <summary>是否已進過 Boss 房（之後提示改為一般文字，不再由精靈說）。</summary>
        private bool _bossMode;
        /// <summary>魚叉數文字（沒有圖示時使用）。</summary>
        private Text _harpoonText;
        /// <summary>魚叉圖示列的容器（右上角）。</summary>
        private RectTransform _harpoonRow;
        /// <summary>各支魚叉圖示。</summary>
        private readonly System.Collections.Generic.List<Image> _harpoonIcons = new System.Collections.Generic.List<Image>();
        /// <summary>封印進度文字。</summary>
        private Text _sealText;
        /// <summary>提示訊息文字。</summary>
        private Text _messageText;
        /// <summary>提示訊息剩餘秒數。</summary>
        private float _messageTimer;
        /// <summary>全畫面閃白圖。</summary>
        private Image _flash;
        /// <summary>閃白剩餘秒數。</summary>
        private float _flashTimer;
        /// <summary>閃白總秒數。</summary>
        private float _flashDuration;
        /// <summary>受擊紅閃圖（在 HUD 元件底下，不擋數值條）。</summary>
        private Image _hurt;
        /// <summary>受擊紅閃剩餘秒數。</summary>
        private float _hurtTimer;
        /// <summary>玩家快取。</summary>
        private Player _player;

        /// <summary>建立 HUD 元件。</summary>
        private void Awake()
        {
            var root = UIFactory.Stretch("HUD", transform);

            // 左側頭像，右側依序 HP / SAN / 氧氣泡泡
            BuildPortrait(root, new Vector2(30f, -30f));
            float barX = 30f + _portraitWidth + 12f;
            // 框圖比例 1872:297 → 寬 360 時高約 57
            var barSize = new Vector2(360f, 57f);
            _hp = new UIFrameBar(root, "HP", new Vector2(barX, -30f), barSize, _hpFrame, _hpFill, new Color(0.85f, 0.2f, 0.25f));
            _sanity = new UIFrameBar(root, "Sanity", new Vector2(barX, -95f), barSize, _sanFrame, _sanFill, new Color(0.6f, 0.35f, 0.85f), withCap: true);
            _oxygen = new UIBubbleRow(root, "Oxygen", new Vector2(barX, -162f), _bubbleSize, _bubbleSpacing, _bubbleSprites, Mathf.Max(1, _bubbleCount));

            var harpoonRt = UIFactory.Rect("Harpoons", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(500f, 40f));
            _harpoonText = UIFactory.Text(harpoonRt, "", 26, TextAnchor.UpperRight, Color.white);
            _harpoonRow = UIFactory.Rect("HarpoonIcons", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -20f), new Vector2(500f, 70f));

            var sealRt = UIFactory.Rect("Seals", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -100f), new Vector2(600f, 40f)); // 魚叉圖示列下方
            _sealText = UIFactory.Text(sealRt, "", 24, TextAnchor.UpperRight, new Color(1f, 0.9f, 0.6f));
            sealRt.gameObject.SetActive(_showSealCount);

            var msgRt = UIFactory.Rect("Message", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1400f, 60f));
            _messageText = UIFactory.Text(msgRt, "", 30, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.8f));

            _elf = new ElfDialog(root, _elfSprite, _elfAlpha);
            _root = root;

            var helpRt = UIFactory.Rect("Help", root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 20f), new Vector2(1400f, 30f));
            UIFactory.Text(helpRt, "A/D 移動  W 跳  S 穿過平台往下  Space 魚叉  Q 憋氣  E 互動  Tab 背包", 18, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.5f));

            // 全畫面閃白（最上層，平常透明）
            _flash = UIFactory.Image(UIFactory.Stretch("ScreenFlash", root), new Color(1f, 1f, 1f, 0f));
            _flash.raycastTarget = false;

            // 受擊紅閃（放最底層，數值條仍清楚）
            var hurtRt = UIFactory.Stretch("HurtFlash", root);
            hurtRt.SetAsFirstSibling();
            _hurt = UIFactory.Image(hurtRt, Color.clear);
            _hurt.raycastTarget = false;
        }

        /// <summary>建立頭像：框圖在底，頭像疊在框內（框圖 726×697，邊框約 28 像素）。</summary>
        private void BuildPortrait(RectTransform root, Vector2 pos)
        {
            float aspect = _portraitFrame != null ? _portraitFrame.rect.height / _portraitFrame.rect.width : 697f / 726f;
            var size = new Vector2(_portraitWidth, _portraitWidth * aspect);
            var frameRt = UIFactory.Rect("Portrait", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);
            var frame = UIFactory.Image(frameRt, _portraitFrame != null ? Color.white : Color.black);
            frame.sprite = _portraitFrame;

            var faceRt = UIFactory.Stretch("Face", frameRt);
            var inset = new Vector2(size.x * 28f / 726f, size.y * 28f / 697f);
            faceRt.offsetMin = inset;
            faceRt.offsetMax = -inset;
            var face = UIFactory.Image(faceRt, Color.white);
            face.sprite = _portrait;
            face.enabled = _portrait != null;
        }

        /// <summary>訂閱提示訊息與 Boss 現身。</summary>
        private void OnEnable()
        {
            GameEvents.MessageRequested += ShowMessage;
            GameEvents.BossRevealed += OnBossRevealed;
            GameEvents.ScreenFlashRequested += OnScreenFlash;
        }

        /// <summary>取消訂閱。</summary>
        private void OnDisable()
        {
            GameEvents.MessageRequested -= ShowMessage;
            GameEvents.BossRevealed -= OnBossRevealed;
            GameEvents.ScreenFlashRequested -= OnScreenFlash;
        }

        /// <summary>開始閃白：立刻全白，seconds 秒內淡出。</summary>
        private void OnScreenFlash(float seconds)
        {
            _flashDuration = Mathf.Max(0.05f, seconds);
            _flashTimer = _flashDuration;
        }

        /// <summary>第一次進 Boss 房：精靈退場，之後提示改為一般文字。</summary>
        private void OnBossRevealed()
        {
            _bossMode = true;
            _elf.Hide();
        }

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
            status.Damaged += OnDamaged;
            status.NotifyAll(); // 訂閱前可能已錯過初始事件，主動同步一次
        }

        /// <summary>玩家受擊：開始紅閃。</summary>
        private void OnDamaged() => _hurtTimer = Mathf.Max(0.05f, _hurtSeconds);

        /// <summary>更新 HP 條。</summary>
        private void OnHealth(double c, double m) => _hp.Set((float)(c / m), $"HP  {Math.Ceiling(c):0} / {m:0}"); // 無條件進位：還活著就不會顯示 0

        /// <summary>更新氧氣泡泡（依目前氧氣 / 目前上限的百分比）。</summary>
        private void OnOxygen(double c, double m) => _oxygen.Set(m > 0d ? (float)(c / m) : 0f);

        /// <summary>更新 SAN 條（含失去的最大值）。</summary>
        private void OnSanity(double c, double m, double b)
        {
            _sanity.Set((float)(c / b), $"SAN  {c:0} / {m:0}");
            _sanity.SetCap((float)(m / b));
        }

        /// <summary>更新封印進度。</summary>
        private void OnSeals(int count, int required) => _sealText.text = $"封印  {count} / {required}";

        /// <summary>更新魚叉數：每支一個斜放的魚叉圖示（由右往左排），用掉的變半透明；沒有圖時用文字。</summary>
        private void OnAmmo(int a, int m)
        {
            if (_harpoonSprite == null)
            {
                _harpoonText.text = $"魚叉  {new string('■', a)}{new string('□', m - a)}";
                return;
            }
            _harpoonText.text = "";
            while (_harpoonIcons.Count < m)
            {
                int i = _harpoonIcons.Count;
                var rt = UIFactory.Rect($"Harpoon{i}", _harpoonRow, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-30f - i * 34f, 0f), new Vector2(84f, 15f));
                rt.localRotation = Quaternion.Euler(0f, 0f, 55f); // 斜放，像美術原圖
                var img = UIFactory.Image(rt, Color.white);
                img.sprite = _harpoonSprite;
                img.preserveAspect = true;
                _harpoonIcons.Add(img);
            }
            for (int i = 0; i < _harpoonIcons.Count; i++)
            {
                _harpoonIcons[i].gameObject.SetActive(i < m);
                _harpoonIcons[i].color = i < a ? Color.white : new Color(1f, 1f, 1f, 0.2f); // 右邊起算：還在手上的是實心
            }
        }

        /// <summary>更新數值條 / 泡泡動畫並淡出提示訊息。</summary>
        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _hp.Tick(dt);
            _sanity.Tick(dt);
            _oxygen.Tick(dt);
            _elf.Tick(dt);
            TickSealArrows();

            if (_hurtTimer > 0f)
            {
                _hurtTimer -= dt;
                var hc = _hurtColor;
                hc.a *= Mathf.Clamp01(_hurtTimer / Mathf.Max(0.05f, _hurtSeconds));
                _hurt.color = hc;
            }

            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                _flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(_flashTimer / _flashDuration));
            }

            if (_messageTimer > 0f)
            {
                _messageTimer -= Time.unscaledDeltaTime;
                var c = _messageText.color;
                c.a = Mathf.Clamp01(_messageTimer / 0.4f) * _messageAlpha;
                _messageText.color = c;
            }
        }

        /// <summary>每個未開的封印道具寶箱各一支箭頭（最後一個粉紅色，其餘紫色），多出來的箭頭隱藏。</summary>
        private void TickSealArrows()
        {
            int used = 0;
            foreach (var chest in TreasureChest.All)
            {
                if (chest == null || chest.IsOpened || !(chest.ItemPrefab is SealItem)) continue;
                bool final = chest.IsFinalSeal;
                if (used >= _sealArrows.Count) _sealArrows.Add(null);
                // 顏色依是否為最後一個決定，同一支箭頭顏色不同時重建
                var color = final ? _finalSealArrowColor : _sealArrowColor;
                if (_sealArrows[used] == null || _sealArrows[used].Color != color)
                {
                    _sealArrows[used]?.Destroy();
                    _sealArrows[used] = new TargetArrow(_root, color);
                }
                _sealArrows[used].Tick(chest.transform, final ? "最後的雕像" : "雕像");
                used++;
            }
            for (int i = used; i < _sealArrows.Count; i++) _sealArrows[i]?.Tick(null, null);
        }

        /// <summary>顯示提示訊息：進 Boss 房前由精靈說（左下對話框），之後用畫面下方一般文字。</summary>
        private void ShowMessage(string text, float duration)
        {
            if (!_bossMode)
            {
                _elf.Show(text, duration);
                return;
            }
            _messageText.text = text;
            _messageTimer = duration;
            var c = _messageText.color;
            c.a = _messageAlpha;
            _messageText.color = c;
        }
    }
}
