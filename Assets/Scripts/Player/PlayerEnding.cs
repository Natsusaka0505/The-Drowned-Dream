using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// Action：是否觸發結局（F-BOSS-03）。封印道具齊全且在祭壇旁按互動鍵 → 封印 Boss → 進入結局。
    /// [待確認] 封印形式，原型為一次完成。
    /// </summary>
    public class PlayerEnding : MonoBehaviour
    {
        /// <summary>封印後到結局的秒數。</summary>
        [SerializeField] private float _endingDelay = 1.5f;

        /// <summary>玩家數值（封印道具數）。</summary>
        private PlayerStatus _status;
        /// <summary>輸入來源。</summary>
        private PlayerInputReader _input;
        /// <summary>本次靠近祭壇是否已提示。</summary>
        private bool _promptShown;
        /// <summary>結局倒數（小於 0 表示未觸發）。</summary>
        private float _endingTimer = -1f;

        /// <summary>是否已觸發結局。</summary>
        public bool IsTriggered { get; private set; }

        /// <summary>快取元件。</summary>
        private void Awake()
        {
            _status = GetComponent<PlayerStatus>();
            _input = GetComponent<PlayerInputReader>();
        }

        /// <summary>靠近祭壇提示、按鍵封印、倒數進結局。</summary>
        private void Update()
        {
            if (IsTriggered)
            {
                if (_endingTimer < 0f) return;
                _endingTimer -= Time.deltaTime;
                if (_endingTimer < 0f) GameEvents.RaiseBossSealed();
                return;
            }

            var altar = SealAltar.Instance;
            if (altar == null || !altar.IsInRange(transform.position))
            {
                _promptShown = false;
                return;
            }

            if (!_promptShown)
            {
                _promptShown = true;
                GameEvents.ShowMessage(_status.HasAllSeals
                    ? "按 E 進行封印"
                    : $"祭壇需要 {_status.RequiredSeals} 個封印道具（目前 {_status.SealCount}）", 2f);
            }

            if (_input.InteractPressed && CanTriggerEnding()) TriggerEnding(altar);
        }

        /// <summary>是否滿足觸發結局的條件（封印道具齊全）。</summary>
        public bool CanTriggerEnding() => _status.HasAllSeals && !IsTriggered;

        /// <summary>封印 Boss 並開始結局倒數。</summary>
        private void TriggerEnding(SealAltar altar)
        {
            IsTriggered = true;
            altar.Seal();
            GameEvents.ShowMessage("封印完成……", _endingDelay);
            _endingTimer = _endingDelay;
        }
    }
}
