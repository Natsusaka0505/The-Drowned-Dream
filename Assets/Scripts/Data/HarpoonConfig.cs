using UnityEngine;

namespace DrownedDream
{
    /// <summary>魚槍參數（F-WPN）。魚叉以拋物線飛行。</summary>
    [CreateAssetMenu(menuName = "Drowned Dream/Harpoon Config", fileName = "HarpoonConfig")]
    public class HarpoonConfig : ScriptableObject
    {
        /// <summary>魚叉上限</summary>
        [SerializeField] private int _maxAmmo = 3;
        /// <summary>魚叉上限（唯讀）</summary>
        public int MaxAmmo => _maxAmmo;
        /// <summary>飛行初速（單位/秒）</summary>
        [SerializeField] private float _flightSpeed = 15f;
        /// <summary>飛行初速（單位/秒）（唯讀）</summary>
        public float FlightSpeed => _flightSpeed;
        /// <summary>拋物線下墜的重力加速度</summary>
        [SerializeField] private float _gravity = 6f;
        /// <summary>拋物線下墜的重力加速度（唯讀）</summary>
        public float Gravity => _gravity;
        /// <summary>最大下墜速度</summary>
        [SerializeField] private float _maxFallSpeed = 10f;
        /// <summary>最大下墜速度（唯讀）</summary>
        public float MaxFallSpeed => _maxFallSpeed;
        /// <summary>發射間隔秒數</summary>
        [SerializeField] private float _fireInterval = 0.4f;
        /// <summary>發射間隔秒數（唯讀）</summary>
        public float FireInterval => _fireInterval;
        /// <summary>[待確認] 低於發射點超過此距離視為掉入深淵，自動歸還</summary>
        [SerializeField] private float _abyssFallDistance = 20f;
        /// <summary>[待確認] 低於發射點超過此距離視為掉入深淵，自動歸還（唯讀）</summary>
        public float AbyssFallDistance => _abyssFallDistance;
    }
}
