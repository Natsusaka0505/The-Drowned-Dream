using System.Collections;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 最後一個封印道具（F-BOSS-10）：第一次進 Boss 房、Boss 啟動時，
    /// 從子物件的候選點隨機選一個，放出裝著邪神雕像的黑寶箱（提示等 Boss 房訊息顯示完再出現）。
    /// </summary>
    public class HiddenSealSpawner : MonoBehaviour
    {
        /// <summary>要放出的寶箱 Prefab（黑寶箱）。</summary>
        [SerializeField] private GameObject _chestPrefab;
        /// <summary>出現時的提示文字。</summary>
        [SerializeField] private string _message = "遠處傳來低語……最後一尊雕像出現在洞窟某處。";
        /// <summary>提示延遲秒數（避免和 Boss 房提示重疊）。</summary>
        [SerializeField] private float _messageDelay = 3f;

        /// <summary>是否已放出（只放一次）。</summary>
        private bool _spawned;

        /// <summary>訂閱 Boss 啟動事件。</summary>
        private void OnEnable() => GameEvents.BossActivated += Spawn;

        /// <summary>取消訂閱。</summary>
        private void OnDisable() => GameEvents.BossActivated -= Spawn;

        /// <summary>隨機選一個候選點（子物件）放出寶箱。</summary>
        private void Spawn()
        {
            if (_spawned || _chestPrefab == null || transform.childCount == 0) return;
            _spawned = true;
            var point = transform.GetChild(Random.Range(0, transform.childCount));
            Instantiate(_chestPrefab, point.position, Quaternion.identity);
            Debug.Log($"[Seal] 最後一個封印道具出現在 {point.name}（{point.position}）");
            StartCoroutine(ShowMessageLater());
        }

        /// <summary>延遲顯示提示。</summary>
        private IEnumerator ShowMessageLater()
        {
            yield return new WaitForSeconds(_messageDelay);
            GameEvents.ShowMessage(_message, 3f);
        }

        /// <summary>在場景中畫出所有候選點。</summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0.2f, 0.8f);
            foreach (Transform child in transform) Gizmos.DrawWireCube(child.position + Vector3.up * 0.6f, new Vector3(1.3f, 1.2f, 0f));
        }
    }
}
