using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 憋氣 bonus 區域基底（F-BRE-05 / F-MAP-04）。
    /// 新增 bonus 類型 = 繼承此類別並實作 OnHoldStarted / OnHoldEnded / WhileHolding。
    /// </summary>
    public abstract class BreathBonusZone : MonoBehaviour
    {
        /// <summary>玩家快取。</summary>
        private Player _player;
        /// <summary>上一幀是否憋氣中。</summary>
        private bool _wasHolding;

        /// <summary>玩家（子類別使用）。</summary>
        protected Player Player => _player;

        /// <summary>偵測憋氣狀態變化並呼叫對應回呼。</summary>
        protected virtual void Update()
        {
            if (_player == null) _player = Player.Instance;
            if (_player == null) return;

            bool holding = _player.Status.IsHoldingBreath;
            if (holding && !_wasHolding) OnHoldStarted();
            else if (!holding && _wasHolding) OnHoldEnded();
            if (holding) WhileHolding();
            _wasHolding = holding;
        }

        /// <summary>開始憋氣時呼叫。</summary>
        protected virtual void OnHoldStarted() { }
        /// <summary>結束憋氣時呼叫。</summary>
        protected virtual void OnHoldEnded() { }
        /// <summary>憋氣期間每幀呼叫。</summary>
        protected virtual void WhileHolding() { }
    }
}
