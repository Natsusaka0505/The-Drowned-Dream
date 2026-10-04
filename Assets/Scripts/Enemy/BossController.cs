using System.Collections;
using UnityEngine;

namespace DrownedDream
{
    /// <summary>Boss 攻擊種類。</summary>
    public enum BossAttack
    {
        /// <summary>扇形彈幕。</summary>
        Volley,
        /// <summary>咆哮：畫面震動並扣房內玩家 SAN。</summary>
        Roar,
        /// <summary>慢速追蹤彈。</summary>
        Homing,
        /// <summary>觸手掃地：預告後判定塊沿地面掃過整個房間，要跳起來躲。</summary>
        TentacleSweep,
        /// <summary>落雷：在玩家附近預告數道直柱後落下。</summary>
        Lightning,
    }

    /// <summary>
    /// Boss（F-BOSS）。由 BossArea 啟動 / 停止；玩家在 Boss 房時依 _attackOrder 輪流使用各種攻擊。
    /// 第一次啟動後進入「全圖攻擊」：玩家不在 Boss 房時，定時在玩家附近落雷 / 放追蹤彈（只扣 HP，不扣 SAN），封印才停（F-BOSS-11）。
    /// [待確認] 魚槍能否傷害 Boss（預設不行）。
    /// </summary>
    [RequireComponent(typeof(FearSource))]
    public class BossController : MonoBehaviour, IDamageable
    {
        /// <summary>外觀 Renderer。</summary>
        [SerializeField] private SpriteRenderer _renderer;
        /// <summary>會擋下彈幕的 Layer。</summary>
        [SerializeField] private LayerMask _projectileBlockMask;
        /// <summary>憋氣隱形時 Boss 是否也看不到玩家。</summary>
        [SerializeField] private bool _respectStealth = true;

        [Header("攻擊輪替")]
        /// <summary>攻擊順序（依序輪流，用完從頭開始）。</summary>
        [SerializeField] private BossAttack[] _attackOrder =
        {
            BossAttack.Volley, BossAttack.TentacleSweep, BossAttack.Homing, BossAttack.Lightning, BossAttack.Roar,
        };
        /// <summary>兩次攻擊之間的間隔秒數（從上一招結束算起）。</summary>
        [SerializeField] private float _attackInterval = 2f;
        /// <summary>攻擊傷害（扣 HP；VitalsConfig 設定固定比例時以比例為準）。</summary>
        [SerializeField] private float _damage = 25f;

        [Header("扇形彈幕")]
        /// <summary>每波彈幕數量。</summary>
        [SerializeField] private int _volleyCount = 3;
        /// <summary>彈幕擴散角度。</summary>
        [SerializeField] private float _volleySpread = 20f;
        /// <summary>彈幕速度。</summary>
        [SerializeField] private float _volleySpeed = 5f;
        /// <summary>彈幕存活秒數。</summary>
        [SerializeField] private float _projectileLifetime = 6f;

        [Header("咆哮")]
        /// <summary>咆哮前蓄力秒數（Boss 閃爍）。</summary>
        [SerializeField] private float _roarWindup = 0.6f;
        /// <summary>咆哮扣 SAN 量（玩家在 Boss 房內且未隱形）。</summary>
        [SerializeField] private float _roarSanityDamage = 20f;
        /// <summary>咆哮畫面震動幅度。</summary>
        [SerializeField] private float _roarShake = 0.35f;
        /// <summary>咆哮畫面震動秒數。</summary>
        [SerializeField] private float _roarShakeTime = 0.8f;

        [Header("追蹤彈")]
        /// <summary>每次發射數量。</summary>
        [SerializeField] private int _homingCount = 2;
        /// <summary>追蹤彈速度。</summary>
        [SerializeField] private float _homingSpeed = 3.5f;
        /// <summary>追蹤彈轉向速度（度 / 秒）。</summary>
        [SerializeField] private float _homingTurnRate = 100f;
        /// <summary>追蹤彈存活秒數（從畫面外飛進來，要夠久）。</summary>
        [SerializeField] private float _homingLifetime = 8f;

