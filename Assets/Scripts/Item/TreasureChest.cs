using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 寶箱（F-INV-15）：玩家靠近按互動鍵打開 → 播放「晃動 → 開箱 → 道具升起 → 飛向玩家」動畫 → 自動取得道具。
    /// 灰 = 繃帶、藍灰 = 藥丸、黑 = 邪神雕像（由各顏色 Prefab Variant 設定）。
    /// </summary>
    public class TreasureChest : MonoBehaviour
    {
        /// <summary>場景中所有尚未打開的寶箱（給 PlayerOpenChest 找最近的寶箱）。</summary>
        private static readonly List<TreasureChest> s_closed = new List<TreasureChest>();

        #region Status

        [Header("外觀")]
        /// <summary>寶箱圖（會晃動 / 換圖的子物件）。</summary>
        [SerializeField] private SpriteRenderer _renderer;
        /// <summary>關閉的寶箱圖。</summary>
        [SerializeField] private Sprite _closedSprite;
        /// <summary>打開的寶箱圖。</summary>
        [SerializeField] private Sprite _openSprite;

        [Header("內容物")]
        /// <summary>打開後出現的道具 Prefab（需有 PickupItem）。</summary>
        [SerializeField] private PickupItem _itemPrefab;
        /// <summary>互動距離（以寶箱底部中心計算）。</summary>
        [SerializeField] private float _interactRadius = 1.5f;

        [Header("動畫")]
        /// <summary>開箱前晃動秒數。</summary>
        [SerializeField] private float _shakeTime = 0.35f;
        /// <summary>晃動幅度（單位）。</summary>
        [SerializeField] private float _shakeAmplitude = 0.06f;
        /// <summary>換成打開圖時的彈跳放大倍率。</summary>
        [SerializeField] private float _popScale = 1.15f;
        /// <summary>彈跳回原大小的秒數。</summary>
        [SerializeField] private float _popTime = 0.15f;
        /// <summary>道具出現位置（相對寶箱底部中心）。</summary>
        [SerializeField] private Vector2 _itemSpawnOffset = new Vector2(0f, 0.6f);
        /// <summary>道具升起的高度（相對出現位置）。</summary>
        [SerializeField] private float _itemRiseHeight = 1.2f;
        /// <summary>道具升起秒數。</summary>
        [SerializeField] private float _itemRiseTime = 0.5f;
        /// <summary>道具在空中停留展示的秒數。</summary>
        [SerializeField] private float _itemHoldTime = 0.4f;
        /// <summary>道具飛向玩家的秒數。</summary>
        [SerializeField] private float _itemFlyTime = 0.3f;

        /// <summary>是否已打開。</summary>
        public bool IsOpened { get; private set; }

        #endregion

        /// <summary>登記為未打開的寶箱。</summary>
        private void OnEnable()
        {
            if (!IsOpened && !s_closed.Contains(this)) s_closed.Add(this);
        }

        /// <summary>從清單移除。</summary>
        private void OnDisable() => s_closed.Remove(this);

        /// <summary>設定為關閉圖。</summary>
        private void Awake()
        {
            if (_renderer != null && _closedSprite != null) _renderer.sprite = _closedSprite;
        }

        /// <summary>找出範圍內最近、尚未打開的寶箱（沒有則回傳 null）。</summary>
        public static TreasureChest FindInRange(Vector2 position)
        {
            TreasureChest best = null;
            float bestDist = float.MaxValue;
            foreach (var chest in s_closed)
            {
                float dist = Vector2.Distance(position, chest.transform.position);
                if (dist > chest._interactRadius || dist >= bestDist) continue;
                best = chest;
                bestDist = dist;
            }
            return best;
        }

        #region Action

        /// <summary>打開寶箱並播放動畫，播完把道具交給玩家。</summary>
        public void Open(Player player)
        {
            if (IsOpened) return;
            IsOpened = true;
            s_closed.Remove(this);
            StartCoroutine(OpenRoutine(player));
        }

        #endregion

        /// <summary>開箱動畫：晃動 → 換打開圖並彈跳 → 道具升起 → 停留 → 飛向玩家並取得。</summary>
        private IEnumerator OpenRoutine(Player player)
        {
            var visual = _renderer.transform;
            Vector3 basePos = visual.localPosition;
            Vector3 baseScale = visual.localScale;

            // 1. 晃動
            for (float t = 0f; t < _shakeTime; t += Time.deltaTime)
            {
                float x = Mathf.Sin(t * 60f) * _shakeAmplitude * (1f - t / _shakeTime);
                visual.localPosition = basePos + new Vector3(x, 0f, 0f);
                yield return null;
            }
            visual.localPosition = basePos;

            // 2. 換成打開圖並彈跳
            if (_openSprite != null) _renderer.sprite = _openSprite;
            for (float t = 0f; t < _popTime; t += Time.deltaTime)
            {
                visual.localScale = baseScale * Mathf.Lerp(_popScale, 1f, t / _popTime);
                yield return null;
            }
            visual.localScale = baseScale;

            if (_itemPrefab == null) yield break;

            // 3. 道具從箱口升起並放大（動畫期間停用拾取與浮動）
            Vector3 start = transform.position + (Vector3)_itemSpawnOffset;
            Vector3 top = start + Vector3.up * _itemRiseHeight;
            var item = Instantiate(_itemPrefab, start, Quaternion.identity);
            var itemCol = item.GetComponent<Collider2D>();
            if (itemCol != null) itemCol.enabled = false;
            item.enabled = false;
            Vector3 itemScale = item.transform.localScale;
            var itemRenderer = item.GetComponentInChildren<SpriteRenderer>();
            if (itemRenderer != null) itemRenderer.sortingOrder = _renderer.sortingOrder + 1;

            for (float t = 0f; t < _itemRiseTime; t += Time.deltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / _itemRiseTime, 3f); // ease-out
                item.transform.position = Vector3.LerpUnclamped(start, top, k);
                item.transform.localScale = itemScale * k;
                yield return null;
            }
            item.transform.position = top;
            item.transform.localScale = itemScale;

            // 4. 停留展示
            yield return new WaitForSeconds(_itemHoldTime);

            // 5. 飛向玩家並縮小，抵達後取得
            var pickup = player != null ? player.GetComponent<PlayerPickup>() : null;
            if (pickup != null)
            {
                for (float t = 0f; t < _itemFlyTime; t += Time.deltaTime)
                {
                    float k = t / _itemFlyTime;
                    item.transform.position = Vector3.Lerp(top, player.transform.position, k * k);
                    item.transform.localScale = itemScale * Mathf.Lerp(1f, 0.5f, k);
                    yield return null;
                }
                if (pickup.TryPickUp(item)) yield break;
            }

            // 玩家無法取得（例如已死亡）：道具留在箱子上方，恢復成一般可撿道具
            item.transform.position = top;
            item.transform.localScale = itemScale;
            if (itemCol != null) itemCol.enabled = true;
            item.enabled = true;
        }

        /// <summary>選取時畫出互動範圍。</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactRadius);
        }
    }
}
