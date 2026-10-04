using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 敵人 Action（docs/core）：自動移動 AI（左右移動）、偵測玩家、攻擊判斷。
    /// 行為依 EnemyData.Behaviour：
    /// Patrol（水母）範圍外隨機遊走、範圍內上下左右追擊 + 蓄力衝刺 + 吐泡泡彈；Stationary（觸手）橫戳 + 地面突刺；Passive（深淵之眼）凝視光束。
    /// 玩家憋氣隱形時不偵測、不追擊、不攻擊（F-ENM-03）。
    /// </summary>
    [RequireComponent(typeof(EnemyStatus), typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour
    {
        /// <summary>AI 狀態。</summary>
        private enum State
        {
            /// <summary>巡邏 / 待機。</summary>
            Patrol,
            /// <summary>追擊玩家。</summary>
            Chase,
            /// <summary>返回出生點。</summary>
            Return,
            /// <summary>攻擊蓄力。</summary>
            Windup,
            /// <summary>攻擊判定中。</summary>
            Attack,
            /// <summary>攻擊後冷卻。</summary>
            Recover,
            /// <summary>魚怪衝刺前蓄力。</summary>
            DashWindup,
            /// <summary>魚怪衝刺中。</summary>
            Dash,
        }

        /// <summary>外觀 Renderer。</summary>
        [SerializeField] private SpriteRenderer _renderer;
        /// <summary>活動範圍左界（世界 X）；左界 ≥ 右界時不限制。巡邏與追擊都不會超出（避免穿牆 / 穿過地面方塊）。</summary>
        [SerializeField] private float _minX;
        /// <summary>活動範圍右界（世界 X）。</summary>
        [SerializeField] private float _maxX;
        /// <summary>地形 Layer（擋彈幕、截斷光束；未設定時用名為 Ground 的 Layer）。</summary>
        [SerializeField] private LayerMask _groundMask;

        /// <summary>敵人數值。</summary>
        private EnemyStatus _status;
        /// <summary>運動學剛體。</summary>
        private Rigidbody2D _body;
        /// <summary>出生點。</summary>
        private Vector2 _home;
        /// <summary>目前狀態。</summary>
        private State _state;
        /// <summary>狀態計時（觸手攻擊用）。</summary>
        private float _stateTimer;
        /// <summary>受擊閃白剩餘秒數。</summary>
        private float _hitFlash;
        /// <summary>原始顏色。</summary>
        private Color _baseColor;
        /// <summary>原始縮放。</summary>
        private Vector3 _baseScale;
        /// <summary>外觀原始局部位置（浮動基準）。</summary>
        private Vector3 _baseVisualPos;
        /// <summary>浮動相位（每隻隨機，避免同步上下）。</summary>
        private float _bobPhase;
        /// <summary>魚怪衝刺冷卻倒數。</summary>
        private float _dashCooldownTimer;
        /// <summary>魚怪衝刺方向（蓄力開始時朝向玩家，單位向量）。</summary>
        private Vector2 _dashVector = Vector2.right;
        /// <summary>隨機遊走的目標點。</summary>
        private Vector2 _wanderTarget;
        /// <summary>遊走抵達後停留的剩餘秒數。</summary>
        private float _wanderPause;
        /// <summary>魚怪泡泡彈倒數。</summary>
        private float _bubbleTimer;
        /// <summary>觸手地刺倒數。</summary>
        private float _spikeTimer;
        /// <summary>深淵之眼光束倒數。</summary>
        private float _beamTimer;

        /// <summary>敵人資料。</summary>
        private EnemyData Data => _status.Data;

        /// <summary>初始化剛體與外觀。</summary>
        private void Awake()
        {
            _status = GetComponent<EnemyStatus>();
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _home = transform.position;
            _wanderTarget = _home;
            if (_renderer != null)
            {
                _baseColor = _renderer.color;
                _baseScale = _renderer.transform.localScale;
                _baseVisualPos = _renderer.transform.localPosition;
            }
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
            if (_groundMask.value == 0) _groundMask = LayerMask.GetMask("Ground");
        }

        /// <summary>訂閱受擊事件。</summary>
        private void OnEnable() => _status.Hit += OnHit;

        /// <summary>取消訂閱受擊事件。</summary>
        private void OnDisable() => _status.Hit -= OnHit;

        /// <summary>依行為類型更新。</summary>
        private void Update()
        {
            UpdateFlash();
            if (!_status.IsAlive || !GameFlow.IsPlaying) return;
            UpdateBob();

            var player = Player.Instance;
            _status.SetPlayerDetected(DetectPlayer(player, _status.DetectRange));
            switch (Data.Behaviour)
            {
                case EnemyBehaviour.Patrol: UpdatePatrol(player); break;
                case EnemyBehaviour.Stationary: UpdateStationary(player); break;
                default: UpdatePassive(player); break;
            }
        }

        /// <summary>偵測玩家：玩家可見且在範圍內。</summary>
        public bool DetectPlayer(Player player, double range)
        {
            if (player == null || !player.IsVisibleToEnemies) return false;
            return Vector2.Distance(player.transform.position, _body.position) <= range;
        }

        /// <summary>攻擊判斷：玩家在範圍內時造成傷害。</summary>
        public void TryAttack(Player player, double range)
        {
            if (!DetectPlayer(player, range)) return;
            player.Status.TakeHit(Data.AttackDamage, _body.position);
        }

        /// <summary>巡游魚怪：左右巡邏 → 偵測後左右追擊 → 失去目標返回。</summary>
        private void UpdatePatrol(Player player)
        {
            switch (_state)
            {
                case State.Patrol:
                case State.Return:
                    // 玩家不在範圍內：在出生點附近隨機遊走（停一下 → 選新目標 → 漂過去）
                    if (_status.PlayerDetected)
                    {
                        _state = State.Chase;
                        break;
                    }
                    _status.SetMoveSpeed(Data.MoveSpeed);
                    if (_wanderPause > 0f)
                    {
                        _wanderPause -= Time.deltaTime;
                        _status.SetMoving(false);
                        break;
                    }
                    if (Move2D(_wanderTarget))
                    {
                        _wanderPause = Random.Range(0.4f, 1.5f);
                        _wanderTarget = PickWanderTarget();
                    }
                    break;

                case State.Chase:
                    // 玩家在範圍內：上下左右都追過去（漂浮怪）
                    if (!DetectPlayer(player, _status.DetectRange * Data.LoseRangeMultiplier))
                    {
                        _state = State.Patrol;
                        _wanderTarget = PickWanderTarget();
                        break;
                    }
                    _status.SetMoveSpeed(Data.ChaseSpeed);
                    Move2D(player.transform.position);
                    UpdateBubbleShot(player);
                    TryStartDash(player);
                    break;

                case State.DashWindup:
                    // 停下閃紅預告衝刺
                    _status.SetMoving(false);
                    _stateTimer -= Time.deltaTime;
                    if (_renderer != null && _hitFlash <= 0f) _renderer.color = Color.Lerp(_baseColor, Color.red, 0.5f + 0.5f * Mathf.Sin(Time.time * 30f));
                    if (_stateTimer <= 0f)
                    {
                        _state = State.Dash;
                        _stateTimer = Data.DashTime;
                    }
                    break;

                case State.Dash:
                    _stateTimer -= Time.deltaTime;
                    _status.SetMoveSpeed(Data.DashSpeed);
                    Move2D(_body.position + _dashVector * 100f); // 朝蓄力時鎖定的方向直衝
                    if (_stateTimer <= 0f)
                    {
                        _state = State.Chase;
                        _dashCooldownTimer = Data.DashCooldown;
                    }
                    break;
            }
            TryAttack(player, _status.AttackRange);
        }

        /// <summary>魚怪：玩家在衝刺距離內且冷卻結束 → 進入蓄力。</summary>
        private void TryStartDash(Player player)
        {
            _dashCooldownTimer -= Time.deltaTime;
            if (_dashCooldownTimer > 0f || Data.DashTime <= 0f || !DetectPlayer(player, Data.DashRange)) return;
            _dashVector = ((Vector2)player.transform.position - _body.position).normalized;
            _state = State.DashWindup;
            _stateTimer = Data.DashWindup;
        }

        /// <summary>魚怪：追擊中定時朝玩家吐一顆慢速泡泡彈（可被魚叉打破）。</summary>
        private void UpdateBubbleShot(Player player)
        {
            if (Data.BubbleInterval <= 0f) return;
            _bubbleTimer -= Time.deltaTime;
            if (_bubbleTimer > 0f) return;
            _bubbleTimer = Data.BubbleInterval;
            Vector2 dir = ((Vector2)player.transform.position - _body.position).normalized;
            EnemyProjectile.Spawn(_body.position + dir * 0.6f, dir * Data.BubbleSpeed, Data.BubbleSize, new Color(0.7f, 0.95f, 1f, 0.85f),
                Data.AttackDamage, 0f, Data.BubbleLifetime, _groundMask, breakable: true,
                frames: Data.BubbleFrames, frameDuration: Data.BubbleFrameDuration, artScale: Data.BubbleArtScale);
        }

        /// <summary>觸手：玩家在偵測範圍內、橫戳範圍外時，定時在玩家腳下預告後冒出地刺。</summary>
        private void UpdateGroundSpike(Player player)
        {
            if (Data.SpikeCooldown <= 0f) return;
            _spikeTimer -= Time.deltaTime;
            if (_spikeTimer > 0f || !_status.PlayerDetected || DetectPlayer(player, _status.AttackRange)) return;
            _spikeTimer = Data.SpikeCooldown;
            float floorY = _body.position.y - Data.Size.y / 2f; // 觸手腳底 = 地面
            var center = new Vector2(player.transform.position.x, floorY + Data.SpikeSize.y / 2f);
            TelegraphStrike.Spawn(center, Data.SpikeSize, 0f, new Color(0.85f, 0.35f, 0.4f), Data.SpikeWindup, Data.SpikeActive, Data.AttackDamage, 0f)
                .WithSpikes(3, new Color(0.75f, 0.7f, 0.65f)); // 骨白色尖刺，預測線為暗紅

        }

        /// <summary>深淵之眼：看到玩家後瞄準一段時間（預告線），再射出直線光束（碰地形截斷），命中額外扣 SAN。</summary>
        private void UpdatePassive(Player player)
        {
            _status.SetMoving(false);
            if (Data.BeamCooldown <= 0f) return;
            _beamTimer -= Time.deltaTime;
            if (_beamTimer > 0f || !_status.PlayerDetected) return;
            _beamTimer = Data.BeamCooldown + Data.BeamAim;

            Vector2 origin = _body.position;
            Vector2 dir = ((Vector2)player.transform.position - origin).normalized;
            float length = Data.BeamLength;
            var wall = Physics2D.Raycast(origin, dir, length, _groundMask);
            if (wall.collider != null) length = wall.distance;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            TelegraphStrike.Spawn(origin + dir * (length / 2f), new Vector2(length, Data.BeamWidth), angle, new Color(1f, 0.85f, 0.3f),
                Data.BeamAim, Data.BeamActive, Data.AttackDamage, Data.BeamSanityDamage).WithFx(Data.BeamFx, Data.BeamFxWidth, Data.BeamFxTime);
        }

        /// <summary>觸手：待機 → 蓄力 → 攻擊 → 冷卻（不移動）；待機時也會用地刺攻擊較遠的玩家。</summary>
        private void UpdateStationary(Player player)
        {
            _status.SetMoving(false);
            _stateTimer -= Time.deltaTime;
            switch (_state)
            {
                case State.Patrol:
                    if (DetectPlayer(player, _status.AttackRange))
                    {
                        _state = State.Windup;
                        _stateTimer = Data.AttackWindup;
                    }
                    else
                    {
                        UpdateGroundSpike(player);
                    }
                    break;

                case State.Windup:
                    if (_stateTimer <= 0f)
                    {
                        _state = State.Attack;
                        _stateTimer = Data.AttackActiveTime;
                    }
                    break;

                case State.Attack:
                    TryAttack(player, _status.AttackRange);
                    if (_stateTimer <= 0f)
                    {
                        _state = State.Recover;
                        _stateTimer = Data.AttackCooldown;
                    }
                    break;

                case State.Recover:
                    if (_stateTimer <= 0f) _state = State.Patrol;
                    break;
            }
            UpdateAttackVisual();
        }

        /// <summary>在出生點附近選一個遊走目標（水平 ±巡邏距離、垂直 -0.5 ~ +1，夾在活動範圍內）。</summary>
        private Vector2 PickWanderTarget()
        {
            float x = _home.x + Random.Range(-Data.PatrolDistance, Data.PatrolDistance);
            if (_maxX > _minX) x = Mathf.Clamp(x, _minX, _maxX);
            float y = _home.y + Random.Range(-0.5f, 1f);
            return new Vector2(x, y);
        }

        /// <summary>
        /// 上下左右移向目標（漂浮怪用），回傳是否已抵達或被擋住。
        /// 垂直方向最低到「腳底貼地」的高度、最高到出生點上方 3 單位；前方有牆 / 平台就改走單一軸，兩軸都擋住視為抵達。
        /// </summary>
        private bool Move2D(Vector2 target)
        {
            if (_maxX > _minX) target.x = Mathf.Clamp(target.x, _minX, _maxX);
            target.y = Mathf.Clamp(target.y, _home.y - Data.HoverHeight, _home.y + 3f);
            Vector2 pos = _body.position;
            float step = (float)_status.MoveSpeed * Time.deltaTime;
            Vector2 next = Vector2.MoveTowards(pos, target, step);
            Vector2 delta = next - pos;
            if (delta.sqrMagnitude < 0.000001f)
            {
                _status.SetMoving(false);
                return true;
            }

            if (Blocked(pos, delta))
            {
                // 試著只走水平或只走垂直（沿牆滑動）
                var dx = new Vector2(delta.x, 0f);
                var dy = new Vector2(0f, delta.y);
                if (dx.sqrMagnitude > 0f && !Blocked(pos, dx)) delta = dx;
                else if (dy.sqrMagnitude > 0f && !Blocked(pos, dy)) delta = dy;
                else
                {
                    _status.SetMoving(false);
                    return true;
                }
            }

            _status.SetMoving(true);
            _body.MovePosition(pos + delta);
            if (_renderer != null && Mathf.Abs(delta.x) > 0.0001f) _renderer.flipX = delta.x < 0f;
            return Vector2.Distance(pos + delta, target) < 0.05f;
        }

        /// <summary>沿 delta 方向移動時，身體前緣是否會撞到地形。</summary>
        private bool Blocked(Vector2 pos, Vector2 delta)
        {
            float extent = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? Data.Size.x / 2f : Data.Size.y / 2f;
            return Physics2D.Raycast(pos, delta.normalized, delta.magnitude + extent, _groundMask).collider != null;
        }

        /// <summary>只沿 X 軸移向目標，回傳是否已抵達。</summary>
        private bool MoveHorizontally(float targetX)
        {
            if (_maxX > _minX) targetX = Mathf.Clamp(targetX, _minX, _maxX); // 限制在活動範圍內
            Vector2 pos = _body.position;
            float step = (float)_status.MoveSpeed * Time.deltaTime;
            float newX = Mathf.MoveTowards(pos.x, targetX, step);
            // 前方有牆 / 平台側面就停下（手擺關卡沒有設活動範圍時避免穿牆），視為抵達讓巡邏折返
            float dx = newX - pos.x;
            if (Mathf.Abs(dx) > 0f && Physics2D.Raycast(pos, new Vector2(Mathf.Sign(dx), 0f), Mathf.Abs(dx) + Data.Size.x / 2f, _groundMask).collider != null)
            {
                _status.SetMoving(false);
                return true;
            }
            bool arrived = Mathf.Abs(newX - targetX) < 0.05f;
            _status.SetMoving(!arrived);
            _body.MovePosition(new Vector2(newX, pos.y));
            if (_renderer != null && Mathf.Abs(targetX - pos.x) > 0.01f) _renderer.flipX = targetX < pos.x;
            return arrived;
        }

        /// <summary>外觀微微上下浮動。</summary>
        private void UpdateBob()
        {
            if (_renderer == null) return;
            float y = Mathf.Sin(Time.time * Data.BobSpeed + _bobPhase) * Data.BobHeight; // 幅度 / 速度在 EnemyData 設定
            _renderer.transform.localPosition = _baseVisualPos + new Vector3(0f, y, 0f);
        }

        /// <summary>受擊時閃白。</summary>
        private void OnHit(int hitCount) => _hitFlash = 0.15f;

        /// <summary>受擊閃白計時。</summary>
        private void UpdateFlash()
        {
            if (_renderer == null) return;
            if (_hitFlash > 0f)
            {
                _hitFlash -= Time.deltaTime;
                _renderer.color = Color.white;
            }
            else if (Data.Behaviour != EnemyBehaviour.Stationary)
            {
                _renderer.color = _baseColor;
            }
        }

        /// <summary>
        /// 觸手（海蝶）攻擊視覺：蓄力變亮；攻擊時朝玩家方向噴出觸鬚（有觸鬚圖時），沒有觸鬚圖則沿用拉長身體。
        /// </summary>
        private void UpdateAttackVisual()
        {
            if (_renderer == null || _hitFlash > 0f) return;
            var t = _renderer.transform;
            bool hasTendril = Data.TendrilSprite != null;
            switch (_state)
            {
                case State.Windup:
                    _renderer.color = Color.Lerp(_baseColor, Color.yellow, 0.6f);
                    t.localScale = _baseScale;
                    SetTendril(0f);
                    break;
                case State.Attack:
                    _renderer.color = Color.Lerp(_baseColor, Color.white, 0.3f);
                    if (hasTendril)
                    {
                        // 攻擊剛開始的 30% 時間內快速伸出到攻擊範圍
                        float elapsed = Data.AttackActiveTime - _stateTimer;
                        SetTendril(Mathf.Clamp01(elapsed / Mathf.Max(0.01f, Data.AttackActiveTime * 0.3f)));
                    }
                    else
                    {
                        float reach = Data.AttackRange * 2f / Mathf.Max(0.01f, Data.Size.x);
                        t.localScale = new Vector3(_baseScale.x * reach, _baseScale.y, 1f);
                    }
                    break;
                case State.Recover:
                    _renderer.color = _baseColor;
                    t.localScale = _baseScale;
                    // 冷卻開始時 0.15 秒縮回
                    SetTendril(Mathf.Clamp01(1f - (Data.AttackCooldown - _stateTimer) / 0.15f));
                    break;
                default:
                    _renderer.color = _baseColor;
                    t.localScale = _baseScale;
                    SetTendril(0f);
                    break;
            }
        }

        /// <summary>觸鬚（執行期建立）。</summary>
        private SpriteRenderer _tendril;
        /// <summary>觸鬚伸出方向（1 右、-1 左），每次開始伸出時朝向玩家。</summary>
        private int _tendrilDir = 1;

        /// <summary>設定觸鬚伸出比例（0 = 收起）；從 0 開始伸出時朝向玩家。</summary>
        private void SetTendril(float ratio)
        {
            if (Data.TendrilSprite == null) return;
            if (_tendril == null)
            {
                var go = new GameObject("Tendril");
                go.transform.SetParent(transform, false);
                _tendril = go.AddComponent<SpriteRenderer>();
                _tendril.sprite = Data.TendrilSprite;
                _tendril.sharedMaterial = _renderer.sharedMaterial;
                _tendril.sortingOrder = _renderer.sortingOrder - 1;
            }

            if (ratio <= 0f)
            {
                _tendril.enabled = false;
                var player = Player.Instance;
                if (player != null) _tendrilDir = player.transform.position.x >= _body.position.x ? 1 : -1;
                return;
            }
            _tendril.enabled = true;
            float spriteLength = Mathf.Max(0.01f, Data.TendrilSprite.bounds.size.x);
            float length = Data.AttackRange * ratio;
            _tendril.transform.localPosition = new Vector3(0f, Data.Size.y * 0.1f, 0f);
            _tendril.transform.localScale = new Vector3(_tendrilDir * length / spriteLength, 0.6f, 1f);
        }

        /// <summary>選取時畫出偵測 / 攻擊 / 巡邏範圍。</summary>
        private void OnDrawGizmosSelected()
        {
            var status = GetComponent<EnemyStatus>();
            if (status == null || status.Data == null) return;
            var data = status.Data;
            Vector3 center = Application.isPlaying ? (Vector3)_home : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.DetectRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.AttackRange);
            if (data.Behaviour == EnemyBehaviour.Patrol)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(center + Vector3.left * data.PatrolDistance, center + Vector3.right * data.PatrolDistance);
            }
        }
    }
}