        [Header("觸手掃地")]
        /// <summary>掃地預告秒數。</summary>
        [SerializeField] private float _sweepWarn = 1f;
        /// <summary>掃過整個房間的秒數。</summary>
        [SerializeField] private float _sweepTime = 0.9f;
        /// <summary>掃地判定高度（低於跳躍高度才躲得掉）。</summary>
        [SerializeField] private float _sweepHeight = 1.2f;
        /// <summary>掃地判定塊寬度。</summary>
        [SerializeField] private float _sweepWidth = 2.5f;

        [Header("落雷")]
        /// <summary>每次落雷道數。</summary>
        [SerializeField] private int _lightningCount = 3;
        /// <summary>落雷之間的水平間距。</summary>
        [SerializeField] private float _lightningSpacing = 3f;
        /// <summary>落雷寬度。</summary>
        [SerializeField] private float _lightningWidth = 1.4f;
        /// <summary>落雷預告秒數。</summary>
        [SerializeField] private float _lightningWarn = 0.9f;
        /// <summary>落雷判定秒數。</summary>
        [SerializeField] private float _lightningActive = 0.25f;
        /// <summary>每道落雷出現的間隔秒數。</summary>
        [SerializeField] private float _lightningStagger = 0.2f;
        /// <summary>落雷特效（FX Lightning II 的 fx_lightning_02；空 = 只顯示色塊）。</summary>
        [SerializeField] private GameObject _lightningFx;
        /// <summary>落雷特效寬度（閃電圖只佔畫格約 1/4 寬，所以比判定寬）。</summary>
        [SerializeField] private float _lightningFxWidth = 4f;
        /// <summary>落雷特效存在秒數。</summary>
        [SerializeField] private float _lightningFxTime = 0.5f;

        [Header("全圖攻擊（玩家離開 Boss 房後）")]
        /// <summary>全圖攻擊間隔秒數（0 = 關閉）。</summary>
        [SerializeField] private float _globalInterval = 5f;
        /// <summary>全圖落雷道數。</summary>
        [SerializeField] private int _globalLightningCount = 2;
        /// <summary>全圖追蹤彈數量（從玩家上方兩側出現，穿牆）。</summary>
        [SerializeField] private int _globalHomingCount = 2;
        /// <summary>沒有攝影機時，追蹤彈出現位置相對玩家的距離。</summary>
        [SerializeField] private float _globalHomingSpawnDistance = 7f;
        /// <summary>追蹤彈出現在畫面外多遠（單位）。</summary>
        [SerializeField] private float _offscreenMargin = 1.5f;

        [Header("[待確認] 魚槍傷害")]
        /// <summary>魚槍能否傷害 Boss。</summary>
        [SerializeField] private bool _canBeDamaged;
        /// <summary>被攻擊幾次死亡（可受傷時才有意義）。</summary>
        [SerializeField] private int _maxHits = 30;
        /// <summary>Boss 腳底離房間地面的高度（站在浮岩上時 &gt; 0；掃地 / 落雷仍以房間地面為準）。</summary>
        [SerializeField] private float _hoverHeight;

        /// <summary>所在的 Boss 房（掃地 / 落雷範圍、咆哮判定）。</summary>
        private Room _room;
        /// <summary>碰撞框（取得腳底高度）。</summary>
        private Collider2D _collider;
        /// <summary>下一招使用的索引。</summary>
        private int _attackIndex;
        /// <summary>距離下一招的秒數。</summary>
        private float _attackTimer;
        /// <summary>是否正在出招（出招中不倒數）。</summary>
        private bool _attacking;
        /// <summary>出招前蓄力中（加強閃爍）。</summary>
        private bool _charging;
        /// <summary>是否已被喚醒（第一次啟動後開始全圖攻擊，封印才停）。</summary>
        private bool _awakened;
        /// <summary>距離下一次全圖攻擊的秒數。</summary>
        private float _globalTimer;
        /// <summary>全圖攻擊下一招是否用落雷（與追蹤彈交替）。</summary>
        private bool _globalUseLightning = true;
        /// <summary>被攻擊次數。</summary>
        private int _hitCount;
        /// <summary>原始顏色。</summary>
        private Color _baseColor;

