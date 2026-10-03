using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 平台群組：放在關卡 Prefab 的 Platforms 物件上，執行時把底下所有平台（例如 float1~6 Prefab）設成 Ground Layer，
    /// 讓玩家站得上去、魚叉插得進去，並加上 PlatformEffector2D 變成單向平台（可從下方跳穿、站在上面），不需要改動平台 Prefab 本身。
    /// </summary>
    public class PlatformGroup : MonoBehaviour
    {
        /// <summary>要套用的 Layer 名稱。</summary>
        [SerializeField] private string _layerName = "Ground";
        /// <summary>是否設成單向平台（左右交錯的階梯需要，否則跳上去會撞到頭頂那層）。</summary>
        [SerializeField] private bool _oneWay = true;
        /// <summary>單向平台頂面可站立的角度範圍。</summary>
        [SerializeField] private float _surfaceArc = 160f;

        /// <summary>遞迴設定所有子物件的 Layer。</summary>
        private void Awake()
        {
            int layer = LayerMask.NameToLayer(_layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[DrownedDream] 找不到 Layer：{_layerName}", this);
                return;
            }
            foreach (var t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            if (!_oneWay) return;

            foreach (var col in GetComponentsInChildren<Collider2D>(true))
            {
                if (col.isTrigger) continue;
                col.usedByEffector = true;
                var effector = col.GetComponent<PlatformEffector2D>();
                if (effector == null) effector = col.gameObject.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = _surfaceArc;
                effector.useSideFriction = false;
            }
        }
    }
}
