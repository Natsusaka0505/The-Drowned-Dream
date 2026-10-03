using System.Collections.Generic;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 音效與環境音（SD-02 音效）：只訂閱事件，不被其他系統直接呼叫。
    /// 單次音效用多個聲道輪流播；環境音（風聲 / 低 SAN / 憋氣 / Boss 房 / 腳步）依狀態淡入淡出，憋氣時壓低 BGM。
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        /// <summary>音效設定。</summary>
        [SerializeField] private AudioConfig _config;
        /// <summary>單次音效同時可播的聲道數。</summary>
        [SerializeField] private int _voices = 8;

        /// <summary>單次音效聲道。</summary>
        private AudioSource[] _pool;
        /// <summary>下一個要用的聲道。</summary>
        private int _nextVoice;
        /// <summary>各音效上次播放時間（冷卻用）。</summary>
        private readonly Dictionary<SfxEntry, float> _lastPlayed = new Dictionary<SfxEntry, float>();

        /// <summary>腳步循環。</summary>
        private AudioSource _footsteps;
        /// <summary>洞窟風聲循環。</summary>
        private AudioSource _caveWind;
        /// <summary>低 SAN 循環。</summary>
        private AudioSource _lowSanity;
        /// <summary>憋氣中循環。</summary>
        private AudioSource _breathLoop;
        /// <summary>Boss 房循環。</summary>
        private AudioSource _bossHall;

        /// <summary>玩家（訂閱玩家元件事件、讀取狀態）。</summary>
        private Player _player;
        /// <summary>玩家剛體（腳步聲判斷速度）。</summary>
        private Rigidbody2D _playerBody;
        /// <summary>Boss 房（判斷玩家是否在房內）。</summary>
        private BossArea _bossArea;
        /// <summary>BGM 播放器（憋氣時壓低）。</summary>
        private BgmPlayer _bgm;
        /// <summary>上一次的 SAN 分段（判斷往下掉還是回升）。</summary>
        private int _lastSanityStage;

        /// <summary>建立單次音效聲道與各循環層。</summary>
        private void Awake()
        {
            _pool = new AudioSource[Mathf.Max(1, _voices)];
            for (int i = 0; i < _pool.Length; i++) _pool[i] = NewSource($"Voice{i}", null);
            if (_config == null) return;
            _footsteps = NewSource("Footsteps", _config.Footsteps.Clip);
            _caveWind = NewSource("CaveWind", _config.CaveWind.Clip);
            _lowSanity = NewSource("LowSanity", _config.LowSanity.Clip);
            _breathLoop = NewSource("BreathLoop", _config.BreathLoop.Clip);
            _bossHall = NewSource("BossHall", _config.BossHall.Clip);
        }

        /// <summary>訂閱全域事件。</summary>
        private void OnEnable()
        {
            GameEvents.EnemyHit += OnEnemyHit;
            GameEvents.RecoveryUsed += OnRecoveryUsed;
            GameEvents.BossRevealed += OnBossRoar;
            GameEvents.BossActivated += OnBossRoar;
            GameEvents.BossSealed += OnBossSealed;
        }

        /// <summary>取消訂閱全域事件。</summary>
        private void OnDisable()
        {
            GameEvents.EnemyHit -= OnEnemyHit;
            GameEvents.RecoveryUsed -= OnRecoveryUsed;
            GameEvents.BossRevealed -= OnBossRoar;
            GameEvents.BossActivated -= OnBossRoar;
            GameEvents.BossSealed -= OnBossSealed;
        }

        /// <summary>找出玩家 / Boss 房 / BGM 並訂閱玩家元件事件（Player 在 Awake 註冊，因此放在 Start）。</summary>
        private void Start()
        {
            _bossArea = FindFirstObjectByType<BossArea>();
            _bgm = FindFirstObjectByType<BgmPlayer>();
            _player = Player.Instance;
            if (_player == null) return;
            _playerBody = _player.GetComponent<Rigidbody2D>();
            _player.Move.Jumped += OnJumped;
            _player.Move.Landed += OnLanded;
            _player.Attack.Thrown += OnThrown;
            _player.Breath.StateChanged += OnBreathStateChanged;
            _player.Confusion.WarningStarted += OnConfusionWarning;
            _player.Status.SanityStageChanged += OnSanityStageChanged;
        }

        /// <summary>取消訂閱玩家元件事件。</summary>
        private void OnDestroy()
        {
            if (_player == null) return;
            _player.Move.Jumped -= OnJumped;
            _player.Move.Landed -= OnLanded;
            _player.Attack.Thrown -= OnThrown;
            _player.Breath.StateChanged -= OnBreathStateChanged;
            _player.Confusion.WarningStarted -= OnConfusionWarning;
            _player.Status.SanityStageChanged -= OnSanityStageChanged;
        }

        /// <summary>依目前狀態把各循環層淡到目標音量，憋氣時壓低 BGM。</summary>
        private void Update()
        {
            if (_config == null) return;
            bool playing = GameFlow.IsPlaying;
            bool holding = _player != null && _player.Status.IsHoldingBreath;

            // 洞窟風聲：封面 / 開場以外一直有
            Fade(_caveWind, GameFlow.State is GameState.Title or GameState.Intro ? 0f : _config.CaveWind.Volume);
            // 低 SAN：低於起始比例開始淡入，SAN 0 最大聲
            float sanT = 0f;
            if (_player != null && _config.LowSanityStartRatio > 0f)
                sanT = Mathf.Clamp01(1f - (float)_player.Status.SanityRatio / _config.LowSanityStartRatio);
            Fade(_lowSanity, sanT * _config.LowSanity.Volume);
            // 憋氣中
            Fade(_breathLoop, holding ? _config.BreathLoop.Volume : 0f);
            // Boss 房
            Fade(_bossHall, _bossArea != null && _bossArea.PlayerInside ? _config.BossHall.Volume : 0f);
            // 腳步：遊玩中、站在地上、有在走
            bool walking = playing && _player != null && _playerBody != null && _player.Move.IsGrounded
                           && Mathf.Abs(_playerBody.linearVelocity.x) >= _config.FootstepMinSpeed;
            Fade(_footsteps, walking ? _config.Footsteps.Volume : 0f);

            if (_bgm != null) _bgm.Duck = holding ? _config.MusicDuckWhileHolding : 1f;
        }

        /// <summary>播放單次音效（clip 為空或冷卻中則略過）。</summary>
        private void Play(SfxEntry entry)
        {
            if (entry == null || entry.Clip == null) return;
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(entry, out float last) && now - last < entry.Cooldown) return;
            _lastPlayed[entry] = now;

            var source = _pool[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _pool.Length;
            source.pitch = entry.Pitch + Random.Range(-entry.PitchJitter, entry.PitchJitter);
            source.PlayOneShot(entry.Clip, entry.Volume);
        }

        /// <summary>把循環層音量往目標移動（不受 timeScale 影響）。</summary>
        private void Fade(AudioSource source, float target)
        {
            if (source == null || source.clip == null) return;
            source.volume = Mathf.MoveTowards(source.volume, target, _config.LayerFadeSpeed * Time.unscaledDeltaTime);
        }

        /// <summary>建立子物件 AudioSource（2D；有 clip 時以音量 0 開始循環）。</summary>
        private AudioSource NewSource(string name, AudioClip loopClip)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            if (loopClip == null) return source;
            source.clip = loopClip;
            source.loop = true;
            source.volume = 0f;
            source.Play();
            return source;
        }

        #region 事件 → 音效

        /// <summary>起跳。</summary>
        private void OnJumped() => Play(_config.Jump);

        /// <summary>落地：下落夠快才播。</summary>
        private void OnLanded(float fallSpeed)
        {
            if (fallSpeed >= _config.LandMinFallSpeed) Play(_config.Land);
        }

        /// <summary>發射魚叉。</summary>
        private void OnThrown() => Play(_config.HarpoonThrow);

        /// <summary>敵人被命中：擊殺那一下播擊殺音，否則播命中音。</summary>
        private void OnEnemyHit(bool killed) => Play(killed ? _config.EnemyKill : _config.HarpoonHit);

        /// <summary>使用回復道具。</summary>
        private void OnRecoveryUsed() => Play(_config.Eat);

        /// <summary>開始憋氣。</summary>
        private void OnBreathStateChanged(BreathState state)
        {
            if (state == BreathState.Holding) Play(_config.BreathHold);
        }

        /// <summary>Boss 現身 / 啟動（冷卻內不重播，避免特寫後馬上啟動又吼一次）。</summary>
        private void OnBossRoar() => Play(_config.BossRoar);

        /// <summary>封印完成。</summary>
        private void OnBossSealed() => Play(_config.SealComplete);

        /// <summary>精神錯亂預告：低語。</summary>
        private void OnConfusionWarning() => Play(_config.Whisper);

        /// <summary>SAN 掉進更低分段（幻覺出現）：驚嚇。</summary>
        private void OnSanityStageChanged(int stage)
        {
            if (stage > _lastSanityStage) Play(_config.JumpScare);
            _lastSanityStage = stage;
        }

        #endregion
    }
}