        /// <summary>場上的 Boss（小地圖標示用）。</summary>
        public static BossController Instance { get; private set; }
        /// <summary>Boss 判定範圍（世界座標）。</summary>
        public Bounds Bounds => _collider != null ? _collider.bounds : new Bounds(transform.position, Vector3.one);
        /// <summary>是否啟動中（攻擊玩家）。</summary>
        public bool IsActive { get; private set; }
        /// <summary>是否已被封印。</summary>
        public bool IsSealed { get; private set; }
        /// <summary>是否還活著（封印後視為死亡）。</summary>
        public bool IsAlive => !IsSealed && _hitCount < _maxHits;

        /// <summary>記錄原始顏色與碰撞框。</summary>
        private void Awake()
        {
            if (_renderer != null) _baseColor = _renderer.color;
            _collider = GetComponent<Collider2D>();
            Instance = this;
        }

        /// <summary>清除場上 Boss 參照。</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>找出所在 Boss 房（Room 在 OnEnable 註冊，因此放在 Start）。</summary>
        private void Start() => _room = Room.FindAt(transform.position);

        /// <summary>啟動 Boss（玩家進 Boss 房）；第一次啟動同時喚醒全圖攻擊。</summary>
        public void Activate()
        {
            if (IsSealed || IsActive) return;
            IsActive = true;
            _attackTimer = _attackInterval;
            if (!_awakened) _globalTimer = _globalInterval;
            _awakened = true;
            Debug.Log("[Boss] 啟動，開始攻擊");
            GameEvents.RaiseBossActivated();
        }

        /// <summary>停止 Boss（玩家離房），中斷正在出的招。</summary>
        public void Deactivate()
        {
            if (IsActive) Debug.Log("[Boss] 停止（玩家離開 Boss 房）");
            IsActive = false;
            StopAttack();
        }

        /// <summary>封印 Boss。</summary>
        public void Seal()
        {
            IsSealed = true;
            IsActive = false;
            StopAttack();
            GetComponent<FearSource>().enabled = false;
            if (_renderer != null) _renderer.color = new Color(0.3f, 0.3f, 0.35f, 0.6f);
        }

        /// <summary>啟動時閃爍，定時依序出招。</summary>
        private void Update()
        {
            if (_renderer != null && !IsSealed)
            {
                float speed = _charging ? 20f : 6f;
                float pulse = IsActive ? 0.5f + 0.5f * Mathf.Sin(Time.time * speed) : 0f;
                _renderer.color = Color.Lerp(_baseColor, Color.red, pulse * (_charging ? 0.9f : 0.5f));
            }

            if (IsSealed || !GameFlow.IsPlaying) return;
            var player = Player.Instance;
            if (player == null || !player.Status.IsAlive) return;
            if (_respectStealth && !player.IsVisibleToEnemies) return;

            if (!IsActive)
            {
                UpdateGlobalAttack(player);
                return;
            }
            if (_attacking) return;

            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f || _attackOrder == null || _attackOrder.Length == 0) return;

            var attack = _attackOrder[_attackIndex % _attackOrder.Length];
            _attackIndex++;
            StartCoroutine(AttackRoutine(attack, player));
        }

        /// <summary>全圖攻擊：喚醒後玩家不在 Boss 房時，定時在玩家附近交替落雷 / 追蹤彈（只扣 HP）。</summary>
        private void UpdateGlobalAttack(Player player)
        {
            if (!_awakened || _globalInterval <= 0f) return;
            _globalTimer -= Time.deltaTime;
            if (_globalTimer > 0f) return;
            _globalTimer = _globalInterval;

            if (_globalUseLightning) StartCoroutine(GlobalLightning(player));
            else GlobalHoming(player);
            _globalUseLightning = !_globalUseLightning;
        }

