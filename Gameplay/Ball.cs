using System.Collections.Generic;
using BounceHeroes.Core;
using BounceHeroes.Data;
using UnityEngine;

namespace BounceHeroes.Gameplay
{
    /// <summary>
    /// 발사되어 벽과 몬스터에 반사되는 볼입니다.
    /// 일정 속도를 유지하며, 고스트 볼은 트리거로 전환되어 몬스터를 관통합니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class Ball : MonoBehaviour
    {
        [SerializeField] private BallVisualController visualController;

        /// <summary>타격 FX 프리팹 조회 등에 사용하는 비주얼 컨트롤러입니다.</summary>
        public BallVisualController VisualController => visualController;

        private const float HitCooldownSeconds = 0.15f;
        private const float RecallSpeedMultiplier = 1.5f;
        private const float CollectSpeedMultiplier = 2f;
        private const float CollectReachDistance = 0.2f;
        private const float MiniWallSpinMin = 360f;
        private const float MiniWallSpinMax = 900f;

        private readonly Dictionary<Monster, float> _lastHitTimes = new Dictionary<Monster, float>();

        private Rigidbody2D _rigidbody;
        private CircleCollider2D _collider;
        private BallLauncher _launcher;
        private float _speed;
        private float _maxFlightTime;
        private float _flightTimer;
        private bool _isRecalling;
        private bool _isCollecting;
        private Transform _collectTarget;
        private Vector3 _baseScale = Vector3.one;
        private RigidbodyConstraints2D _baseConstraints;
        private System.Action _releaseToPool;
        private ICombatService _combat;

        /// <summary>이 볼을 발사한 발사기입니다. 스킬 훅에서 미니볼 스폰 등에 사용합니다.</summary>
        public BallLauncher Launcher => _launcher;

        /// <summary>볼의 충돌 데미지입니다.</summary>
        public int Damage { get; private set; }

        /// <summary>이 볼에 적용된 액티브 스킬입니다. 노멀 볼이면 null입니다.</summary>
        public ActiveSkillData Skill { get; private set; }

        /// <summary>적용된 스킬의 레벨입니다.</summary>
        public int Level { get; private set; }

        /// <summary>클러스터 볼이 생성한 소형 볼인지 여부입니다.</summary>
        public bool IsMini { get; private set; }

        /// <summary>노멀 볼 여부입니다. 소형 볼은 특수 볼로 취급합니다.</summary>
        public bool IsNormalBall => Skill == null && !IsMini;

        /// <summary>타격 전까지 누적된 벽 반사 횟수입니다.</summary>
        public int WallBounceStacks { get; private set; }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();
            _baseScale = transform.localScale;
            _baseConstraints = _rigidbody.constraints;
        }

        /// <summary>
        /// 이 볼을 풀로 되돌리는 방법을 연결합니다. 풀이 인스턴스를 최초 생성할 때 한 번만 호출합니다.
        /// </summary>
        public void BindPool(System.Action releaseToPool)
        {
            _releaseToPool = releaseToPool;
        }

        /// <summary>
        /// 풀에서 꺼낼 때 이전 사용의 잔여 상태를 깨끗이 초기화합니다.
        /// (발사기가 <see cref="SetAsMini"/>/<see cref="Fire"/>를 호출하기 전에 실행됩니다.)
        /// </summary>
        public void ResetState()
        {
            visualController?.CancelMotions();
            transform.localScale = _baseScale;
            transform.rotation = Quaternion.identity;

            IsMini = false;
            WallBounceStacks = 0;
            _flightTimer = 0f;
            _isRecalling = false;
            _isCollecting = false;
            _collectTarget = null;
            _lastHitTimes.Clear();

            _rigidbody.constraints = _baseConstraints;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.linearVelocity = Vector2.zero;
            _collider.isTrigger = false;
        }

        /// <summary>
        /// 볼을 초기화하고 지정한 방향으로 발사합니다.
        /// </summary>
        /// <param name="launcher">볼을 관리하는 발사기</param>
        /// <param name="origin">발사 위치</param>
        /// <param name="direction">발사 방향(정규화 필요 없음)</param>
        /// <param name="skill">적용할 액티브 스킬. 노멀 볼이면 null</param>
        /// <param name="level">스킬 레벨</param>
        /// <param name="damage">충돌 데미지</param>
        /// <param name="speed">이동 속도</param>
        /// <param name="maxFlightTime">자동 회수까지의 시간(초)</param>
        public void Fire(BallLauncher launcher, Vector2 origin, Vector2 direction,
            ActiveSkillData skill, int level, int damage, float speed, float maxFlightTime)
        {
            _launcher = launcher;
            Skill = skill;
            Level = level;
            Damage = damage;
            _speed = speed;
            _maxFlightTime = maxFlightTime;

            transform.position = origin;

            bool pierces = skill != null && !skill.ReflectsOffMonsters;
            _collider.isTrigger = pierces;

            visualController?.Apply();

            _rigidbody.linearVelocity = direction.normalized * speed;
        }

        /// <summary>
        /// 클러스터 볼이 생성하는 소형 볼로 설정합니다.
        /// </summary>
        public void SetAsMini()
        {
            IsMini = true;
            transform.localScale = Vector3.one * 0.55f;

            // 기본 볼 프리팹은 회전이 고정되어 있어(FreezeRotation), 벽 충돌 시 회전 연출을 넣으려면
            // 미니볼만 별도로 회전 제약을 풀어줘야 한다.
            _rigidbody.constraints &= ~RigidbodyConstraints2D.FreezeRotation;
        }

