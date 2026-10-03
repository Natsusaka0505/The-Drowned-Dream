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

        [Header("精神錯亂（F-SAN-11/12）")]
        /// <summary>SAN 百分比低於此值才會精神錯亂</summary>
        [Range(0f, 1f)] [SerializeField] private float _confusionStartRatio = 0.75f;
        /// <summary>SAN 百分比低於此值才會精神錯亂（唯讀）</summary>
        public float ConfusionStartRatio => _confusionStartRatio;
        /// <summary>SAN 剛低於起始比例時的錯亂機率（之後線性增加到 SAN 0 時的機率）</summary>
        [Range(0f, 1f)] [SerializeField] private float _confusionMinChance = 0.15f;
        /// <summary>SAN 剛低於起始比例時的錯亂機率（唯讀）</summary>
        public float ConfusionMinChance => _confusionMinChance;
        /// <summary>SAN 0 時的錯亂機率</summary>
        [Range(0f, 1f)] [SerializeField] private float _confusionMaxChance = 0.45f;
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
        /// <summary>精神錯亂持續秒數</summary>
        [SerializeField] private float _confusionDuration = 2f;
        /// <summary>精神錯亂持續秒數（唯讀）</summary>
        public float ConfusionDuration => _confusionDuration;

        [Header("錯亂中的按鍵（F-SAN-11）")]
        /// <summary>[待確認] 錯亂中每次按 A/W/D 被換成另外兩個方向的機率</summary>
        [Range(0f, 1f)] [SerializeField] private float _swapChance = 0.5f;
        /// <summary>錯亂中每次按 A/W/D 被換成另外兩個方向的機率（唯讀）</summary>
        public float SwapChance => _swapChance;
        /// <summary>[待確認] 錯亂中按 Space 沒射出的機率</summary>
        [Range(0f, 1f)] [SerializeField] private float _fireMisfireChance = 0.25f;
        /// <summary>錯亂中按 Space 沒射出的機率（唯讀）</summary>
        public float FireMisfireChance => _fireMisfireChance;
        /// <summary>[待確認] 錯亂中按 Space 延遲射出的機率</summary>
        [Range(0f, 1f)] [SerializeField] private float _fireDelayChance = 0.35f;
        /// <summary>錯亂中按 Space 延遲射出的機率（唯讀）</summary>
        public float FireDelayChance => _fireDelayChance;
        /// <summary>[待確認] 延遲射出的秒數範圍（最小, 最大）</summary>
        [SerializeField] private Vector2 _fireDelayRange = new Vector2(0.3f, 0.8f);
        /// <summary>延遲射出的秒數範圍（唯讀）</summary>
        public Vector2 FireDelayRange => _fireDelayRange;
    }
}
