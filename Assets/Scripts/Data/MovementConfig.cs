using UnityEngine;

namespace DrownedDream
{
    /// <summary>平台跳躍移動參數（F-MAP-07）。水下手感以低重力 + 慢下落呈現。</summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Movement Config", fileName = "MovementConfig")]
    public class MovementConfig : ScriptableObject
    {
        [Header("水平移動")]
        /// <summary>最大水平速度（單位/秒）</summary>
        [SerializeField] private float _maxSpeed = 5f;
        /// <summary>最大水平速度（單位/秒）（唯讀）</summary>
        public float MaxSpeed => _maxSpeed;
        /// <summary>地面加速度</summary>
        [SerializeField] private float _acceleration = 0f;
        /// <summary>地面加速度（唯讀）</summary>
        public float Acceleration => _acceleration;
        /// <summary>放開方向鍵時的減速度</summary>
        [SerializeField] private float _deceleration = 0f;
        /// <summary>放開方向鍵時的減速度（唯讀）</summary>
        public float Deceleration => _deceleration;
        /// <summary>空中加速度倍率</summary>
        [Tooltip("空中加速度倍率")] [SerializeField] private float _airControl = 0f;
        /// <summary>空中加速度倍率（唯讀）</summary>
        public float AirControl => _airControl;

        [Header("跳躍 / 重力（水下手感）")]
        /// <summary>起跳初速度</summary>
        [SerializeField] private float _jumpVelocity = 0f;
        /// <summary>起跳初速度（唯讀）</summary>
        public float JumpVelocity => _jumpVelocity;
        /// <summary>重力倍率（越低越有水中漂浮感）</summary>
        [SerializeField] private float _gravityScale = 0f;
        /// <summary>重力倍率（越低越有水中漂浮感）（唯讀）</summary>
        public float GravityScale => _gravityScale;
        /// <summary>下降時的重力倍率（大於 1 = 過了最高點後加速落下）</summary>
        [Tooltip("下降時的重力倍率")] [SerializeField] private float _fallGravityMultiplier = 0f;
        /// <summary>下降時的重力倍率（唯讀）</summary>
        public float FallGravityMultiplier => _fallGravityMultiplier;
        /// <summary>最大下落速度（模擬水阻）</summary>
        [Tooltip("最大下落速度（水阻）")] [SerializeField] private float _maxFallSpeed = 0f;
        /// <summary>最大下落速度（模擬水阻）（唯讀）</summary>
        public float MaxFallSpeed => _maxFallSpeed;
        /// <summary>離開地面後仍可起跳的寬限秒數</summary>
        [SerializeField] private float _coyoteTime = 0.1f;
        /// <summary>離開地面後仍可起跳的寬限秒數（唯讀）</summary>
        public float CoyoteTime => _coyoteTime;
        /// <summary>落地前預先按跳躍的緩衝秒數</summary>
        [SerializeField] private float _jumpBufferTime = 0.12f;
        /// <summary>落地前預先按跳躍的緩衝秒數（唯讀）</summary>
        public float JumpBufferTime => _jumpBufferTime;

        [Header("地面偵測")]
        /// <summary>腳底往下偵測地面的距離</summary>
        [SerializeField] private float _groundCheckDistance = 0.08f;
        /// <summary>腳底往下偵測地面的距離（唯讀）</summary>
        public float GroundCheckDistance => _groundCheckDistance;
    }
}
