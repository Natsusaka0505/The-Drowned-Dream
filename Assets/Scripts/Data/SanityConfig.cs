using UnityEngine;

namespace DrownedDream
{
    /// <summary>SAN 參數（F-SAN）。SAN 不致死，只影響難度。</summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Sanity Config", fileName = "SanityConfig")]
    public class SanityConfig : ScriptableObject
    {
        /// <summary>基礎最大 SAN</summary>
        [SerializeField] private float _maxSanity = 100f;
        /// <summary>基礎最大 SAN（唯讀）</summary>
        public float MaxSanity => _maxSanity;
        /// <summary>離開所有恐懼範圍後每秒恢復量</summary>
        [SerializeField] private float _recoverPerSecond = 3f;
        /// <summary>離開所有恐懼範圍後每秒恢復量（唯讀）</summary>
        public float RecoverPerSecond => _recoverPerSecond;
        /// <summary>憋氣隱形時是否仍會掉 SAN（已確認：會）</summary>
        [SerializeField] private bool _drainWhileInvisible = true;
        /// <summary>憋氣隱形時是否仍會掉 SAN（已確認：會）（唯讀）</summary>
        public bool DrainWhileInvisible => _drainWhileInvisible;

        [Header("地圖變化分段（以基礎最大 SAN 的百分比，由高到低）")]
        /// <summary>低 SAN 分段門檻（百分比，由高到低）</summary>
        [SerializeField] private float[] _stageThresholds = { 0.7f, 0.4f, 0.2f };
        /// <summary>低 SAN 分段門檻（百分比，由高到低）（唯讀）</summary>
        public float[] StageThresholds => _stageThresholds;

        [Header("方向錯亂（F-SAN-11/12）")]
        /// <summary>SAN 百分比高於此值不會方向錯亂</summary>
        [Range(0f, 1f)] [SerializeField] private float _confusionStartRatio = 0.5f;
        /// <summary>SAN 百分比高於此值不會方向錯亂（唯讀）</summary>
        public float ConfusionStartRatio => _confusionStartRatio;
        /// <summary>SAN 0 時的錯亂機率</summary>
        [Range(0f, 1f)] [SerializeField] private float _confusionMaxChance = 0.3f;
        /// <summary>SAN 0 時的錯亂機率（唯讀）</summary>
        public float ConfusionMaxChance => _confusionMaxChance;
        /// <summary>[待確認] 每隔幾秒擲一次錯亂判定</summary>
        [SerializeField] private float _confusionCheckInterval = 4f;
        /// <summary>[待確認] 每隔幾秒擲一次錯亂判定（唯讀）</summary>
        public float ConfusionCheckInterval => _confusionCheckInterval;
        /// <summary>錯亂前的預告秒數</summary>
        [SerializeField] private float _confusionWarningTime = 0.75f;
        /// <summary>錯亂前的預告秒數（唯讀）</summary>
        public float ConfusionWarningTime => _confusionWarningTime;
        /// <summary>左右反轉持續秒數</summary>
        [SerializeField] private float _confusionDuration = 2f;
        /// <summary>左右反轉持續秒數（唯讀）</summary>
        public float ConfusionDuration => _confusionDuration;
    }
}
