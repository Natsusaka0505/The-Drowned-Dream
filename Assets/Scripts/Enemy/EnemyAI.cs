using UnityEngine;

namespace DrownedDream
{
    /// <summary>
    /// 敵人 Action（docs/core）：自動移動 AI（左右移動）、偵測玩家、攻擊判斷。
    /// 行為依 EnemyData.Behaviour：Patrol 左右巡邏 / 追擊；Stationary 固定蓄力攻擊；Passive 不移動不攻擊。
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
        }

        /// <summary>外觀 Renderer。</summary>
        [SerializeField] private SpriteRenderer _renderer;

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
        /// <summary>巡邏方向（1 右、-1 左）。</summary>
        private int _patrolDir = 1;
        /// <summary>受擊閃白剩餘秒數。</summary>
        private float _hitFlash;
        /// <summary>原始顏色。</summary>
        private Color _baseColor;
        /// <summary>原始縮放。</summary>
        private Vector3 _baseScale;

        /// <summary>敵人資料。</summary>
        private EnemyData Data => _status.Data;

        /// <summary>初始化剛體與外觀。</summary>
        private void Awake()
        {
            _status = GetComponent<EnemyStatus>();
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _home = transform.position;
            if (_renderer != null)
            {
                _baseColor = _renderer.color;
                _baseScale = _renderer.transform.localScale;
            }
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

            var player = Player.Instance;
            _status.SetPlayerDetected(DetectPlayer(player, _status.DetectRange));
            switch (Data.Behaviour)
            {
                case EnemyBehaviour.Patrol: UpdatePatrol(player); break;
                case EnemyBehaviour.Stationary: UpdateStationary(player); break;
                default: _status.SetMoving(false); break;
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
            player.Status.TakeHit(Data.AttackDamage);
        }

        /// <summary>巡游魚怪：左右巡邏 → 偵測後左右追擊 → 失去目標返回。</summary>
        private void UpdatePatrol(Player player)
        {
            switch (_state)
            {
                case State.Patrol:
                    if (_status.PlayerDetected)
                    {
                        _state = State.Chase;
                        break;
                    }
                    _status.SetMoveSpeed(Data.MoveSpeed);
                    float targetX = _home.x + _patrolDir * Data.PatrolDistance;
                    if (MoveHorizontally(targetX)) _patrolDir = -_patrolDir;
                    break;

                case State.Chase:
                    if (!DetectPlayer(player, _status.DetectRange * Data.LoseRangeMultiplier))
                    {
                        _state = State.Return;
                        break;
                    }
                    _status.SetMoveSpeed(Data.ChaseSpeed);
                    MoveHorizontally(player.transform.position.x);
                    break;

                case State.Return:
                    if (_status.PlayerDetected)
                    {
                        _state = State.Chase;
                        break;
                    }
                    _status.SetMoveSpeed(Data.MoveSpeed);
                    if (MoveHorizontally(_home.x)) _state = State.Patrol;
                    break;
            }
            TryAttack(player, _status.AttackRange);
        }

        /// <summary>觸手：待機 → 蓄力 → 攻擊 → 冷卻（不移動）。</summary>
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

        /// <summary>只沿 X 軸移向目標，回傳是否已抵達。</summary>
        private bool MoveHorizontally(float targetX)
        {
            Vector2 pos = _body.position;
            float step = (float)_status.MoveSpeed * Time.deltaTime;
            float newX = Mathf.MoveTowards(pos.x, targetX, step);
            bool arrived = Mathf.Abs(newX - targetX) < 0.05f;
            _status.SetMoving(!arrived);
            _body.MovePosition(new Vector2(newX, pos.y));
            if (_renderer != null && Mathf.Abs(targetX - pos.x) > 0.01f) _renderer.flipX = targetX < pos.x;
            return arrived;
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

        /// <summary>觸手原型視覺：蓄力變亮、攻擊時伸長。</summary>
        private void UpdateAttackVisual()
        {
            if (_renderer == null || _hitFlash > 0f) return;
            var t = _renderer.transform;
            switch (_state)
            {
                case State.Windup:
                    _renderer.color = Color.Lerp(_baseColor, Color.yellow, 0.6f);
                    t.localScale = _baseScale;
                    break;
                case State.Attack:
                    _renderer.color = Color.Lerp(_baseColor, Color.white, 0.3f);
                    float reach = Data.AttackRange * 2f / Mathf.Max(0.01f, Data.Size.x);
                    t.localScale = new Vector3(_baseScale.x * reach, _baseScale.y, 1f);
                    break;
                default:
                    _renderer.color = _baseColor;
                    t.localScale = _baseScale;
                    break;
            }
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