        /// <summary>全圖落雷：在玩家所在區塊、以玩家為中心預告直柱後落下。</summary>
        private IEnumerator GlobalLightning(Player player)
        {
            var room = Room.FindAt(player.transform.position);
            if (room == null) yield break;
            Debug.Log("[Boss] 全圖攻擊：落雷");
            var b = room.Bounds;
            float baseX = player.transform.position.x;
            int count = Mathf.Max(1, _globalLightningCount);
            for (int i = 0; i < count; i++)
            {
                int step = (i + 1) / 2 * (i % 2 == 1 ? 1 : -1);
                float x = Mathf.Clamp(baseX + step * _lightningSpacing, b.min.x + _lightningWidth, b.max.x - _lightningWidth);
                TelegraphStrike.Spawn(new Vector2(x, b.center.y), new Vector2(_lightningWidth, b.size.y), 0f, new Color(1f, 1f, 0.6f),
                    _lightningWarn, _lightningActive, _damage, 0f).WithFx(_lightningFx, _lightningFxWidth, _lightningFxTime);
                yield return new WaitForSeconds(_lightningStagger);
            }
        }

        /// <summary>全圖追蹤彈：從玩家畫面外出現，可穿牆追向玩家。</summary>
        private void GlobalHoming(Player player)
        {
            Debug.Log("[Boss] 全圖攻擊：追蹤彈");
            SpawnOffscreenHoming(player.transform.position, _globalHomingCount);
        }

        /// <summary>中斷出招並重置狀態。</summary>
        private void StopAttack()
        {
            StopAllCoroutines();
            _attacking = false;
            _charging = false;
        }

        /// <summary>執行一招，結束後開始倒數下一招。</summary>
        private IEnumerator AttackRoutine(BossAttack attack, Player player)
        {
            _attacking = true;
            Debug.Log($"[Boss] 出招：{attack}");
            switch (attack)
            {
                case BossAttack.Volley: FireVolley(player.transform.position); break;
                case BossAttack.Homing: FireHoming(); break;
                case BossAttack.TentacleSweep: TentacleSweep(); break;
                case BossAttack.Lightning: yield return Lightning(player); break;
                case BossAttack.Roar: yield return Roar(player); break;
            }
            _attacking = false;
            _attackTimer = _attackInterval;
        }

