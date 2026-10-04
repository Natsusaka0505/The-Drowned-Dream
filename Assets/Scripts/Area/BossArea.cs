using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DrownedDream
{
    /// <summary>
    /// area（docs/core）：Boss 房。放在 Boss 房內，執行時自動找出所在的 Room。
    /// 第一次進房 → 精靈變身成 Boss 的演出（嘲諷台詞 → 飛到 Boss 位置 → 發光放大 → 閃白 → Boss 現身）再切回玩家；
    /// 進房 → Boss 啟動（不論道具是否集齊）；離房 → Boss 停止（F-BOSS-02/04/05/06、F-BOSS-23）。
    /// </summary>
    public class BossArea : MonoBehaviour
    {
        /// <summary>房內的 Boss。</summary>
        [SerializeField] private BossController _boss;
        /// <summary>第一次進房的鏡頭特寫秒數。</summary>
        [SerializeField] private float _closeUpSeconds = 3.5f;
        /// <summary>[待確認] Boss 是否追出房間（原型：否）。</summary>
        [SerializeField] private bool _bossFollowsOutside;

        [Header("精靈變身演出（第一次進房）")]
        /// <summary>精靈圖（場景中飛行、變身用）。</summary>
        [SerializeField] private Sprite _elfSprite;
        /// <summary>精靈在場景中的高度（單位）。</summary>
        [SerializeField] private float _elfHeight = 1.4f;
        /// <summary>海兔從玩家頭上跳到身旁的秒數（場景有 SeaHare 時）。</summary>
        [SerializeField] private float _leaveHeadSeconds = 0.6f;
        /// <summary>第一句台詞（精靈在玩家身旁說）。</summary>
        [SerializeField] private string _tauntLine1 = "終於上當了……";
        /// <summary>第二句（玩家已集齊雕像時，飛向 Boss 位置途中說）。</summary>
        [SerializeField] private string _tauntLine2WithSeals = "謝謝你一路幫我把雕像帶回來。";
        /// <summary>第二句（玩家沒集齊雕像時）。</summary>
        [SerializeField] private string _tauntLine2WithoutSeals = "可惜你沒湊齊雕像，也封印不了我。";
        /// <summary>第三句（變身前）。</summary>
        [SerializeField] private string _tauntLine3 = "這片深海，就是你的墓。";
        /// <summary>每句台詞停留秒數。</summary>
        [SerializeField] private float _lineSeconds = 1.6f;
        /// <summary>精靈飛到 Boss 位置的秒數。</summary>
        [SerializeField] private float _flySeconds = 1.6f;
        /// <summary>變身（發光、抖動、放大）秒數。</summary>
        [SerializeField] private float _transformSeconds = 1.2f;
        /// <summary>變身時放大到的倍率。</summary>
        [SerializeField] private float _transformScale = 3f;
        /// <summary>變身光色。</summary>
        [SerializeField] private Color _transformColor = new Color(1f, 0.2f, 0.15f);
        /// <summary>閃白淡出秒數。</summary>
        [SerializeField] private float _flashSeconds = 0.8f;

        #region Status

        /// <summary>是否第一次進到 Boss 房間。</summary>
        public bool IsFirstEntry { get; private set; } = true;
        /// <summary>玩家目前是否在 Boss 房內。</summary>
        public bool PlayerInside { get; private set; }

        #endregion

        /// <summary>所在區塊。</summary>
        private Room _room;

        /// <summary>找出所在區塊並訂閱進出事件（Room 在 OnEnable 註冊，因此放在 Start）。</summary>
        private void Start()
        {
            _room = Room.FindAt(transform.position);
            if (_room == null)
            {
                Debug.LogError("[DrownedDream] BossArea 不在任何 Room 範圍內", this);
                return;
            }
            _room.PlayerEntered += OnPlayerEntered;
            _room.PlayerExited += OnPlayerExited;
        }

        /// <summary>取消訂閱。</summary>
        private void OnDestroy()
        {
            if (_room == null) return;
            _room.PlayerEntered -= OnPlayerEntered;
            _room.PlayerExited -= OnPlayerExited;
        }

        #region Action

        /// <summary>偵測到玩家進入 Boss 房。</summary>
        private void OnPlayerEntered()
        {
            PlayerInside = true;
            var player = Player.Instance;
            if (player == null || _boss == null || _boss.IsSealed) return;

            if (IsFirstEntry)
            {
                IsFirstEntry = false;
                StartCoroutine(FirstEntryRoutine(player));
                return;
            }
            AnnounceAndActivate(player);
        }

        /// <summary>偵測到玩家離開 Boss 房。</summary>
        private void OnPlayerExited()
        {
            PlayerInside = false;
            if (_boss != null && !_bossFollowsOutside) _boss.Deactivate();
        }

        /// <summary>
        /// 第一次進房（鎖輸入）：精靈出現在玩家身旁說第一句 → 鏡頭轉向 Boss、精靈飛過去說第二句 → 說第三句 →
        /// 發光抖動放大 → 閃白、Boss 現身咆哮 → 特寫數秒 → 切回玩家、Boss 啟動。
        /// 台詞在 Boss 現身前發出，因此由左下精靈對話框說；現身後精靈退場（HUD 收到 BossRevealed）。
        /// </summary>
        private IEnumerator FirstEntryRoutine(Player player)
        {
            player.Input.SetLocked(true);
            var cam = GameCamera.Instance;
            Vector2 start = (Vector2)player.transform.position + new Vector2(-1.2f, 1.6f);
            Vector2 target = _boss.transform.position;
            Transform elf;
            Light2D elfLight;
            var hare = SeaHare.Instance;
            if (hare != null)
            {
                // 0. 海兔從玩家頭上跳下來，邊跳邊放大到演出大小
                elf = hare.LeaveHead(30);
                elfLight = AddElfLight(elf.gameObject);
                Vector2 from = elf.position;
                Vector3 fromScale = elf.localScale;
                Vector3 toScale = Vector3.one * (_elfHeight / Mathf.Max(0.01f, hare.SpriteHeight));
                for (float t = 0f; t < _leaveHeadSeconds; t += Time.deltaTime)
                {
                    float k = Mathf.SmoothStep(0f, 1f, t / _leaveHeadSeconds);
                    elf.position = Vector2.Lerp(from, start, k) + Vector2.up * (Mathf.Sin(k * Mathf.PI) * 1f);
                    elf.localScale = Vector3.Lerp(fromScale, toScale, k);
                    yield return null;
                }
            }
            else
            {
                elf = SpawnElf(start, out elfLight);
            }

            // 1. 精靈在玩家身旁
            GameEvents.ShowMessage(_tauntLine1, _lineSeconds);
            yield return Hover(elf, start, _lineSeconds);

            // 2. 鏡頭轉向 Boss，精靈飛過去
            if (cam != null) cam.SwitchToBossRoom(_boss.gameObject);
            GameEvents.ShowMessage(player.Status.HasAllSeals ? _tauntLine2WithSeals : _tauntLine2WithoutSeals, _flySeconds + 0.3f);
            for (float t = 0f; t < _flySeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / _flySeconds);
                if (elf != null) elf.position = Vector2.Lerp(start, target, k) + Vector2.up * (Mathf.Sin(k * Mathf.PI) * 2f);
                yield return null;
            }

            // 3. 最後一句
            GameEvents.ShowMessage(_tauntLine3, _lineSeconds);
            yield return Hover(elf, target, _lineSeconds);

            // 4. 變身：轉紅、抖動、放大、光變強
            var sr = elf != null ? elf.GetComponent<SpriteRenderer>() : null;
            Vector3 baseScale = elf != null ? elf.localScale : Vector3.one;
            for (float t = 0f; t < _transformSeconds; t += Time.deltaTime)
            {
                float k = t / _transformSeconds;
                if (elf != null)
                {
                    elf.position = target + Random.insideUnitCircle * (0.05f + 0.25f * k);
                    elf.localScale = baseScale * Mathf.Lerp(1f, _transformScale, k * k);
                }
                if (sr != null) sr.color = Color.Lerp(Color.white, _transformColor, k);
                if (elfLight != null) elfLight.intensity = Mathf.Lerp(1f, 6f, k);
                if (cam != null) cam.Shake(0.1f * k, 0.1f);
                yield return null;
            }

            // 5. 閃白、Boss 現身
            GameEvents.RaiseScreenFlash(_flashSeconds);
            if (elf != null) Destroy(elf.gameObject);
            _boss.Reveal();
            GameEvents.RaiseBossRevealed(); // 咆哮音效、精靈對話框退場
            if (cam != null) cam.Shake(0.4f, 0.8f);
            yield return new WaitForSeconds(_closeUpSeconds);

            if (cam != null) cam.SwitchToPlayer();
            player.Input.SetLocked(false);
            if (PlayerInside) AnnounceAndActivate(player);
        }

        /// <summary>在場景中產生精靈（場景沒有海兔時的備案，帶一盞白光），回傳 Transform；沒有精靈圖時回傳 null（演出照常進行）。</summary>
        private Transform SpawnElf(Vector2 position, out Light2D light)
        {
            light = null;
            if (_elfSprite == null) return null;
            var go = new GameObject("ElfTransform");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _elfSprite;
            sr.sortingOrder = 30; // 在 Boss、浮岩、玩家前面
            float h = _elfSprite.bounds.size.y;
            if (h > 0f) go.transform.localScale = Vector3.one * (_elfHeight / h);
            light = AddElfLight(go);
            return go.transform;
        }

        /// <summary>幫精靈加一盞白光（變身時調亮）。</summary>
        private static Light2D AddElfLight(GameObject go)
        {
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(0.8f, 0.95f, 1f);
            light.intensity = 1f;
            light.pointLightOuterRadius = 3f;
            return light;
        }

        /// <summary>精靈在 center 附近輕輕上下飄 seconds 秒（elf 為 null 時只等待）。</summary>
        private static IEnumerator Hover(Transform elf, Vector2 center, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (elf != null) elf.position = center + Vector2.up * (Mathf.Sin(t * 3f) * 0.15f);
                yield return null;
            }
        }

        /// <summary>提示封印進度並啟動 Boss（不論道具是否集齊都會攻擊，2026-10-04 改）。</summary>
        private void AnnounceAndActivate(Player player)
        {
            var status = player.Status;
            GameEvents.ShowMessage(status.HasAllSeals
                ? $"封印道具已齊（{status.SealCount}/{status.RequiredSeals}）——邪神變得更加猛烈！在牠的攻擊下啟動左右兩座祭壇（按 E）才能封印！"
                : $"封印道具不足（{status.SealCount}/{status.RequiredSeals}）……牠醒了！", 3f);
            _boss.Activate();
        }

        #endregion
    }
}
