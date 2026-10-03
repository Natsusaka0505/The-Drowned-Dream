using UnityEngine;

namespace DrownedDream
{
    /// <summary>關卡 Prefab 中的玩家起點標記（Build Prototype Scene 讀它的位置放玩家，執行期不做事）。</summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        /// <summary>在場景中畫出起點位置。</summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.8f, 1.6f, 0f));
        }
    }
}
