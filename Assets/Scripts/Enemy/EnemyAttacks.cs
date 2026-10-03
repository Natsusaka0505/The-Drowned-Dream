using UnityEngine;

namespace DrownedDream
{
    /// <summary>敵人攻擊用的原型圖（執行期產生，不需要美術資產）。</summary>
    public static class HazardSprites
    {
        /// <summary>方塊快取。</summary>
        private static Sprite s_square;
        /// <summary>圓形快取。</summary>
        private static Sprite s_circle;

        /// <summary>1 單位大小的白色方塊。</summary>
        public static Sprite Square
        {
            get
            {
                if (s_square == null) s_square = Make(false);
                return s_square;
            }
        }

        /// <summary>1 單位直徑的白色圓形。</summary>
        public static Sprite Circle
        {
            get
            {
                if (s_circle == null) s_circle = Make(true);
                return s_circle;
            }
        }

        /// <summary>地刺快取。</summary>
        private static Sprite s_spike;

        /// <summary>地刺：底寬 0.5、高 1 的尖三角（32×64 像素、PPU 64，pivot 在底部中央），由下往上漸亮，邊緣柔化。</summary>
        public static Sprite Spike
        {
            get
            {
                if (s_spike != null) return s_spike;
                const int w = 32, h = 64;
                var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < h; y++)
                {
                    float v = (y + 0.5f) / h;                 // 0 底 → 1 尖
                    float half = (1f - v) * w / 2f;            // 這一列三角形的半寬
                    float shade = Mathf.Lerp(0.45f, 1f, v);    // 底部暗、尖端亮
                    for (int x = 0; x < w; x++)
                    {
                        float dx = Mathf.Abs(x + 0.5f - w / 2f);
                        float a = Mathf.Clamp01(half - dx + 0.5f); // 邊緣 1px 柔化
                        tex.SetPixel(x, y, new Color(shade, shade, shade, a));
                    }
                }
                tex.Apply();
                s_spike = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), h);
                return s_spike;
            }
        }

        /// <summary>柔光帶快取。</summary>
        private static Sprite s_softBand;

        /// <summary>1 單位大小的柔光帶：沿 X 實心，沿 Y 從中線往上下兩側淡出（預測線 / 判定區用）。</summary>
        public static Sprite SoftBand
        {
            get
            {
                if (s_softBand != null) return s_softBand;
                const int size = 32;
                var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < size; y++)
                {
                    float d = Mathf.Abs((y + 0.5f) / size * 2f - 1f); // 中線 0 → 邊緣 1
                    float a = Mathf.Pow(1f - d, 1.6f);
                    for (int x = 0; x < size; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                s_softBand = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                return s_softBand;
            }
        }

        /// <summary>產生 32×32 白色方塊 / 圓形（32px = 1 單位）。</summary>
        private static Sprite Make(bool circle)
        {
            const int size = 32;
            var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float a = circle ? Mathf.Clamp01(r - d) : 1f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>建立一個帶 SpriteRenderer 的物件。</summary>
        public static SpriteRenderer Spawn(string name, Sprite sprite, Vector2 position, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>玩家是否可被攻擊（活著且沒有憋氣隱形）。</summary>
        public static bool CanHit(Player player) => player != null && player.IsVisibleToEnemies;

        /// <summary>對玩家造成傷害（HP 依 VitalsConfig 比例扣）與額外 SAN 傷害，回傳是否命中。</summary>
        public static bool HitPlayer(Player player, float damage, float sanityDamage)
        {
            if (!CanHit(player)) return false;
            if (!player.Status.TakeHit(damage)) return false;
            if (sanityDamage > 0f) player.Status.LoseSanity(sanityDamage);
            return true;
        }
    }

    /// <summary>
    /// 敵人彈幕（Boss 扇形彈 / 追蹤彈、魚怪泡泡彈）。碰到玩家造成傷害、碰到地形消失；
    /// 可設定追蹤轉向速度，以及能否被魚叉打破。
    /// </summary>
    public class EnemyProjectile : MonoBehaviour, IDamageable
    {
        /// <summary>玩家判定半徑（玩家中心到彈幕中心）。</summary>
        private const float PlayerHitRadius = 0.6f;

        /// <summary>移動速度向量。</summary>
        private Vector2 _velocity;
        /// <summary>傷害（扣 HP）。</summary>
        private float _damage;
        /// <summary>額外 SAN 傷害。</summary>
        private float _sanityDamage;
        /// <summary>剩餘存活秒數。</summary>
        private float _lifetime;
        /// <summary>追蹤轉向速度（度 / 秒，0 = 直線）。</summary>
        private float _turnRate;
        /// <summary>會擋下彈幕的 Layer。</summary>
        private LayerMask _blockMask;
        /// <summary>是否已被打破 / 命中。</summary>
        private bool _dead;

        /// <summary>是否還存在（魚叉判定用）。</summary>
        public bool IsAlive => !_dead;

        /// <summary>執行期產生一顆彈幕（breakable = 可被魚叉打破，會放在 Enemy Layer 並加觸發碰撞）。</summary>
        public static EnemyProjectile Spawn(Vector2 position, Vector2 velocity, float size, Color color, float damage, float sanityDamage,
            float lifetime, LayerMask blockMask, float turnRate = 0f, bool breakable = false)
        {
            var sr = HazardSprites.Spawn("EnemyProjectile", HazardSprites.Circle, position, color, 20);
            sr.transform.localScale = Vector3.one * size;
            var p = sr.gameObject.AddComponent<EnemyProjectile>();
            p._velocity = velocity;
            p._damage = damage;
            p._sanityDamage = sanityDamage;
            p._lifetime = lifetime;
            p._blockMask = blockMask;
            p._turnRate = turnRate;
            if (breakable)
            {
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0) sr.gameObject.layer = enemyLayer;
                var col = sr.gameObject.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.5f;
            }
            return p;
        }

        /// <summary>追蹤轉向、移動、檢查撞牆與命中玩家。</summary>
        private void Update()
        {
            if (!GameFlow.IsPlaying) return;
            float dt = Time.deltaTime;
            _lifetime -= dt;
            var player = Player.Instance;

            if (_turnRate > 0f && HazardSprites.CanHit(player))
            {
                Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
                float angle = Vector2.SignedAngle(_velocity, toPlayer);
                float turn = Mathf.Clamp(angle, -_turnRate * dt, _turnRate * dt);
                _velocity = Quaternion.Euler(0f, 0f, turn) * _velocity;
            }

            Vector2 pos = transform.position;
            Vector2 step = _velocity * dt;
            if (_lifetime <= 0f || Physics2D.Raycast(pos, step.normalized, step.magnitude, _blockMask).collider != null)
            {
                Destroy(gameObject);
                return;
            }
            transform.position = pos + step;

            if (HazardSprites.CanHit(player) &&
                Vector2.Distance(player.transform.position, transform.position) <= PlayerHitRadius + transform.localScale.x * 0.5f &&
                HazardSprites.HitPlayer(player, _damage, _sanityDamage))
            {
                Destroy(gameObject);
            }
        }

        /// <summary>被魚叉命中：彈幕破掉。</summary>
        public bool TakeHit()
        {
            if (_dead) return false;
            _dead = true;
            Destroy(gameObject);
            return true;
        }
    }

    /// <summary>
    /// 預告式範圍攻擊（觸手地刺、深淵之眼光束、Boss 觸手掃地 / 落雷）。
    /// 預告：中心預測線 + 兩側邊線 + 由中線往兩側擴張的柔光帶，越接近發動閃得越快；
    /// 發動：有特效時只留淡淡餘光，沒有特效時柔光帶變亮成判定區（可沿長邊掃過）。
    /// </summary>
    public class TelegraphStrike : MonoBehaviour
    {
        /// <summary>玩家判定半徑。</summary>
        private const float PlayerRadius = 0.4f;
        /// <summary>預測線 / 邊線粗細。</summary>
        private const float LineThickness = 0.05f;
        /// <summary>預告結束前開始快閃的比例。</summary>
        private const float UrgentRatio = 0.7f;

        /// <summary>柔光帶（預告擴張、發動時為判定區）。</summary>
        private SpriteRenderer _band;
        /// <summary>中心預測線。</summary>
        private SpriteRenderer _coreLine;
        /// <summary>兩側邊線。</summary>
        private SpriteRenderer[] _edges;
        /// <summary>主色。</summary>
        private Color _color;
        /// <summary>預告總秒數。</summary>
        private float _warnTime;
        /// <summary>預告已進行秒數。</summary>
        private float _warnTimer;
        /// <summary>判定總秒數。</summary>
        private float _activeTime;
        /// <summary>判定已進行秒數。</summary>
        private float _activeTimer;
        /// <summary>傷害（扣 HP）。</summary>
        private float _damage;
        /// <summary>額外 SAN 傷害。</summary>
        private float _sanityDamage;
        /// <summary>整個範圍大小（寬 = 局部 X、高 = 局部 Y）。</summary>
        private Vector2 _size;
        /// <summary>掃擊時判定塊的寬度（0 = 不掃，整區同時判定）。</summary>
        private float _sweepWidth;
        /// <summary>掃擊方向（1 = 往局部 +X、-1 = 往 -X）。</summary>
        private int _sweepDir;
        /// <summary>這次攻擊是否已命中（每次攻擊只扣一次）。</summary>
        private bool _hasHit;
        /// <summary>範圍中心。</summary>
        private Vector2 _center;
        /// <summary>旋轉角度（度）。</summary>
        private float _angle;
        /// <summary>外觀用：長邊長度。</summary>
        private float _length;
        /// <summary>外觀用：短邊寬度。</summary>
        private float _width;
        /// <summary>外觀用：長邊是否為局部 Y（直柱；外觀根物件會轉 90 度，讓長邊一律沿外觀局部 X）。</summary>
        private bool _longIsY;
        /// <summary>判定開始時播放的特效 Prefab（可空，例如落雷的閃電）。</summary>
        private GameObject _fxPrefab;
        /// <summary>特效寬度（長度 = 範圍長邊）。</summary>
        private float _fxWidth;
        /// <summary>特效存在秒數。</summary>
        private float _fxLifetime;
        /// <summary>特效是否已播放。</summary>
        private bool _fxPlayed;
        /// <summary>地刺外觀（WithSpikes 設定後才有）。</summary>
        private SpriteRenderer[] _spikes;
        /// <summary>各地刺的完整高度。</summary>
        private float[] _spikeHeights;
        /// <summary>地刺收回秒數（判定結束後播放，不造成傷害）。</summary>
        private float _retractTime;
        /// <summary>收回已進行秒數。</summary>
        private float _retractTimer;

        /// <summary>地刺冒出時間（佔判定時間的比例）。</summary>
        private const float SpikeRiseRatio = 0.35f;
        /// <summary>相鄰地刺冒出的時間差（秒）。</summary>
        private const float SpikeStagger = 0.04f;

        /// <summary>
        /// 產生一次預告攻擊：center / size / angle 決定範圍；warn = 預告秒數；active = 判定秒數；
        /// sweepWidth &gt; 0 時判定塊沿局部 X 掃過整個範圍（sweepDir 決定方向）。
        /// </summary>
        public static TelegraphStrike Spawn(Vector2 center, Vector2 size, float angle, Color color, float warn, float active,
            float damage, float sanityDamage, float sweepWidth = 0f, int sweepDir = 1)
        {
            var go = new GameObject("TelegraphStrike");
            var s = go.AddComponent<TelegraphStrike>();
            s._color = color;
            s._warnTime = Mathf.Max(0.01f, warn);
            s._activeTime = Mathf.Max(0.01f, active);
            s._damage = damage;
            s._sanityDamage = sanityDamage;
            s._size = size;
            s._sweepWidth = sweepWidth;
            s._sweepDir = sweepDir >= 0 ? 1 : -1;
            s._center = center;
            s._angle = angle;
            s._longIsY = size.y > size.x;
            s._length = s._longIsY ? size.y : size.x;
            s._width = s._longIsY ? size.x : size.y;

            // 外觀根物件：長邊一律沿局部 X
            go.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, angle + (s._longIsY ? 90f : 0f)));
            s._band = s.MakePart("Band", HazardSprites.SoftBand, 18);
            s._coreLine = s.MakePart("CoreLine", HazardSprites.Square, 19);
            s._edges = new[] { s.MakePart("EdgeA", HazardSprites.Square, 19), s.MakePart("EdgeB", HazardSprites.Square, 19) };
            s.UpdateWarnVisual(0f);
            return s;
        }

        /// <summary>
        /// 設定判定開始時要播放的特效（直向 Quad 特效沿範圍長邊拉伸：寬 fxWidth × 長邊長度，存在 lifetime 秒；有特效時判定區只留餘光）。
        /// </summary>
        public TelegraphStrike WithFx(GameObject fxPrefab, float fxWidth, float lifetime)
        {
            _fxPrefab = fxPrefab;
            _fxWidth = fxWidth;
            _fxLifetime = lifetime;
            return this;
        }

        /// <summary>
        /// 改用地刺外觀（只適用直立、不旋轉的範圍）：發動時 count 根尖刺從範圍底部依序彈出（中間最高），
        /// 判定結束後花 retract 秒縮回地面。
        /// </summary>
        public TelegraphStrike WithSpikes(int count, Color color, float retract = 0.2f)
        {
            count = Mathf.Max(1, count);
            _retractTime = retract;
            _spikes = new SpriteRenderer[count];
            _spikeHeights = new float[count];
            var root = new GameObject("Spikes").transform;
            root.SetParent(transform, false);
            // 外觀根物件為直柱時轉了 90 度，地刺放在獨立的世界方向（底部中央、不旋轉）
            root.SetPositionAndRotation(_center - new Vector2(0f, _size.y / 2f), Quaternion.identity);
            float spacing = _size.x / count;
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : i / (float)(count - 1) * 2f - 1f;        // -1 ~ 1
                _spikeHeights[i] = _size.y * Mathf.Lerp(1f, 0.65f, Mathf.Abs(t));     // 中間最高
                var go = new GameObject($"Spike{i}");
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(t * (_size.x - spacing) / 2f, 0f, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = HazardSprites.Spike;
                sr.color = color;
                sr.sortingOrder = 20;
                _spikes[i] = sr;
                go.transform.localScale = new Vector3(spacing * 1.25f * 2f, 0f, 1f); // 圖本身寬 0.5，×2 換算成單位寬
            }
            return this;
        }

        /// <summary>地刺高度：第 i 根在 elapsed 秒時的高度比例（彈出時略為超出再回彈）。</summary>
        private float SpikeRise(int i, float elapsed)
        {
            float delay = Mathf.Abs(i - (_spikes.Length - 1) / 2f) * SpikeStagger; // 從中間往兩側
            float rise = Mathf.Max(0.01f, _activeTime * SpikeRiseRatio);
            float t = Mathf.Clamp01((elapsed - delay) / rise);
            return t < 1f ? Mathf.Sin(t * Mathf.PI * 0.65f) / Mathf.Sin(Mathf.PI * 0.65f) : 1f; // 0 → 約 1.12 → 1 的回彈曲線
        }

        /// <summary>設定所有地刺的高度比例。</summary>
        private void SetSpikes(System.Func<int, float> ratio)
        {
            for (int i = 0; i < _spikes.Length; i++)
            {
                var t = _spikes[i].transform;
                t.localScale = new Vector3(t.localScale.x, _spikeHeights[i] * ratio(i), 1f);
            }
        }

        /// <summary>建立一個外觀子物件。</summary>
        private SpriteRenderer MakePart(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>預告 → 判定（可掃擊）→ 結束銷毀。</summary>
        private void Update()
        {
            if (!GameFlow.IsPlaying) return;
            float dt = Time.deltaTime;

            if (_warnTimer < _warnTime)
            {
                _warnTimer += dt;
                UpdateWarnVisual(Mathf.Clamp01(_warnTimer / _warnTime));
                return;
            }

            if (_activeTimer >= _activeTime)
            {
                // 判定結束：有地刺時先收回再銷毀
                _retractTimer += dt;
                if (_spikes != null && _retractTimer < _retractTime)
                {
                    float r = 1f - Mathf.Clamp01(_retractTimer / _retractTime);
                    SetSpikes(_ => r * r);
                    _band.enabled = false;
                    return;
                }
                Destroy(gameObject);
                return;
            }

            _activeTimer += dt;
            float k = Mathf.Clamp01(_activeTimer / _activeTime);
            if (!_fxPlayed) PlayFx();
            if (_spikes != null) SetSpikes(i => SpikeRise(i, _activeTimer));

            // 判定區（掃擊時只有一塊沿長邊移動）
            Vector2 hitSize = _size;
            float localX = 0f;
            if (_sweepWidth > 0f)
            {
                hitSize.x = Mathf.Min(_sweepWidth, _size.x);
                float travel = (_size.x - hitSize.x) / 2f;
                localX = Mathf.Lerp(-travel, travel, _sweepDir > 0 ? k : 1f - k);
            }
            var rot = Quaternion.Euler(0f, 0f, _angle);
            Vector2 hitCenter = _center + (Vector2)(rot * new Vector3(localX, 0f, 0f));
            UpdateActiveVisual(k, hitSize, localX);

            var player = Player.Instance;
            if (!_hasHit && HazardSprites.CanHit(player) && Contains(hitCenter, hitSize, rot, player.transform.position))
            {
                _hasHit = HazardSprites.HitPlayer(player, _damage, _sanityDamage);
            }
        }

        /// <summary>預告外觀：柔光帶由中線往兩側擴張、邊線漸亮，最後 30% 快速閃爍。</summary>
        private void UpdateWarnVisual(float t)
        {
            float ease = 1f - (1f - t) * (1f - t);
            float blinkSpeed = t < UrgentRatio ? 8f : 28f;
            float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * blinkSpeed);

            SetPart(_band, Vector2.zero, new Vector2(_length, _width * Mathf.Lerp(0.15f, 1f, ease)), Mathf.Lerp(0.12f, 0.35f, t) * (0.7f + 0.3f * blink));
            SetPart(_coreLine, Vector2.zero, new Vector2(_length, LineThickness), Mathf.Lerp(0.35f, 0.85f, t) * (0.6f + 0.4f * blink));
            for (int i = 0; i < 2; i++)
            {
                float y = (i == 0 ? 0.5f : -0.5f) * _width;
                SetPart(_edges[i], new Vector2(0f, y), new Vector2(_length, LineThickness), Mathf.Lerp(0.1f, 0.9f, ease) * (0.6f + 0.4f * blink));
            }
        }

        /// <summary>發動外觀：有特效時只留淡淡餘光；沒有特效時柔光帶變亮成判定區（掃擊時跟著判定塊移動）。</summary>
        private void UpdateActiveVisual(float k, Vector2 hitSize, float localX)
        {
            _coreLine.enabled = false;
            foreach (var e in _edges) e.enabled = false;

            // hitSize / localX 是攻擊局部座標（X 沿 _angle）；外觀根物件在直柱時多轉了 90 度，要換軸
            float hitLong = _longIsY ? hitSize.y : hitSize.x;
            float hitWide = _longIsY ? hitSize.x : hitSize.y;
            Vector2 offset = _longIsY ? new Vector2(0f, -localX) : new Vector2(localX, 0f);
            // 有特效或地刺時只留淡淡餘光，否則柔光帶就是判定區
            float alpha = _fxPrefab != null || _spikes != null ? Mathf.Lerp(0.3f, 0f, k) : Mathf.Lerp(0.95f, 0.45f, k);
            SetPart(_band, offset, new Vector2(hitLong, hitWide * 1.15f), alpha);
        }

        /// <summary>設定外觀子物件的位置、大小（局部，長邊沿 X）與透明度。</summary>
        private void SetPart(SpriteRenderer part, Vector2 localPos, Vector2 size, float alpha)
        {
            part.transform.localPosition = localPos;
            part.transform.localScale = new Vector3(size.x, size.y, 1f);
            part.color = new Color(_color.r, _color.g, _color.b, Mathf.Clamp01(alpha));
        }

        /// <summary>
        /// 在範圍中心生成特效（獨立物件，時間到自行銷毀；移除特效自帶的 3D 碰撞）。
        /// 特效圖是直向的：範圍長邊是局部 X（例如光束）時轉 -90 度，讓特效沿長邊延伸。
        /// </summary>
        private void PlayFx()
        {
            _fxPlayed = true;
            if (_fxPrefab == null) return;
            bool alongX = _size.x > _size.y;
            float length = alongX ? _size.x : _size.y;
            var fx = Instantiate(_fxPrefab, new Vector3(_center.x, _center.y, 0f), Quaternion.Euler(0f, 0f, _angle - (alongX ? 90f : 0f)));
            fx.transform.localScale = new Vector3(_fxWidth, length, 1f);
            foreach (var col in fx.GetComponentsInChildren<Collider>()) Destroy(col);
            foreach (var r in fx.GetComponentsInChildren<Renderer>()) r.sortingOrder = 21;
            Destroy(fx, _fxLifetime);
        }

        /// <summary>點（加上玩家半徑）是否在旋轉矩形內。</summary>
        private static bool Contains(Vector2 center, Vector2 size, Quaternion rot, Vector2 point)
        {
            Vector2 local = Quaternion.Inverse(rot) * (point - center);
            return Mathf.Abs(local.x) <= size.x / 2f + PlayerRadius && Mathf.Abs(local.y) <= size.y / 2f + PlayerRadius;
        }
    }
}
