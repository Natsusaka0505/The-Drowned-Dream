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

        [Header("精靈台詞（開寶箱取得道具時隨機說一句）")]
        /// <summary>開出繃帶（回 HP）時的台詞。</summary>
        [SerializeField] private string[] _hpLines =
        {
            "這捲繃帶還很新……是誰留在這裡的呢？",
            "受傷了就纏一纏吧，我幫你盯著四周！",
            "海水泡到傷口會很痛喔，忍耐一下～",
            "繃帶上有奇怪的符號……先別想太多。",
        };
        /// <summary>開出藥丸（回 SAN）時的台詞。</summary>
        [SerializeField] private string[] _sanityLines =
        {
            "吃下去，腦袋裡的低語會安靜一點。",
            "藥丸？……希望不是過期的。",
            "你的眼神有點飄喔，快吃一顆吧！",
            "嗯？罐子上的字一直在動……沒事沒事！",
        };
        /// <summary>開出邪神雕像（封印道具）時的台詞。</summary>
        [SerializeField] private string[] _sealLines =
        {
            "就是它！封印需要的雕像！",
            "這尊雕像……好像在看著我們。",
            "雕像冰冰的，卻又在微微發熱……",
            "再多找幾尊，就能把那傢伙封起來了！",
        };
        /// <summary>其他道具的台詞。</summary>
        [SerializeField] private string[] _otherLines =
        {
            "找到東西了！",
            "這個箱子比看起來還重呢。",
        };

        /// <summary>是否已打開。</summary>
        public bool IsOpened { get; private set; }
        /// <summary>是否為最後一個封印道具（第一次進 Boss 房後才出現，小地圖 / 方向箭頭會特別標示）。</summary>
        public bool IsFinalSeal { get; set; }
        /// <summary>內容物 Prefab（小地圖依類型上色）。</summary>
        public PickupItem ItemPrefab => _itemPrefab;

        /// <summary>場景中所有寶箱（含已打開，小地圖用）。</summary>
        public static readonly List<TreasureChest> All = new List<TreasureChest>();

        #endregion

        /// <summary>登記為未打開的寶箱。</summary>
        private void OnEnable()
        {
            if (!IsOpened && !s_closed.Contains(this)) s_closed.Add(this);
            if (!All.Contains(this)) All.Add(this);
        }

        /// <summary>從清單移除。</summary>
        private void OnDisable()
        {
            s_closed.Remove(this);
            All.Remove(this);
        }

        /// <summary>依內容物挑一句精靈台詞。</summary>
        private string PickLine(PickupItem item)
        {
            string[] lines = _otherLines;
            if (item is SealItem) lines = _sealLines;
            else if (item is RecoveryItem r) lines = r.HpRestore > 0d ? _hpLines : r.SanityRestore > 0d ? _sanityLines : _otherLines;
            return lines != null && lines.Length > 0 ? lines[Random.Range(0, lines.Length)] : null;
        }

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
                string line = PickLine(item);
                string itemName = item.DisplayName;
                if (pickup.TryPickUp(item))
                {
                    // 蓋過道具本身的拾取提示：精靈隨機說一句 + 取得什麼（進 Boss 房後 HUD 會改成一般文字）
                    if (!string.IsNullOrEmpty(line)) GameEvents.ShowMessage($"{line}\n——取得 {itemName}", 3f);
                    yield break;
                }
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
