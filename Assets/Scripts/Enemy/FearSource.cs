using System.Collections.Generic;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>恐懼範圍（F-ENM-04 / F-SAN-02）。玩家在半徑內時持續掉 SAN。</summary>
    public class FearSource : MonoBehaviour
    {
        /// <summary>場上所有啟用中的恐懼範圍。</summary>
        private static readonly List<FearSource> s_active = new List<FearSource>();

        /// <summary>恐懼半徑。</summary>
        [SerializeField] private float _radius = 5f;
        /// <summary>範圍內每秒扣 SAN。</summary>
        [SerializeField] private float _drainPerSecond = 5f;

        /// <summary>恐懼半徑（敵人依資料設定）。</summary>
        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }

        /// <summary>每秒扣 SAN（敵人依資料設定）。</summary>
        public float DrainPerSecond
        {
            get => _drainPerSecond;
            set => _drainPerSecond = value;
        }

        /// <summary>加入清單。</summary>
        private void OnEnable() => s_active.Add(this);

        /// <summary>移出清單。</summary>
        private void OnDisable() => s_active.Remove(this);

        /// <summary>所有涵蓋該位置的恐懼範圍扣 SAN 速率總和。</summary>
        public static float TotalDrainAt(Vector2 position)
        {
            float total = 0f;
            foreach (var source in s_active)
            {
                float r = source._radius;
                if (((Vector2)source.transform.position - position).sqrMagnitude <= r * r)
                {
                    total += source._drainPerSecond;
                }
            }
            return total;
        }

        /// <summary>選取時畫出恐懼範圍。</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.2f, 0.8f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