        /// <summary>朝目標發射一波扇形彈幕。</summary>
        private void FireVolley(Vector2 target)
        {
            Vector2 origin = transform.position;
            Vector2 baseDir = (target - origin).normalized;
            int count = Mathf.Max(1, _volleyCount);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : (i / (float)(count - 1) - 0.5f);
                var dir = (Vector2)(Quaternion.Euler(0f, 0f, t * _volleySpread) * baseDir);
                EnemyProjectile.Spawn(origin, dir * _volleySpeed, 0.5f, new Color(0.8f, 0.2f, 1f), _damage, 0f, _projectileLifetime, _projectileBlockMask);
            }
        }

        /// <summary>追蹤彈：從玩家畫面外（左上 / 右上方向）出現，朝玩家慢慢轉向追來（可穿牆）。</summary>
        private void FireHoming()
        {
            var player = Player.Instance;
            if (player != null) SpawnOffscreenHoming(player.transform.position, _homingCount);
        }

        /// <summary>在畫面外（以玩家為中心的左上 ~ 右上扇形方向，剛好出畫面邊緣）放出追蹤彈，初速朝向玩家。</summary>
        private void SpawnOffscreenHoming(Vector2 target, int count)
        {
            count = Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                float side = count == 1 ? 0f : (i / (float)(count - 1)) * 2f - 1f; // -1 ~ 1
                var dir = new Vector2(side, 0.8f).normalized;
                Vector2 pos = OffscreenPoint(target, dir);
                EnemyProjectile.Spawn(pos, (target - pos).normalized * _homingSpeed, 0.7f, new Color(0.3f, 1f, 0.6f), _damage, 0f,
                    _homingLifetime, 0, _homingTurnRate);
            }
        }

        /// <summary>從 from 沿 dir 往外延伸到攝影機畫面外（多 _offscreenMargin）的位置；沒有攝影機時用固定距離。</summary>
        private Vector2 OffscreenPoint(Vector2 from, Vector2 dir)
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic) return from + dir * _globalHomingSpawnDistance;
            Vector2 center = cam.transform.position;
            float halfH = cam.orthographicSize + _offscreenMargin;
            float halfW = cam.orthographicSize * cam.aspect + _offscreenMargin;
            // 從畫面中心沿 dir 打到外框（加邊距）
            float t = Mathf.Min(Mathf.Abs(dir.x) > 0.001f ? halfW / Mathf.Abs(dir.x) : float.MaxValue,
                                Mathf.Abs(dir.y) > 0.001f ? halfH / Mathf.Abs(dir.y) : float.MaxValue);
            return center + dir * t;
        }

        /// <summary>觸手掃地：預告整條地面，再由 Boss 這側往另一側掃過。</summary>
        private void TentacleSweep()
        {
            if (!TryGetRoomBounds(out var b)) return;
            float floorY = FloorY;
            float left = b.min.x + 0.5f;
            float right = b.max.x - 0.5f;
            var center = new Vector2((left + right) / 2f, floorY + _sweepHeight / 2f);
            int dir = transform.position.x <= center.x ? 1 : -1;
            TelegraphStrike.Spawn(center, new Vector2(right - left, _sweepHeight), 0f, new Color(0.25f, 0.6f, 0.4f),
                _sweepWarn, _sweepTime, _damage, 0f, _sweepWidth, dir);
        }

        /// <summary>落雷：以玩家位置為中心預告數道直柱，依序落下。</summary>
        private IEnumerator Lightning(Player player)
        {
            if (!TryGetRoomBounds(out var b)) yield break;
            float floorY = FloorY;
            float height = b.max.y - floorY;
            float baseX = player.transform.position.x;
            int count = Mathf.Max(1, _lightningCount);
            for (int i = 0; i < count; i++)
            {
                // 0, +1, -1, +2, -2 … 由中間往兩側
                int step = (i + 1) / 2 * (i % 2 == 1 ? 1 : -1);
                float x = Mathf.Clamp(baseX + step * _lightningSpacing, b.min.x + _lightningWidth, b.max.x - _lightningWidth);
                TelegraphStrike.Spawn(new Vector2(x, floorY + height / 2f), new Vector2(_lightningWidth, height), 0f, new Color(1f, 1f, 0.6f),
                    _lightningWarn, _lightningActive, _damage, 0f).WithFx(_lightningFx, _lightningFxWidth, _lightningFxTime);
                yield return new WaitForSeconds(_lightningStagger);
            }
            yield return new WaitForSeconds(_lightningWarn + _lightningActive);
        }

        /// <summary>咆哮：蓄力閃爍 → 畫面震動 → 房內未隱形的玩家扣 SAN。</summary>
        private IEnumerator Roar(Player player)
        {
            _charging = true;
            yield return new WaitForSeconds(_roarWindup);
            _charging = false;

            GameEvents.RaiseBossRoared();
            if (GameCamera.Instance != null) GameCamera.Instance.Shake(_roarShake, _roarShakeTime);
            bool inRoom = _room == null || Room.FindAt(player.transform.position) == _room;
            if (inRoom && HazardSprites.CanHit(player)) player.Status.LoseSanity(_roarSanityDamage);
            yield return new WaitForSeconds(_roarShakeTime);
        }

        /// <summary>房間地面高度（Boss 腳底往下扣掉浮空高度）。</summary>
        private float FloorY => (_collider != null ? _collider.bounds.min.y : transform.position.y) - _hoverHeight;

        /// <summary>取得 Boss 房邊界。</summary>
        private bool TryGetRoomBounds(out Bounds bounds)
        {
            if (_room == null) _room = Room.FindAt(transform.position);
            bounds = _room != null ? _room.Bounds : default;
            return _room != null;
        }

        /// <summary>被魚叉命中（預設無效）。</summary>
        public bool TakeHit()
        {
            if (!_canBeDamaged || !IsAlive) return false;
            _hitCount++;
            return true;
        }
    }
}
