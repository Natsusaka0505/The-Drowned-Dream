using System.Collections;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 背景音樂播放器：依遊戲流程切換 BGM（開場 → 探索），切歌時淡出 / 淡入。音量 / 淡入淡出秒數可在 Inspector 調整。
    /// 除了訂閱狀態事件，每幀也會比對目前狀態該播的曲子，漏接事件（例如 Play 中重新編譯）也能自動切回正確的曲子。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BgmPlayer : MonoBehaviour
    {
        /// <summary>開場演出時播放的 BGM。</summary>
        [SerializeField] private AudioClip _introClip;
        /// <summary>遊玩（探索）時播放的 BGM。</summary>
        [SerializeField] private AudioClip _exploreClip;
        /// <summary>目標音量（0~1）。</summary>
        [Range(0f, 1f)]
        [SerializeField] private float _volume = 0.6f;
        /// <summary>淡入秒數（0 = 直接播放）。</summary>
        [SerializeField] private float _fadeInSeconds = 2f;
        /// <summary>切歌時舊曲淡出秒數（0 = 直接切）。</summary>
        [SerializeField] private float _fadeOutSeconds = 1f;

        /// <summary>播放用 AudioSource。</summary>
        private AudioSource _source;
        /// <summary>目前要播放（或正在切換過去）的曲子。</summary>
        private AudioClip _current;
        /// <summary>是否正在淡出 / 淡入（不參與熱重載序列化，重新編譯後會歸零）。</summary>
        [System.NonSerialized] private bool _fading;
        /// <summary>切歌淡入淡出的程度（0~1，由協程控制）。</summary>
        private float _fadeLevel;
        /// <summary>目前的壓低倍率（往 Duck 平滑靠近）。</summary>
        private float _duckLevel = 1f;

        /// <summary>壓低倍率目標（1 = 正常；例如憋氣時由 GameAudio 設為 0.4）。</summary>
        public float Duck { get; set; } = 1f;

        /// <summary>設定 AudioSource 為 2D 循環播放。</summary>
        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
        }

        /// <summary>訂閱流程狀態切換。</summary>
        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        /// <summary>取消訂閱流程狀態切換。</summary>
        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        /// <summary>依目前流程狀態播放對應 BGM（尚未收到事件時的初始播放）。</summary>
        private void Start()
        {
            if (_current == null) OnGameStateChanged(GameFlow.State);
        }

        /// <summary>每幀確認曲子與流程狀態一致（淡入淡出被中斷時拉回正常），並組合音量 = 設定音量 × 淡入淡出 × 壓低。</summary>
        private void Update()
        {
            var want = ClipFor(GameFlow.State);
            if (want != null && want != _current) Play(want);
            else if (want != null && !_fading)
            {
                if (_source.clip != _current) _source.clip = _current;
                _fadeLevel = 1f;
                if (!_source.isPlaying) _source.Play();
            }

            _duckLevel = Mathf.MoveTowards(_duckLevel, Duck, 2f * Time.unscaledDeltaTime);
            _source.volume = _volume * _fadeLevel * _duckLevel;
        }

        /// <summary>流程狀態事件 → 切到對應 BGM。</summary>
        private void OnGameStateChanged(GameState state) => Play(ClipFor(state));

        /// <summary>流程狀態對應的曲子：開場放開場曲，其餘（遊玩 / 暫停 / 結局）放探索曲。</summary>
        private AudioClip ClipFor(GameState state) => state == GameState.Intro && _introClip != null ? _introClip : _exploreClip;

        /// <summary>切換並播放指定 BGM（同一首則不重播）。曲子一律由流程狀態決定，因此不開放外部呼叫。</summary>
        private void Play(AudioClip clip)
        {
            if (clip == null || clip == _current) return;

            _current = clip;
            Debug.Log($"[DrownedDream] BGM → {clip.name}");
            StopAllCoroutines();
            StartCoroutine(SwitchTo(clip));
        }

        /// <summary>舊曲淡出後換新曲並淡入。</summary>
        private IEnumerator SwitchTo(AudioClip clip)
        {
            _fading = true;
            if (_source.isPlaying && _fadeOutSeconds > 0f) yield return FadeTo(0f, _fadeOutSeconds);

            _source.clip = clip;
            _fadeLevel = _fadeInSeconds > 0f ? 0f : 1f;
            _source.Play();
            if (_fadeInSeconds > 0f) yield return FadeTo(1f, _fadeInSeconds);
            _fading = false;
        }

        /// <summary>在指定秒數內把淡入淡出程度從目前值漸變到目標值（不受 timeScale 影響）。</summary>
        private IEnumerator FadeTo(float target, float seconds)
        {
            float start = _fadeLevel;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _fadeLevel = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            _fadeLevel = target;
        }
    }
}
