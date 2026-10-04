using System;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>單次音效設定：clip、音量、音高、冷卻。clip 留空 = 不播放。</summary>
    [Serializable]
    public class SfxEntry
    {
        /// <summary>音效檔</summary>
        [SerializeField] private AudioClip _clip;
        /// <summary>音量（0~1）</summary>
        [Range(0f, 1f)] [SerializeField] private float _volume = 1f;
        /// <summary>基準音高（1 = 原音）</summary>
        [Range(0.3f, 2f)] [SerializeField] private float _pitch = 1f;
        /// <summary>每次播放的音高隨機幅度（±，避免重複聽起來太機械）</summary>
        [Range(0f, 0.5f)] [SerializeField] private float _pitchJitter = 0.05f;
        /// <summary>同一音效最短間隔秒數（避免同時連發疊爆）</summary>
        [SerializeField] private float _cooldown = 0.05f;

        /// <summary>音效檔（唯讀）</summary>
        public AudioClip Clip => _clip;
        /// <summary>音量（唯讀）</summary>
        public float Volume => _volume;
        /// <summary>基準音高（唯讀）</summary>
        public float Pitch => _pitch;
        /// <summary>音高隨機幅度（唯讀）</summary>
        public float PitchJitter => _pitchJitter;
        /// <summary>最短間隔秒數（唯讀）</summary>
        public float Cooldown => _cooldown;

        /// <summary>Unity 反序列化用的無參數建構子。</summary>
        public SfxEntry() { }

        /// <summary>建立預設值（clip 由場景產生器綁定）。</summary>
        public SfxEntry(float volume, float pitch = 1f, float pitchJitter = 0.05f, float cooldown = 0.05f)
        {
            _volume = volume;
            _pitch = pitch;
            _pitchJitter = pitchJitter;
            _cooldown = cooldown;
        }
    }

    /// <summary>循環音設定：clip、最大音量。clip 留空 = 不播放。</summary>
    [Serializable]
    public class LoopEntry
    {
        /// <summary>循環音檔</summary>
        [SerializeField] private AudioClip _clip;
        /// <summary>最大音量（0~1）</summary>
        [Range(0f, 1f)] [SerializeField] private float _volume = 0.5f;

        /// <summary>循環音檔（唯讀）</summary>
        public AudioClip Clip => _clip;
        /// <summary>最大音量（唯讀）</summary>
        public float Volume => _volume;

        /// <summary>Unity 反序列化用的無參數建構子。</summary>
        public LoopEntry() { }

        /// <summary>建立預設值（clip 由場景產生器綁定）。</summary>
        public LoopEntry(float volume) { _volume = volume; }
    }

    /// <summary>音效與環境音設定（SD-02 音效）。預設音量已依素材響度拉平，企劃可在 Inspector 微調。</summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Audio Config", fileName = "AudioConfig")]
    public class AudioConfig : ScriptableObject
    {
        [Header("玩家")]
        /// <summary>跳躍</summary>
        [SerializeField] private SfxEntry _jump = new SfxEntry(0.45f);
        /// <summary>落地</summary>
        [SerializeField] private SfxEntry _land = new SfxEntry(0.8f, cooldown: 0.15f);
        /// <summary>發射魚叉</summary>
        [SerializeField] private SfxEntry _harpoonThrow = new SfxEntry(0.8f);
        /// <summary>開始憋氣</summary>
        [SerializeField] private SfxEntry _breathHold = new SfxEntry(0.7f);
        /// <summary>使用回復道具</summary>
        [SerializeField] private SfxEntry _eat = new SfxEntry(1f);
        /// <summary>主角受擊</summary>
        [SerializeField] private SfxEntry _playerHurt = new SfxEntry(0.9f, cooldown: 0.2f);
        /// <summary>重生復活</summary>
        [SerializeField] private SfxEntry _respawn = new SfxEntry(0.7f, pitchJitter: 0f, cooldown: 1f);

        [Header("敵人 / Boss")]
        /// <summary>魚叉命中敵人</summary>
        [SerializeField] private SfxEntry _harpoonHit = new SfxEntry(0.9f);
        /// <summary>擊殺（預設借用命中音、音高壓低）</summary>
        [SerializeField] private SfxEntry _enemyKill = new SfxEntry(1f, pitch: 0.8f);
        /// <summary>Boss 咆哮（現身 / 啟動）</summary>
        [SerializeField] private SfxEntry _bossRoar = new SfxEntry(0.9f, pitchJitter: 0f, cooldown: 6f);
        /// <summary>封印完成（預設借用 Boss 咆哮、音高壓低）</summary>
        [SerializeField] private SfxEntry _sealComplete = new SfxEntry(1f, pitch: 0.7f, pitchJitter: 0f, cooldown: 1f);
        /// <summary>被怪物偵測到（多隻同時發現只響一次）</summary>
        [SerializeField] private SfxEntry _detected = new SfxEntry(0.8f, cooldown: 3f);
        /// <summary>啟動封印祭壇（鎖鏈）</summary>
        [SerializeField] private SfxEntry _sealChain = new SfxEntry(1f, pitchJitter: 0f, cooldown: 0.5f);
        /// <summary>Boss 追蹤彈</summary>
        [SerializeField] private SfxEntry _bossHoming = new SfxEntry(0.7f, cooldown: 0.5f);
        /// <summary>Boss 落雷（每道都響，冷卻略短於落雷間隔）</summary>
        [SerializeField] private SfxEntry _bossLightning = new SfxEntry(0.8f, cooldown: 0.15f);

        [Header("SAN")]
        /// <summary>低語（精神錯亂預告）</summary>
        [SerializeField] private SfxEntry _whisper = new SfxEntry(0.8f, cooldown: 1f);
        /// <summary>驚嚇（SAN 掉進更低分段）</summary>
        [SerializeField] private SfxEntry _jumpScare = new SfxEntry(1f, pitchJitter: 0f, cooldown: 2f);

        [Header("循環音")]
        /// <summary>腳步（涉水）</summary>
        [SerializeField] private LoopEntry _footsteps = new LoopEntry(0.6f);
        /// <summary>洞窟風聲（一直墊在 BGM 下）</summary>
        [SerializeField] private LoopEntry _caveWind = new LoopEntry(0.25f);
        /// <summary>低 SAN 環境音（SAN 越低越大聲）</summary>
        [SerializeField] private LoopEntry _lowSanity = new LoopEntry(0.45f);
        /// <summary>憋氣中的空靈音</summary>
        [SerializeField] private LoopEntry _breathLoop = new LoopEntry(0.6f);
        /// <summary>Boss 房環境音</summary>
        [SerializeField] private LoopEntry _bossHall = new LoopEntry(0.4f);
        /// <summary>水聲（憋氣屏障上的位置音效）</summary>
        [SerializeField] private LoopEntry _water = new LoopEntry(0.8f);

        [Header("觸發門檻")]
        /// <summary>落地時下落速度至少多少才播落地音（避免小台階一直響）</summary>
        [SerializeField] private float _landMinFallSpeed = 4f;
        /// <summary>水平速度至少多少才算在走路（腳步聲）</summary>
        [SerializeField] private float _footstepMinSpeed = 0.5f;
        /// <summary>SAN 百分比低於此值，低 SAN 環境音開始淡入</summary>
        [Range(0f, 1f)] [SerializeField] private float _lowSanityStartRatio = 0.75f;
        /// <summary>憋氣時 BGM 壓低到原音量的幾倍</summary>
        [Range(0f, 1f)] [SerializeField] private float _musicDuckWhileHolding = 0.4f;
        /// <summary>循環音淡入淡出速度（每秒音量變化）</summary>
        [SerializeField] private float _layerFadeSpeed = 1.5f;

        /// <summary>跳躍（唯讀）</summary>
        public SfxEntry Jump => _jump;
        /// <summary>落地（唯讀）</summary>
        public SfxEntry Land => _land;
        /// <summary>發射魚叉（唯讀）</summary>
        public SfxEntry HarpoonThrow => _harpoonThrow;
        /// <summary>開始憋氣（唯讀）</summary>
        public SfxEntry BreathHold => _breathHold;
        /// <summary>使用回復道具（唯讀）</summary>
        public SfxEntry Eat => _eat;
        /// <summary>主角受擊（唯讀）</summary>
        public SfxEntry PlayerHurt => _playerHurt;
        /// <summary>重生復活（唯讀）</summary>
        public SfxEntry Respawn => _respawn;
        /// <summary>魚叉命中敵人（唯讀）</summary>
        public SfxEntry HarpoonHit => _harpoonHit;
        /// <summary>擊殺（唯讀）</summary>
        public SfxEntry EnemyKill => _enemyKill;
        /// <summary>Boss 咆哮（唯讀）</summary>
        public SfxEntry BossRoar => _bossRoar;
        /// <summary>封印完成（唯讀）</summary>
        public SfxEntry SealComplete => _sealComplete;
        /// <summary>被怪物偵測到（唯讀）</summary>
        public SfxEntry Detected => _detected;
        /// <summary>啟動封印祭壇（唯讀）</summary>
        public SfxEntry SealChain => _sealChain;
        /// <summary>Boss 追蹤彈（唯讀）</summary>
        public SfxEntry BossHoming => _bossHoming;
        /// <summary>Boss 落雷（唯讀）</summary>
        public SfxEntry BossLightning => _bossLightning;
        /// <summary>低語（唯讀）</summary>
        public SfxEntry Whisper => _whisper;
        /// <summary>驚嚇（唯讀）</summary>
        public SfxEntry JumpScare => _jumpScare;
        /// <summary>腳步（唯讀）</summary>
        public LoopEntry Footsteps => _footsteps;
        /// <summary>洞窟風聲（唯讀）</summary>
        public LoopEntry CaveWind => _caveWind;
        /// <summary>低 SAN 環境音（唯讀）</summary>
        public LoopEntry LowSanity => _lowSanity;
        /// <summary>憋氣中的空靈音（唯讀）</summary>
        public LoopEntry BreathLoop => _breathLoop;
        /// <summary>Boss 房環境音（唯讀）</summary>
        public LoopEntry BossHall => _bossHall;
        /// <summary>水聲（唯讀）</summary>
        public LoopEntry Water => _water;
        /// <summary>落地音最低下落速度（唯讀）</summary>
        public float LandMinFallSpeed => _landMinFallSpeed;
        /// <summary>腳步聲最低水平速度（唯讀）</summary>
        public float FootstepMinSpeed => _footstepMinSpeed;
        /// <summary>低 SAN 環境音起始比例（唯讀）</summary>
        public float LowSanityStartRatio => _lowSanityStartRatio;
        /// <summary>憋氣時 BGM 音量倍率（唯讀）</summary>
        public float MusicDuckWhileHolding => _musicDuckWhileHolding;
        /// <summary>循環音淡入淡出速度（唯讀）</summary>
        public float LayerFadeSpeed => _layerFadeSpeed;
    }
}
