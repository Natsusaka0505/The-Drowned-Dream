using UnityEngine;

namespace DrownedDream
{
    /// <summary>封印祭壇（F-BOSS-03）。只負責範圍與封印 Boss；觸發結局的判斷在 PlayerEnding。</summary>
    public class SealAltar : MonoBehaviour
    {
        /// <summary>場景中的祭壇。</summary>
        public static SealAltar Instance { get; private set; }

        /// <summary>要封印的 Boss。</summary>
        [SerializeField] private BossController _boss;
        /// <summary>互動距離。</summary>
        [SerializeField] private float _interactRadius = 1.8f;

        /// <summary>是否已封印。</summary>
        public bool IsSealed { get; private set; }

        /// <summary>註冊單例。</summary>
        private void Awake() => Instance = this;

        /// <summary>清除單例。</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>位置是否在互動範圍內。</summary>
        public bool IsInRange(Vector2 position) => Vector2.Distance(position, transform.position) <= _interactRadius;

        /// <summary>封印 Boss。</summary>
        public void Seal()
        {
            if (IsSealed) return;
            IsSealed = true;
            if (_boss != null) _boss.Seal();
        }

        /// <summary>選取時畫出互動範圍。</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _interactRadius);
        }
    }
}