        /// <summary>
        /// 볼의 스프라이트를 교체합니다. 클러스터 볼 파편처럼 여러 겉모습 중 하나를 무작위로 쓸 때 사용합니다.
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            visualController?.SetSprite(sprite);
        }

        /// <summary>
        /// 이 볼의 시각 컨트롤러에 FX 서비스를 주입합니다. 발사기가 볼 생성 직후 호출합니다.
        /// </summary>
        public void SetFXService(IFXService fx)
        {
            visualController?.SetFXService(fx);
        }

        /// <summary>이 볼의 타격 처리에 사용할 전투 서비스를 주입합니다. 발사기가 볼 생성 직후 호출합니다.</summary>
        public void SetCombatService(ICombatService combat)
        {
            _combat = combat;
        }

        /// <summary>
        /// 볼을 강제로 회수 상태로 전환합니다. 아래로 급강하하여 회수 영역으로 향합니다.
        /// 물리 충돌을 트리거로 바꿔, 몬스터나 벽에 끼어 멈추지 않고 그대로 통과해서 내려갑니다.
        /// </summary>
        public void ForceRecall()
        {
            _isRecalling = true;
            _collider.isTrigger = true;
        }

        /// <summary>
        /// 타격 처리 후 벽 반사 스택을 소모합니다. (마법 거울 패시브)
        /// </summary>
        public void ConsumeWallStacks()
        {
            WallBounceStacks = 0;
        }

        private void Update()
        {
            _flightTimer += Time.deltaTime;

            if (!_isCollecting && !_isRecalling && _flightTimer >= _maxFlightTime)
                ForceRecall();
        }

        private void FixedUpdate()
        {
            if (_isCollecting)
            {
                MoveTowardCollectTarget();
                return;
            }

            if (_isRecalling)
            {
                _rigidbody.linearVelocity = Vector2.down * (_speed * RecallSpeedMultiplier);
                return;
            }

            Vector2 velocity = _rigidbody.linearVelocity;

            if (velocity.sqrMagnitude < 0.001f)
                velocity = Vector2.down;

            _rigidbody.linearVelocity = velocity.normalized * _speed;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.CompareTag("Wall"))
            {
                WallBounceStacks++;
                ApplyMiniWallSpin();
                return;
            }

            if (collision.collider.TryGetComponent(out Monster monster))
            {
                ContactPoint2D contact = collision.GetContact(0);
                TryHit(monster, contact.normal, contact.point);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("RecallZone"))
            {
                BeginCollectToCenter();
                return;
            }

            // 회수 중에는 관통 스킬 여부와 관계없이 벽/몬스터와 상호작용하지 않고 그대로 통과한다.
            if (_isCollecting || _isRecalling)
                return;

            if (!_collider.isTrigger)
                return;

            if (other.TryGetComponent(out Wall wall))
            {
                _rigidbody.linearVelocity = Vector2.Reflect(_rigidbody.linearVelocity, wall.Normal);
                WallBounceStacks++;
                ApplyMiniWallSpin();
                return;
            }

            if (other.TryGetComponent(out Monster monster))
                TryHit(monster, null, transform.position);
        }

        /// <summary>
        /// 클러스터 볼 파편(미니볼)이 벽에 튕길 때 무작위 방향/속도로 회전을 겁니다.
        /// </summary>
        private void ApplyMiniWallSpin()
        {
            if (!IsMini)
                return;

            float direction = Random.value < 0.5f ? -1f : 1f;
            _rigidbody.angularVelocity = Random.Range(MiniWallSpinMin, MiniWallSpinMax) * direction;
        }

        private void TryHit(Monster monster, Vector2? contactNormal, Vector2 hitPoint)
        {
            if (monster.IsDead)
                return;

            if (_lastHitTimes.TryGetValue(monster, out float lastTime)
                && Time.time - lastTime < HitCooldownSeconds)
                return;

            _lastHitTimes[monster] = Time.time;
            visualController?.PlayHitBurst(hitPoint, contactNormal);
            _combat?.ResolveHit(this, monster, contactNormal, hitPoint);
        }

        /// <summary>
        /// 회수 영역에 닿은 볼을 플레이어의 Center 지점으로 날아가게 합니다.
        /// 목표 지점이 없으면 그 자리에서 즉시 회수 처리합니다.
        /// </summary>
        private void BeginCollectToCenter()
        {
            if (_isCollecting)
                return;

            _collectTarget = _launcher != null ? _launcher.CollectTarget : null;

            if (_collectTarget == null)
            {
                Collect();
                return;
            }

            _isCollecting = true;
            _collider.isTrigger = true;
        }

        private void MoveTowardCollectTarget()
        {
            Vector2 toTarget = (Vector2)_collectTarget.position - (Vector2)transform.position;

            if (toTarget.sqrMagnitude <= CollectReachDistance * CollectReachDistance)
            {
                Collect();
                return;
            }

            _rigidbody.linearVelocity = toTarget.normalized * (_speed * CollectSpeedMultiplier);
        }

        private void Collect()
        {
            if (_launcher != null)
                _launcher.NotifyBallReturned(this);

            if (_releaseToPool != null)
                _releaseToPool();
            else
                Destroy(gameObject);
        }
    }
}
