using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DrownedDream
{
    /// <summary>存檔 / 復活點（F-DTH-01）。[待確認] 復活點形式，原型為碰觸物件更新。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        /// <summary>外觀 Renderer（啟用時變色）。</summary>
        [SerializeField] private SpriteRenderer _renderer;
        /// <summary>啟用中的顏色。</summary>
        [SerializeField] private Color _activeColor = new Color(0.4f, 1f, 0.8f);
        /// <summary>頂部光源（未啟用紅光、啟用後黃光；沒有可留空）。</summary>
        [SerializeField] private Light2D _light;
        /// <summary>未啟用時的光色。</summary>
        [SerializeField] private Color _inactiveLightColor = new Color(1f, 0.15f, 0.12f);
        /// <summary>啟用後的光色。</summary>
        [SerializeField] private Color _activeLightColor = new Color(1f, 0.85f, 0.3f);
        /// <summary>啟用 / 熄滅時光色與外觀顏色漸變的秒數。</summary>
        [SerializeField] private float _transitionTime = 0.8f;
        /// <summary>啟用瞬間光強度衝高的倍率（漸變中段最亮，結束回到原強度；1 = 不衝高）。</summary>
        [SerializeField] private float _activateFlare = 1.8f;

        /// <summary>目前啟用的存檔點。</summary>
        private static Checkpoint s_current;
        /// <summary>未啟用時的顏色。</summary>
        private Color _inactiveColor;
        /// <summary>光源原本的強度（場景設定值）。</summary>
        private float _baseIntensity;
        /// <summary>漸變起點的光色。</summary>
        private Color _fromLightColor;
        /// <summary>漸變終點的光色。</summary>
        private Color _toLightColor;
        /// <summary>漸變起點的外觀顏色。</summary>
        private Color _fromColor;
        /// <summary>漸變終點的外觀顏色。</summary>
        private Color _toColor;
        /// <summary>漸變已進行秒數（小於 0 = 沒有在漸變）。</summary>
        private float _transitionTimer = -1f;
        /// <summary>這次漸變是否為啟用（啟用才衝高強度）。</summary>
        private bool _transitionToActive;

        /// <summary>設定為 Trigger 並記錄原色。</summary>
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (_renderer != null) _inactiveColor = _renderer.color;
            if (_light != null) _baseIntensity = _light.intensity;
            SetLit(false, true);
        }

        /// <summary>播放光色漸變：顏色由起點平滑過渡到終點，啟用時強度中段衝高再回落。</summary>
        private void Update()
        {
            if (_transitionTimer < 0f) return;
            _transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_transitionTimer / Mathf.Max(0.01f, _transitionTime));
            float k = Mathf.SmoothStep(0f, 1f, t);
            if (_renderer != null) _renderer.color = Color.Lerp(_fromColor, _toColor, k);
            if (_light != null)
            {
                _light.color = Color.Lerp(_fromLightColor, _toLightColor, k);
                float flare = _transitionToActive ? Mathf.Sin(t * Mathf.PI) * (_activateFlare - 1f) : 0f;
                _light.intensity = _baseIntensity * (1f + flare);
            }
            if (t >= 1f) _transitionTimer = -1f;
        }

        /// <summary>切換外觀顏色與光色（active = 目前的復活點；instant = 直接套用不漸變）。</summary>
        private void SetLit(bool active, bool instant = false)
        {
            _fromColor = _renderer != null ? _renderer.color : Color.white;
            _toColor = active ? _activeColor : _inactiveColor;
            _fromLightColor = _light != null ? _light.color : Color.white;
            _toLightColor = active ? _activeLightColor : _inactiveLightColor;
            _transitionToActive = active;
            _transitionTimer = 0f;
            if (!instant && _transitionTime > 0f) return;

            if (_renderer != null) _renderer.color = _toColor;
            if (_light != null)
            {
                _light.color = _toLightColor;
                _light.intensity = _baseIntensity;
            }
            _transitionTimer = -1f;
        }

        /// <summary>玩家碰觸：更新復活點。</summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (s_current == this || other.attachedRigidbody == null) return;
            var respawn = other.attachedRigidbody.GetComponent<PlayerRespawn>();
            if (respawn == null) return;

            if (s_current != null) s_current.SetLit(false);
            s_current = this;
            SetLit(true);
            respawn.SetRespawnPoint(transform.position);
            GameEvents.ShowMessage("記憶在此刻下印記（復活點）", 1.5f);
        }

        /// <summary>銷毀時清除目前存檔點。</summary>
        private void OnDestroy()
        {
            if (s_current == this) s_current = null;
        }
    }
}
