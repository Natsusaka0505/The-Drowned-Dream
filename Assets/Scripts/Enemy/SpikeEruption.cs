using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DrownedDream
{
    /// <summary>
    /// 精緻版地刺（Boss 浮岩用）：
    /// 預告：地面裂縫由中間往兩側發光、碎石顫動、地底紅光脈動，越接近發動越快；
    /// 發動：骨刺由中間往兩側依序彈出（高低、傾角不一，略為超出再回彈），噴出碎石與塵霧、紅光閃一下、鏡頭小震；
    /// 收回：骨刺下沉淡出。判定只在發動期間、每次只扣一次。
    /// </summary>
    public class SpikeEruption : MonoBehaviour
    {
        /// <summary>玩家判定的額外寬容（單位）。</summary>
        private const float PlayerPadding = 0.35f;
        /// <summary>骨刺彈出時間（佔判定時間的比例）。</summary>
        private const float RiseRatio = 0.3f;
        /// <summary>相鄰骨刺彈出的時間差（秒）。</summary>
        private const float Stagger = 0.035f;
        /// <summary>收回秒數。</summary>
        private const float RetractTime = 0.35f;
        /// <summary>碎石數量。</summary>
        private const int DebrisCount = 10;

        /// <summary>地面中央（骨刺根部）。</summary>
        private Vector2 _ground;
        /// <summary>範圍寬度。</summary>
        private float _width;
        /// <summary>最高骨刺高度。</summary>
        private float _height;
        /// <summary>預告秒數。</summary>
        private float _warnTime;
        /// <summary>判定秒數。</summary>
        private float _activeTime;
        /// <summary>傷害（扣 HP）。</summary>
        private float _damage;
        /// <summary>主光色（裂縫、閃光）。</summary>
        private Color _glowColor;
        /// <summary>已進行秒數（預告 + 判定 + 收回）。</summary>
        private float _time;
        /// <summary>是否已命中。</summary>
        private bool _hasHit;
        /// <summary>是否已噴發（碎石、閃光只做一次）。</summary>
        private bool _erupted;

        /// <summary>裂縫發光條。</summary>
        private SpriteRenderer _crack;
        /// <summary>裂縫外圍柔光。</summary>
        private SpriteRenderer _crackGlow;
        /// <summary>地底光源。</summary>
        private Light2D _light;
        /// <summary>骨刺。</summary>
        private SpriteRenderer[] _spikes;
        /// <summary>各骨刺完整高度。</summary>
        private float[] _spikeHeights;
        /// <summary>各骨刺寬度。</summary>
        private float[] _spikeWidths;
        /// <summary>預告時顫動的小碎石。</summary>
        private Transform[] _pebbles;
        /// <summary>小碎石原位。</summary>
        private Vector2[] _pebbleBase;

        /// <summary>
        /// 產生一次地刺：ground = 地面中央（站立面）、width = 範圍寬、height = 最高骨刺高度；
        /// warn = 預告秒數、active = 判定秒數、damage = 扣 HP。
        /// </summary>
        public static SpikeEruption Spawn(Vector2 ground, float width, float height, float warn, float active, float damage, Color glowColor)
        {
            var go = new GameObject("SpikeEruption");
            go.transform.position = ground;
            var s = go.AddComponent<SpikeEruption>();
            s._ground = ground;
            s._width = Mathf.Max(0.5f, width);
            s._height = Mathf.Max(0.3f, height);
            s._warnTime = Mathf.Max(0.05f, warn);
            s._activeTime = Mathf.Max(0.05f, active);
            s._damage = damage;
            s._glowColor = glowColor;
            s.Build();
            return s;
        }

        /// <summary>建立裂縫、光源、骨刺、小碎石。</summary>
        private void Build()
        {
            _crackGlow = Part("CrackGlow", HazardSprites.SoftBand, 11, Vector2.zero);
            _crack = Part("Crack", HazardSprites.Square, 12, Vector2.zero);

            _light = gameObject.AddComponent<Light2D>();
            _light.lightType = Light2D.LightType.Point;
            _light.color = _glowColor;
            _light.pointLightInnerRadius = 0.2f;
            _light.pointLightOuterRadius = _width * 0.9f;
            _light.intensity = 0f;

            int count = Mathf.Clamp(Mathf.RoundToInt(_width / 0.4f), 5, 11);
            _spikes = new SpriteRenderer[count];
            _spikeHeights = new float[count];
            _spikeWidths = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : i / (float)(count - 1) * 2f - 1f; // -1 ~ 1
                float x = t * _width * 0.45f + Random.Range(-0.06f, 0.06f);
                var sr = Part($"Spike{i}", HazardSprites.BoneSpike, 13 + (i % 2), new Vector2(x, -0.05f));
                // 中間最高、往外變矮，加一點隨機；外側往外傾
                _spikeHeights[i] = _height * Mathf.Lerp(1f, 0.45f, Mathf.Abs(t)) * Random.Range(0.85f, 1.05f);
                _spikeWidths[i] = Random.Range(0.35f, 0.5f) * Mathf.Lerp(1f, 0.75f, Mathf.Abs(t));
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, -t * 18f + Random.Range(-5f, 5f));
                sr.transform.localScale = new Vector3(_spikeWidths[i], 0f, 1f);
                _spikes[i] = sr;
            }

            _pebbles = new Transform[6];
            _pebbleBase = new Vector2[_pebbles.Length];
            for (int i = 0; i < _pebbles.Length; i++)
            {
                _pebbleBase[i] = new Vector2(Random.Range(-_width / 2f, _width / 2f), 0.05f);
                var sr = Part($"Pebble{i}", HazardSprites.Circle, 12, _pebbleBase[i]);
                sr.color = new Color(0.25f, 0.22f, 0.24f);
                sr.transform.localScale = new Vector3(Random.Range(0.08f, 0.16f), Random.Range(0.06f, 0.12f), 1f);
                _pebbles[i] = sr.transform;
            }
            UpdateWarn(0f);
        }

        /// <summary>建立子物件 SpriteRenderer。</summary>
        private SpriteRenderer Part(string name, Sprite sprite, int order, Vector2 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>預告 → 噴發判定 → 收回 → 銷毀。</summary>
        private void Update()
        {
            if (!GameFlow.IsPlaying) return;
            _time += Time.deltaTime;

            if (_time < _warnTime)
            {
                UpdateWarn(_time / _warnTime);
                return;
            }

            float active = _time - _warnTime;
            if (!_erupted) Erupt();
            if (active < _activeTime)
            {
                UpdateActive(active);
                TryHit(active);
                return;
            }

            float retract = (active - _activeTime) / RetractTime;
            if (retract >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            UpdateRetract(retract);
        }

        /// <summary>預告：裂縫由中間往兩側延伸、發光脈動（後段加快），碎石顫動，地底光漸強。</summary>
        private void UpdateWarn(float t)
        {
            float ease = 1f - (1f - t) * (1f - t);
            float pulse = 0.5f + 0.5f * Mathf.Sin(_time * Mathf.Lerp(10f, 34f, t));
            float len = _width * Mathf.Lerp(0.1f, 1f, ease);

            SetPart(_crack, new Vector2(len, 0.06f), Mathf.Lerp(0.3f, 1f, t) * (0.65f + 0.35f * pulse), Color.Lerp(_glowColor, Color.white, 0.35f * pulse));
            SetPart(_crackGlow, new Vector2(len * 1.1f, Mathf.Lerp(0.2f, 0.9f, ease)), Mathf.Lerp(0.15f, 0.55f, t) * (0.6f + 0.4f * pulse), _glowColor);
            _light.intensity = Mathf.Lerp(0f, 1.6f, t) * (0.6f + 0.4f * pulse);

            float shake = Mathf.Lerp(0.005f, 0.05f, t);
            for (int i = 0; i < _pebbles.Length; i++)
            {
                _pebbles[i].localPosition = _pebbleBase[i] + new Vector2(Random.Range(-shake, shake), Mathf.Abs(Random.Range(0f, shake * 1.5f)));
            }
        }

        /// <summary>噴發：碎石飛濺、塵霧擴散、閃光、鏡頭小震。</summary>
        private void Erupt()
        {
            _erupted = true;
            foreach (var p in _pebbles) p.gameObject.SetActive(false);
            for (int i = 0; i < DebrisCount; i++) SpikeDebris.Spawn(_ground + new Vector2(Random.Range(-_width / 2f, _width / 2f), 0.1f));
            for (int i = 0; i < 3; i++) SpikeDust.Spawn(_ground + new Vector2((i - 1) * _width * 0.35f, 0.15f), _width * 0.5f, _glowColor);
            if (GameCamera.Instance != null) GameCamera.Instance.Shake(0.12f, 0.25f);
        }

        /// <summary>判定中：骨刺依序彈出（回彈曲線），裂縫光與光源由強轉弱。</summary>
        private void UpdateActive(float elapsed)
        {
            float k = elapsed / _activeTime;
            for (int i = 0; i < _spikes.Length; i++)
            {
                float h = _spikeHeights[i] * Rise(i, elapsed);
                _spikes[i].transform.localScale = new Vector3(_spikeWidths[i], h, 1f);
                _spikes[i].color = Color.white;
            }
            SetPart(_crack, new Vector2(_width, 0.08f), Mathf.Lerp(1f, 0.4f, k), Color.white);
            SetPart(_crackGlow, new Vector2(_width * 1.2f, 1.2f), Mathf.Lerp(0.8f, 0.25f, k), _glowColor);
            _light.intensity = Mathf.Lerp(4f, 1f, k);
        }

        /// <summary>收回：骨刺下沉並淡出，光熄滅。</summary>
        private void UpdateRetract(float t)
        {
            float r = 1f - t;
            for (int i = 0; i < _spikes.Length; i++)
            {
                _spikes[i].transform.localScale = new Vector3(_spikeWidths[i], _spikeHeights[i] * r * r, 1f);
                _spikes[i].color = new Color(1f, 1f, 1f, r);
            }
            SetPart(_crack, new Vector2(_width, 0.06f), 0.4f * r, _glowColor);
            SetPart(_crackGlow, new Vector2(_width * 1.2f, 1f), 0.25f * r, _glowColor);
            _light.intensity = r;
        }

        /// <summary>第 i 根骨刺在 elapsed 秒時的高度比例（中間先出，0 → 約 1.12 → 1）。</summary>
        private float Rise(int i, float elapsed)
        {
            float delay = Mathf.Abs(i - (_spikes.Length - 1) / 2f) * Stagger;
            float rise = Mathf.Max(0.01f, _activeTime * RiseRatio);
            float t = Mathf.Clamp01((elapsed - delay) / rise);
            return t < 1f ? Mathf.Sin(t * Mathf.PI * 0.65f) / Mathf.Sin(Mathf.PI * 0.65f) : 1f;
        }

        /// <summary>判定：骨刺冒出到一半後，玩家在範圍內（地面到骨刺高度）就扣一次血。</summary>
        private void TryHit(float elapsed)
        {
            if (_hasHit || elapsed < _activeTime * RiseRatio * 0.5f) return;
            var player = Player.Instance;
            if (!HazardSprites.CanHit(player)) return;
            Vector2 p = player.transform.position;
            bool inX = Mathf.Abs(p.x - _ground.x) <= _width / 2f + PlayerPadding;
            bool inY = p.y >= _ground.y - PlayerPadding && p.y <= _ground.y + _height + PlayerPadding;
            if (inX && inY) _hasHit = HazardSprites.HitPlayer(player, _damage, 0f, _ground);
        }

        /// <summary>設定外觀子物件大小、透明度與顏色。</summary>
        private static void SetPart(SpriteRenderer sr, Vector2 size, float alpha, Color color)
        {
            sr.transform.localScale = new Vector3(size.x, size.y, 1f);
            sr.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }
    }

    /// <summary>地刺噴出的碎石：往上噴、受重力落下、旋轉並淡出。</summary>
    public class SpikeDebris : MonoBehaviour
    {
        /// <summary>速度。</summary>
        private Vector2 _velocity;
        /// <summary>旋轉速度（度 / 秒）。</summary>
        private float _spin;
        /// <summary>剩餘秒數。</summary>
        private float _life;
        /// <summary>總秒數。</summary>
        private float _lifeTotal;
        /// <summary>外觀。</summary>
        private SpriteRenderer _sr;

        /// <summary>重力加速度。</summary>
        private const float Gravity = 14f;

        /// <summary>在 position 產生一顆碎石。</summary>
        public static void Spawn(Vector2 position)
        {
            var sr = HazardSprites.Spawn("SpikeDebris", HazardSprites.Square, position, new Color(0.3f, 0.27f, 0.3f), 14);
            sr.transform.localScale = new Vector3(Random.Range(0.08f, 0.2f), Random.Range(0.06f, 0.15f), 1f);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            var d = sr.gameObject.AddComponent<SpikeDebris>();
            d._sr = sr;
            d._velocity = new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(3.5f, 7f));
            d._spin = Random.Range(-540f, 540f);
            d._lifeTotal = d._life = Random.Range(0.5f, 0.8f);
        }

        /// <summary>移動、旋轉、淡出。</summary>
        private void Update()
        {
            if (!GameFlow.IsPlaying) return;
            float dt = Time.deltaTime;
            _life -= dt;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            _velocity.y -= Gravity * dt;
            transform.position += (Vector3)(_velocity * dt);
            transform.Rotate(0f, 0f, _spin * dt);
            var c = _sr.color;
            c.a = Mathf.Clamp01(_life / _lifeTotal * 1.5f);
            _sr.color = c;
        }
    }

    /// <summary>地刺噴發的塵霧：柔光圓往外擴散、上飄並淡出。</summary>
    public class SpikeDust : MonoBehaviour
    {
        /// <summary>已進行秒數。</summary>
        private float _time;
        /// <summary>最大尺寸。</summary>
        private float _size;
        /// <summary>顏色。</summary>
        private Color _color;
        /// <summary>外觀。</summary>
        private SpriteRenderer _sr;

        /// <summary>存在秒數。</summary>
        private const float Lifetime = 0.7f;

        /// <summary>在 position 產生一團塵霧（size = 最大直徑，tint 與地刺光色混合）。</summary>
        public static void Spawn(Vector2 position, float size, Color tint)
        {
            var color = Color.Lerp(new Color(0.55f, 0.5f, 0.5f), tint, 0.25f);
            var sr = HazardSprites.Spawn("SpikeDust", HazardSprites.Glow, position, color, 15);
            var d = sr.gameObject.AddComponent<SpikeDust>();
            d._sr = sr;
            d._size = Mathf.Max(0.5f, size);
            d._color = color;
            d.Update();
        }

        /// <summary>擴散、上飄、淡出。</summary>
        private void Update()
        {
            if (!GameFlow.IsPlaying) return;
            _time += Time.deltaTime;
            float k = _time / Lifetime;
            if (k >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            float ease = 1f - (1f - k) * (1f - k);
            transform.localScale = new Vector3(_size * Mathf.Lerp(0.4f, 1.6f, ease), _size * Mathf.Lerp(0.25f, 0.8f, ease), 1f);
            transform.position += Vector3.up * (0.6f * Time.deltaTime);
            _sr.color = new Color(_color.r, _color.g, _color.b, 0.55f * (1f - k));
        }
    }
}
