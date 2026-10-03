using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 低 SAN 地圖變化（F-SAN-10）：SAN 分段達到指定值時顯示 / 隱藏目標物件。
    /// 美術可用於幻覺物件、替換牆面等。
    /// </summary>
    public class SanityStageObject : MonoBehaviour
    {
        /// <summary>SAN 分段 ≥ 此值時啟用（1 = 70% 以下，2 = 40% 以下，3 = 20% 以下）。</summary>
        [SerializeField] private int _minStage = 1;
        /// <summary>要切換的物件。</summary>
        [SerializeField] private GameObject _target;
        /// <summary>反向：達到分段時隱藏。</summary>
        [SerializeField] private bool _hideInstead;

        /// <summary>玩家數值（SAN 分段來源）。</summary>
        private PlayerStatus _status;

        /// <summary>訂閱分段事件並套用目前分段。</summary>
        private void Start()
        {
            if (Player.Instance == null) return;
            _status = Player.Instance.Status;
            _status.SanityStageChanged += Apply;
            Apply(_status.SanityStage);
        }

        /// <summary>取消訂閱。</summary>
        private void OnDestroy()
        {
            if (_status != null) _status.SanityStageChanged -= Apply;
        }

        /// <summary>依分段顯示 / 隱藏目標。</summary>
        private void Apply(int stage)
        {
            if (_target == null) return;
            bool reached = stage >= _minStage;
            _target.SetActive(_hideInstead ? !reached : reached);
        }
    }
}
