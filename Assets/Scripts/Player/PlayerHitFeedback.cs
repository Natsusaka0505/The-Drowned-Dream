using System.Collections;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// Action：玩家受擊反饋（訂閱 PlayerStatus.Damaged）：擊退、角色閃紅、鏡頭震動、短暫頓幀。
    /// 畫面紅框由 HUD、受擊音效由 GameAudio、頭上海兔受驚由 SeaHare 各自訂閱同一事件。
    /// </summary>
    public class PlayerHitFeedback : MonoBehaviour
    {
        [Header("擊退")]
        /// <summary>擊退水平速度（往攻擊來源反方向）。</summary>
        [SerializeField] private float _knockbackSpeed = 6f;
        /// <summary>擊退向上速度。</summary>
        [SerializeField] private float _knockbackUp = 4f;
        /// <summary>擊退期間不吃左右輸入的秒數。</summary>
        [SerializeField] private float _knockbackSeconds = 0.2f;

        [Header("閃紅")]
        /// <summary>閃紅的顏色（只改 RGB，保留憋氣透明度）。</summary>
        [SerializeField] private Color _tintColor = new Color(1f, 0.25f, 0.25f);
        /// <summary>閃紅淡回原色的秒數。</summary>
        [SerializeField] private float _tintSeconds = 0.35f;
        /// <summary>要閃紅的 Renderer（空 = 自動抓玩家底下除了海兔以外的 SpriteRenderer）。</summary>
        [SerializeField] private SpriteRenderer[] _tintRenderers;

        [Header("鏡頭 / 頓幀")]
        /// <summary>鏡頭震動幅度。</summary>
        [SerializeField] private float _shakeAmount = 0.18f;
        /// <summary>鏡頭震動秒數。</summary>
        [SerializeField] private float _shakeSeconds = 0.25f;
        /// <summary>頓幀秒數（真實時間；0 = 不頓幀）。</summary>
        [SerializeField] private float _hitStopSeconds = 0.06f;
        /// <summary>頓幀時的時間倍率。</summary>
        [SerializeField, Range(0f, 1f)] private float _hitStopTimeScale = 0.05f;

        /// <summary>玩家數值（受擊事件、攻擊來源）。</summary>
        private PlayerStatus _status;
        /// <summary>移動（擊退、面向）。</summary>
        private PlayerMove _move;
        /// <summary>閃紅剩餘秒數。</summary>
        private float _tintTimer;

        /// <summary>快取元件；沒指定閃紅 Renderer 時自動抓。</summary>
        private void Awake()
        {
            _status = GetComponent<PlayerStatus>();
            _move = GetComponent<PlayerMove>();
            if (_tintRenderers == null || _tintRenderers.Length == 0)
            {
                var list = new System.Collections.Generic.List<SpriteRenderer>();
                foreach (var r in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (r.GetComponent<SeaHare>() == null) list.Add(r);
                }
                _tintRenderers = list.ToArray();
            }
        }

        /// <summary>訂閱受擊事件。</summary>
        private void OnEnable() => _status.Damaged += OnDamaged;

        /// <summary>取消訂閱，並把顏色、時間倍率恢復。</summary>
        private void OnDisable()
        {
            _status.Damaged -= OnDamaged;
            ApplyTint(0f);
            _tintTimer = 0f;
            if (Mathf.Approximately(Time.timeScale, _hitStopTimeScale)) Time.timeScale = 1f; // 頓幀中被停用：協程會中斷，這裡補恢復
        }

        /// <summary>受擊：擊退、開始閃紅、鏡頭震動、頓幀。</summary>
        private void OnDamaged()
        {
            // 擊退方向：遠離攻擊來源；來源不明時往面向反方向
            float dir = -_move.Facing;
            if (_status.LastHitFrom.HasValue)
            {
                float dx = transform.position.x - _status.LastHitFrom.Value.x;
                if (Mathf.Abs(dx) > 0.05f) dir = Mathf.Sign(dx);
            }
            _move.Knockback(new Vector2(dir * _knockbackSpeed, _knockbackUp), _knockbackSeconds);

            _tintTimer = _tintSeconds;
            ApplyTint(1f);
            if (GameCamera.Instance != null) GameCamera.Instance.Shake(_shakeAmount, _shakeSeconds);
            if (_hitStopSeconds > 0f && Mathf.Approximately(Time.timeScale, 1f)) StartCoroutine(HitStop());
        }

        /// <summary>閃紅淡回原色。</summary>
        private void LateUpdate()
        {
            if (_tintTimer <= 0f) return;
            _tintTimer -= Time.deltaTime;
            ApplyTint(_tintSeconds > 0f ? Mathf.Clamp01(_tintTimer / _tintSeconds) : 0f);
        }

        /// <summary>依強度 k（0 = 原色、1 = 全紅）設定顏色，保留透明度。</summary>
        private void ApplyTint(float k)
        {
            if (_tintRenderers == null) return;
            var rgb = Color.Lerp(Color.white, _tintColor, k);
            foreach (var r in _tintRenderers)
            {
                if (r == null) continue;
                rgb.a = r.color.a;
                r.color = rgb;
            }
        }

        /// <summary>頓幀：短暫放慢時間；結束時只在倍率沒被別人改過（例如打開背包暫停）時才恢復。</summary>
        private IEnumerator HitStop()
        {
            Time.timeScale = _hitStopTimeScale;
            yield return new WaitForSecondsRealtime(_hitStopSeconds);
            if (Mathf.Approximately(Time.timeScale, _hitStopTimeScale)) Time.timeScale = 1f;
        }
    }
}
