using System.Collections;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 背景音樂播放器：依遊戲流程切換 BGM（開場 → 探索），切歌時淡出 / 淡入。音量 / 淡入淡出秒數可在 Inspector 調整。
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

        /// <summary>流程狀態 → 對應 BGM（暫停 / 結局沿用探索曲）。</summary>
        private void OnGameStateChanged(GameState state)
        {
            Play(state == GameState.Intro && _introClip != null ? _introClip : _exploreClip);
        }

        /// <summary>切換並播放指定 BGM（同一首則不重播）。</summary>
        public void Play(AudioClip clip)
        {
            if (clip == null || clip == _current) return;

            _current = clip;
            StopAllCoroutines();
            StartCoroutine(SwitchTo(clip));
        }

        /// <summary>舊曲淡出後換新曲並淡入。</summary>
        private IEnumerator SwitchTo(AudioClip clip)
        {
            if (_source.isPlaying && _fadeOutSeconds > 0f) yield return FadeTo(0f, _fadeOutSeconds);

            _source.clip = clip;
            _source.volume = _fadeInSeconds > 0f ? 0f : _volume;
            _source.Play();
            if (_fadeInSeconds > 0f) yield return FadeTo(_volume, _fadeInSeconds);
        }

        /// <summary>在指定秒數內把音量從目前值漸變到目標值（不受 timeScale 影響）。</summary>
        private IEnumerator FadeTo(float target, float seconds)
        {
            float start = _source.volume;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _source.volume = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            _source.volume = target;
        }
    }
}
