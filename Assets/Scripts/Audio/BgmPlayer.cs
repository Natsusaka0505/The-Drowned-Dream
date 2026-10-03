using System.Collections;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 背景音樂播放器：場景開始時循環播放 BGM，並淡入。音量 / 淡入秒數可在 Inspector 調整。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BgmPlayer : MonoBehaviour
    {
        /// <summary>要播放的 BGM。</summary>
        [SerializeField] private AudioClip _clip;
        /// <summary>目標音量（0~1）。</summary>
        [Range(0f, 1f)]
        [SerializeField] private float _volume = 0.6f;
        /// <summary>開場淡入秒數（0 = 直接播放）。</summary>
        [SerializeField] private float _fadeInSeconds = 2f;

        /// <summary>播放用 AudioSource。</summary>
        private AudioSource _source;

        /// <summary>設定 AudioSource 為 2D 循環播放。</summary>
        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
        }

        /// <summary>開始播放 BGM。</summary>
        private void Start()
        {
            Play(_clip);
        }

        /// <summary>切換並播放指定 BGM（同一首則不重播）。</summary>
        public void Play(AudioClip clip)
        {
            if (clip == null || (_source.clip == clip && _source.isPlaying)) return;

            StopAllCoroutines();
            _source.clip = clip;
            _source.Play();
            if (_fadeInSeconds > 0f) StartCoroutine(FadeTo(_volume, _fadeInSeconds));
            else _source.volume = _volume;
        }

        /// <summary>在指定秒數內把音量漸變到目標值。</summary>
        private IEnumerator FadeTo(float target, float seconds)
        {
            float start = _source.volume = 0f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _source.volume = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            _source.volume = target;
        }
    }
}
