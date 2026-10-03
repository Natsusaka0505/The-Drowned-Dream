using System;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 低 SAN 精神錯亂（F-SAN-11/12）。每隔 ConfusionCheckInterval 擲一次機率（SAN 越低機率越高），命中 → 預告 → 錯亂一段時間。
    /// 錯亂期間：每次按下 A/W/D 有機率換成另外兩個方向（按住沿用同一個結果）；按 Space 有機率延遲射出或沒射出。
    /// 移動與攻擊一律透過這裡讀取「錯亂後的輸入」（MoveX / JumpPressed / JumpHeld / FirePressed）。
    /// </summary>
    public class PlayerConfusion : MonoBehaviour
    {
        /// <summary>可互相替換的方向動作（索引同時代表實體按鍵：0 = A、1 = D、2 = W）。</summary>
        private enum Dir
        {
            /// <summary>往左（A）。</summary>
            Left,
            /// <summary>往右（D）。</summary>
            Right,
            /// <summary>跳躍（W）。</summary>
            Jump,
        }

        /// <summary>方向動作數量。</summary>
        private const int DirCount = 3;
        /// <summary>類比搖桿超過此值才算按下左 / 右。</summary>
        private const float AxisThreshold = 0.5f;

        /// <summary>玩家數值（SAN 比例）。</summary>
        private PlayerStatus _status;
        /// <summary>原始輸入。</summary>
        private PlayerInputReader _input;
        /// <summary>距離下次判定的秒數。</summary>
        private float _checkTimer;
        /// <summary>預告剩餘秒數。</summary>
        private float _warningTimer;
        /// <summary>錯亂剩餘秒數。</summary>
        private float _confusedTimer;

        /// <summary>各實體方向鍵上一幀是否按住。</summary>
        private readonly bool[] _rawHeld = new bool[DirCount];
        /// <summary>各實體方向鍵這次按下被換成的動作（放開前不變）。</summary>
        private readonly Dir[] _target = { Dir.Left, Dir.Right, Dir.Jump };
        /// <summary>錯亂後各動作是否按住。</summary>
        private readonly bool[] _logicalHeld = new bool[DirCount];
        /// <summary>已計算過錯亂後輸入的幀（同一幀只算一次，不受腳本執行順序影響）。</summary>
        private int _refreshedFrame = -1;
        /// <summary>錯亂後的水平輸入。</summary>
        private float _moveX;
        /// <summary>錯亂後本幀是否按下跳躍。</summary>
        private bool _jumpPressed;
        /// <summary>錯亂後跳躍是否按住。</summary>
        private bool _jumpHeld;
        /// <summary>錯亂後本幀是否發射。</summary>
        private bool _firePressed;
        /// <summary>延遲射出剩餘秒數（≤ 0 = 沒有排隊中的發射）。</summary>
        private float _fireDelayTimer;

        /// <summary>是否正在預告。</summary>
        public bool IsWarning => _warningTimer > 0f;
        /// <summary>是否正在錯亂。</summary>
        public bool IsConfused => _confusedTimer > 0f;

        /// <summary>錯亂後的水平輸入（-1 ~ 1）。</summary>
        public float MoveX { get { Refresh(); return _moveX; } }
        /// <summary>錯亂後本幀是否按下跳躍。</summary>
        public bool JumpPressed { get { Refresh(); return _jumpPressed; } }
        /// <summary>錯亂後跳躍是否按住（落地後自動連跳用）。</summary>
        public bool JumpHeld { get { Refresh(); return _jumpHeld; } }
        /// <summary>錯亂後本幀是否發射魚叉（含延遲射出）。</summary>
        public bool FirePressed { get { Refresh(); return _firePressed; } }

        /// <summary>開始預告。</summary>
        public event Action WarningStarted;
        /// <summary>開始錯亂。</summary>
        public event Action ConfusionStarted;
        /// <summary>錯亂結束。</summary>
        public event Action ConfusionEnded;

        /// <summary>SAN 參數。</summary>
        private SanityConfig Config => _status.SanityConfig;

        /// <summary>快取元件。</summary>
        private void Awake()
        {
            _status = GetComponent<PlayerStatus>();
            _input = GetComponent<PlayerInputReader>();
        }

        /// <summary>初始化判定計時。</summary>
        private void Start()
        {
            _checkTimer = Config.ConfusionCheckInterval;
        }

        /// <summary>每幀更新錯亂後的輸入，並推進預告 / 錯亂 / 判定。</summary>
        private void Update()
        {
            Refresh();
            if (!GameFlow.IsPlaying) return;
            float dt = Time.deltaTime;

            if (IsConfused)
            {
                _confusedTimer -= dt;
                if (_confusedTimer <= 0f) ConfusionEnded?.Invoke();
                return;
            }

            if (IsWarning)
            {
                _warningTimer -= dt;
                if (_warningTimer <= 0f)
                {
                    _confusedTimer = Config.ConfusionDuration;
                    ConfusionStarted?.Invoke();
                }
                return;
            }

            _checkTimer -= dt;
            if (_checkTimer > 0f) return;
            _checkTimer = Config.ConfusionCheckInterval;

            if (UnityEngine.Random.value < CurrentChance())
            {
                _warningTimer = Config.ConfusionWarningTime;
                WarningStarted?.Invoke();
            }
        }

        /// <summary>目前錯亂機率：SAN ≥ 起始比例為 0；低於起始比例時從最小機率線性增加到 SAN 0 時的最大機率。</summary>
        public float CurrentChance()
        {
            float ratio = (float)_status.SanityRatio;
            if (ratio >= Config.ConfusionStartRatio) return 0f;
            float t = 1f - ratio / Config.ConfusionStartRatio;
            return Mathf.Lerp(Config.ConfusionMinChance, Config.ConfusionMaxChance, t);
        }

        /// <summary>清除錯亂狀態與排隊中的延遲射出（復活用）。</summary>
        public void ResetConfusion()
        {
            bool wasConfused = IsConfused;
            _warningTimer = 0f;
            _confusedTimer = 0f;
            _fireDelayTimer = 0f;
            _checkTimer = Config.ConfusionCheckInterval;
            if (wasConfused) ConfusionEnded?.Invoke();
        }

        /// <summary>由原始輸入算出本幀錯亂後的輸入（同一幀只算一次）。</summary>
        private void Refresh()
        {
            if (_refreshedFrame == Time.frameCount) return;
            _refreshedFrame = Time.frameCount;
            RefreshDirections();
            RefreshFire();
        }

        /// <summary>A/W/D：剛按下時決定要變成哪個動作，按住期間沿用，再組合成水平輸入與跳躍。</summary>
        private void RefreshDirections()
        {
            float rawX = _input.MoveX;
            bool[] held = { rawX < -AxisThreshold, rawX > AxisThreshold, _input.JumpHeld };
            bool[] pressed = { held[0] && !_rawHeld[0], held[1] && !_rawHeld[1], _input.JumpPressed };

            bool remapped = false;
            _jumpPressed = false;
            Array.Clear(_logicalHeld, 0, DirCount);
            for (int i = 0; i < DirCount; i++)
            {
                if (pressed[i]) _target[i] = IsConfused && UnityEngine.Random.value < Config.SwapChance ? OtherDir(i) : (Dir)i;
                _rawHeld[i] = held[i];

                int t = (int)_target[i];
                if (held[i]) _logicalHeld[t] = true;
                if (pressed[i] && _target[i] == Dir.Jump) _jumpPressed = true;
                if ((held[i] || pressed[i]) && t != i) remapped = true;
            }

            // 沒有被替換時直接用原始輸入（保留搖桿的類比值）
            if (!remapped)
            {
                _moveX = rawX;
                _jumpHeld = _input.JumpHeld;
                return;
            }
            _moveX = (_logicalHeld[(int)Dir.Right] ? 1f : 0f) - (_logicalHeld[(int)Dir.Left] ? 1f : 0f);
            _jumpHeld = _logicalHeld[(int)Dir.Jump];
        }

        /// <summary>Space：錯亂中有機率沒射出或延遲射出；延遲時間到才算按下發射。</summary>
        private void RefreshFire()
        {
            _firePressed = false;
            if (_input.FirePressed)
            {
                float roll = UnityEngine.Random.value;
                if (!IsConfused) _firePressed = true;
                else if (roll < Config.FireMisfireChance) { } // 沒射出
                else if (roll < Config.FireMisfireChance + Config.FireDelayChance)
                {
                    var range = Config.FireDelayRange;
                    _fireDelayTimer = UnityEngine.Random.Range(range.x, range.y);
                }
                else _firePressed = true;
            }

            if (_fireDelayTimer <= 0f) return;
            _fireDelayTimer -= Time.deltaTime;
            if (_fireDelayTimer <= 0f && GameFlow.IsPlaying && _status.IsAlive) _firePressed = true; // 死亡 / 非遊玩中就作廢
        }

        /// <summary>隨機挑另外兩個方向之一。</summary>
        private static Dir OtherDir(int self) => (Dir)((self + UnityEngine.Random.Range(1, DirCount)) % DirCount);
    }
}
