using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DrownedDream
{
    /// <summary>
    /// 封印祭壇（F-BOSS-03）。場上可有多座（原型為 Boss 左右各一），每座按互動鍵啟動一次，
    /// 全部啟動才封印 Boss；觸發結局的判斷在 PlayerEnding。
    /// </summary>
    public class SealAltar : MonoBehaviour
    {
        /// <summary>場景中所有祭壇。</summary>
        public static readonly List<SealAltar> All = new List<SealAltar>();

        /// <summary>要封印的 Boss。</summary>
        [SerializeField] private BossController _boss;
        /// <summary>互動距離。</summary>
        [SerializeField] private float _interactRadius = 1.8f;
        /// <summary>祭壇光源（啟動後變亮變色；沒有可留空）。</summary>
        [SerializeField] private Light2D _light;
        /// <summary>啟動後的光色。</summary>
        [SerializeField] private Color _activatedLightColor = new Color(0.75f, 1f, 0.85f);
        /// <summary>啟動後的光強度倍率。</summary>
        [SerializeField] private float _activatedLightBoost = 1.8f;

        /// <summary>此祭壇是否已啟動。</summary>
        public bool IsActivated { get; private set; }

        /// <summary>場上祭壇總數。</summary>
        public static int TotalCount => All.Count;

        /// <summary>已啟動的祭壇數。</summary>
        public static int ActivatedCount
        {
            get
            {
                int n = 0;
                foreach (var a in All) if (a != null && a.IsActivated) n++;
                return n;
            }
        }

        /// <summary>是否所有祭壇都已啟動（= Boss 已封印）。</summary>
        public static bool AllActivated => All.Count > 0 && ActivatedCount >= All.Count;

        /// <summary>登記祭壇。</summary>
        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        /// <summary>移除登記。</summary>
        private void OnDisable() => All.Remove(this);

        /// <summary>位置是否在互動範圍內。</summary>
        public bool IsInRange(Vector2 position) => Vector2.Distance(position, transform.position) <= _interactRadius;

        /// <summary>找出玩家位置所在範圍內最近的祭壇（沒有回傳 null）。</summary>
        public static SealAltar FindInRange(Vector2 position)
        {
            SealAltar best = null;
            float bestDist = float.MaxValue;
            foreach (var a in All)
            {
                if (a == null || !a.IsInRange(position)) continue;
                float d = Vector2.Distance(position, a.transform.position);
                if (d < bestDist)
                {
                    best = a;
                    bestDist = d;
                }
            }
            return best;
        }

        /// <summary>啟動此祭壇；全部啟動時封印 Boss，回傳是否因此完成封印。</summary>
        public bool Activate()
        {
            if (IsActivated) return false;
            IsActivated = true;
            if (_light != null)
            {
                _light.color = _activatedLightColor;
                _light.intensity *= _activatedLightBoost;
            }
            if (!AllActivated) return false;
            if (_boss != null) _boss.Seal();
            return true;
        }

        /// <summary>選取時畫出互動範圍。</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _interactRadius);
        }
    }
}
