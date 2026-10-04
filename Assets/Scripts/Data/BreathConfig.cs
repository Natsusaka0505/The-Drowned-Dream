using UnityEngine;

namespace DrownedDream
{
    /// <summary>憋氣參數（F-BRE）。</summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Breath Config", fileName = "BreathConfig")]
    public class BreathConfig : ScriptableObject
    {
        /// <summary>憋氣最大秒數（肺活量）</summary>
        [SerializeField] private float _maxHoldTime = 5f;
        /// <summary>憋氣最大秒數（肺活量）（唯讀）</summary>
        public float MaxHoldTime => _maxHoldTime;
        /// <summary>[待確認] SAN 0 時最大憋氣時間剩基礎值的比例（依 SAN 百分比線性，F-BRE-02）</summary>
        [Range(0f, 1f)] [SerializeField] private float _minHoldRatio = 0.4f;
        /// <summary>SAN 0 時最大憋氣時間比例（唯讀）</summary>
        public float MinHoldRatio => _minHoldRatio;
        /// <summary>完整憋氣後的 CD 秒數</summary>
        [SerializeField] private float _cooldown = 4f; // 2026-10-04 由 8 縮短為 4
        /// <summary>完整憋氣後的 CD 秒數（唯讀）</summary>
        public float Cooldown => _cooldown;
        /// <summary>提早結束時 CD 依比例縮短，此為最小比例</summary>
        [Range(0f, 1f)] [SerializeField] private float _minCooldownRatio = 0.3f;
        /// <summary>提早結束時 CD 依比例縮短，此為最小比例（唯讀）</summary>
        public float MinCooldownRatio => _minCooldownRatio;
        /// <summary>憋氣時角色透明度（視覺回饋）</summary>
        [Range(0f, 1f)] [SerializeField] private float _holdAlpha = 0.35f;
        /// <summary>憋氣時角色透明度（視覺回饋）（唯讀）</summary>
        public float HoldAlpha => _holdAlpha;
    }
}
