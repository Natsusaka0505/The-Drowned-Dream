using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// Action：是否觸發結局（F-BOSS-03）。封印道具齊全且在祭壇旁按互動鍵 → 啟動該祭壇；
    /// 所有祭壇（原型 Boss 左右各一）都啟動 → 封印 Boss → 進入結局。每一步都有提示。
    /// </summary>
    public class PlayerEnding : MonoBehaviour
    {
        /// <summary>封印後到結局的秒數。</summary>
        [SerializeField] private float _endingDelay = 1.5f;

        /// <summary>玩家數值（封印道具數）。</summary>
        private PlayerStatus _status;
        /// <summary>輸入來源。</summary>
        private PlayerInputReader _input;
        /// <summary>上次提示時靠近的祭壇（換祭壇或狀態改變才再提示）。</summary>
        private SealAltar _promptAltar;
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

            var altar = SealAltar.FindInRange(transform.position);
            if (altar == null)
            {
                _promptAltar = null;
                return;
            }

            if (_promptAltar != altar)
            {
                _promptAltar = altar;
                GameEvents.ShowMessage(PromptFor(altar), 2f);
            }

            if (_input.InteractPressed && CanActivate(altar)) ActivateAltar(altar);
        }

        /// <summary>靠近祭壇時的提示文字（道具不足 / 可啟動 / 已啟動）。</summary>
        private string PromptFor(SealAltar altar)
        {
            if (!_status.HasAllSeals) return $"祭壇需要 {_status.RequiredSeals} 個封印道具（目前 {_status.SealCount}）";
            string progress = $"（{SealAltar.ActivatedCount} / {SealAltar.TotalCount}）";
            return altar.IsActivated
                ? $"這座祭壇已啟動，前往另一座祭壇{progress}"
                : $"按 E 啟動祭壇{progress}";
        }

        /// <summary>是否可以啟動這座祭壇（封印道具齊全、祭壇未啟動、尚未進結局）。</summary>
        public bool CanActivate(SealAltar altar) => _status.HasAllSeals && !IsTriggered && !altar.IsActivated;

        /// <summary>啟動祭壇：還有未啟動的祭壇就提示前往，全部啟動則封印 Boss 並開始結局倒數。</summary>
        private void ActivateAltar(SealAltar altar)
        {
            bool sealedNow = altar.Activate();
            if (!sealedNow)
            {
                int left = SealAltar.TotalCount - SealAltar.ActivatedCount;
                GameEvents.ShowMessage($"祭壇已啟動！（{SealAltar.ActivatedCount} / {SealAltar.TotalCount}）前往邪神另一側的祭壇，還差 {left} 座", 3f);
                return;
            }
            IsTriggered = true;
            GameEvents.ShowMessage("祭壇共鳴……封印完成", _endingDelay);
            _endingTimer = _endingDelay;
        }
    }
}
