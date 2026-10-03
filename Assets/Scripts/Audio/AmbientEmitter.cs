using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 有位置感的循環音（例如憋氣屏障的水聲）：依玩家與此物件的 2D 距離調整音量。
    /// 用 2D 距離計算而不是 3D 音效，避免攝影機 z 軸距離讓聲音永遠聽不到。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AmbientEmitter : MonoBehaviour
    {
        /// <summary>循環音檔。</summary>
        [SerializeField] private AudioClip _clip;
        /// <summary>最大音量（0~1）。</summary>
        [Range(0f, 1f)] [SerializeField] private float _volume = 0.8f;
        /// <summary>距離小於此值（單位）時為最大音量。</summary>
        [SerializeField] private float _innerRadius = 2f;
        /// <summary>距離大於此值（單位）時完全聽不到。</summary>
        [SerializeField] private float _outerRadius = 10f;

        /// <summary>播放用 AudioSource。</summary>
        private AudioSource _source;

        /// <summary>設定為 2D 循環並開始播放（音量由距離決定）。</summary>
        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.loop = true;
            _source.clip = _clip;
            _source.volume = 0f;
            if (_clip != null) _source.Play();
        }

        /// <summary>依玩家距離更新音量。</summary>
        private void Update()
        {
            var player = Player.Instance;
            if (player == null || _clip == null) return;
            float dist = Vector2.Distance(player.transform.position, transform.position);
            float t = 1f - Mathf.InverseLerp(_innerRadius, _outerRadius, dist);
            _source.volume = _volume * t;
        }
    }
}
