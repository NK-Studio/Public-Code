using BounceHeroes.Core;
using System;
using BounceHeroes.Data;
using BounceHeroes.UI;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace BounceHeroes.Gameplay
{
    /// <summary>
    /// 필드를 따라 아래로 연속(float) 하강하는 몬스터입니다.
    /// 지면 블록 위에 본체가 올라선 구조이며, 본체는 아이들로 살아 움직이고
    /// 그림자는 지면에 남아 높이감을 표현합니다. HP 바 폭은 블록 크기에 맞춰 자동 조절됩니다.
    /// </summary>
    public sealed class Monster : MonoBehaviour
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer blockRenderer;
        [SerializeField] private SpriteRenderer shadowRenderer;
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private Transform gfxRoot;
        [SerializeField] private bool applyDataVisuals = true;

        [Header("HP Bar")]
        [SerializeField] private Transform hpBarRoot;
        [SerializeField] private UIDocument hpBarDocument;
        private VisualElement _hpBarContainer;
        private VisualElement _hpBarBackground;
        private VisualElement _hpBarFill;
        private VisualElement _hpBarDeco;
        [SerializeField] private BoxCollider2D bodyCollider;
        [SerializeField] private bool autoLayoutHpBar = true;

        [Header("Spawn Motion")]
        [SerializeField] private float spawnDropHeight = 1.45f;
        [SerializeField] private float spawnVisualDelay = 0.08f;
        [SerializeField] private float spawnDropDuration = 0.34f;
        [SerializeField] private float spawnSquashDuration = 0.18f;
        [SerializeField] private float shadowSpawnScale = 0.75f;
        [SerializeField] private Vector3 landingPunchScale = new Vector3(0.16f, 0f, 0f);

        [Header("Idle Motion")]
        [SerializeField] private float idleDuration = 1.4f;
        [SerializeField] private float idleScaleX = 1.08f;
        [SerializeField] private float idleScaleY = 0.92f;

        [Header("Hit Reaction")]
        [SerializeField] private float hitFlashDuration = 0.12f;
        [SerializeField] private float hitShakeDuration = 0.28f;
        // LitMotion Punch는 좌우 대칭으로 진동한다. Frequency=1이면 카운터 스윙 없이 한 번 밀렸다
        // 돌아오는 단단한 임팩트가 되고, 값을 올릴수록 좌우로 여러 번 튕기는 말랑한(스프링) 느낌이 된다.
        // Damping은 잦아드는 속도로, 값이 클수록 잔여 흔들림이 빨리 사라진다.
        [SerializeField, Range(1, 12)] private int hitShakeFrequency = 1;
        [SerializeField, Range(0.5f, 3f)] private float hitShakeDamping = 1f;
        [SerializeField] private float hitMoveMin = 0.06f;
        [SerializeField] private float hitMoveMax = 0.14f;
        [SerializeField] private float hitLiftMin = 0.02f;
        [SerializeField] private float hitLiftMax = 0.08f;
        [SerializeField] private float hitShakeAngleMin = 10f;
        [SerializeField] private float hitShakeAngleMax = 20f;
        [SerializeField] private float hitScaleMin = 0.05f;
        [SerializeField] private float hitScaleMax = 0.12f;

        [Header("End of Field Attack")]
        [SerializeField] private float endShakeDuration = 1.5f;
        [SerializeField] private float endShakeStrength = 0.08f;
        [SerializeField] private int endShakeVibrato = 14;
        [SerializeField] private float attackFlyDuration = 0.4f;
        [SerializeField] private Ease attackFlyEase = Ease.InBack;

        private const float ShadowBaseAlpha = 0.41f;
        private const float ShadowLocalY = -0.05f;

        private static readonly Color FrozenTint = new Color(0.55f, 0.85f, 1f);
        private static readonly Color BurnTint = new(1f, 0.44f, 0.31f);

        // 커스텀 히트 틴트 셰이더 프로퍼티. 상태(지속) 틴트와 히트(순간) 플래시를 독립된 채널로 분리해
        // 서로의 트윈 타이밍이 겹쳐도 색이 씹히지 않도록 한다.
        private static readonly int StatusColorId = Shader.PropertyToID("_StatusColor");
        private static readonly int StatusAmountId = Shader.PropertyToID("_StatusAmount");
        private static readonly int HitColorId = Shader.PropertyToID("_HitColor");
        private static readonly int HitAmountId = Shader.PropertyToID("_HitAmount");

        private MonsterData _data;
        private GridField _grid;
        private Transform _bodyTransform;
        private Transform _gfxTransform;
        private GameObject _visualInstance;
        private int _maxHp;
        private int _currentHp;
        private float _descendSpeed;
        private float _bottomOffsetY;
        private float _halfWidth;
        private float _halfHeight;
        private float _bodyRestY;
        private Vector3 _hpBarBaseLocalPosition;
        private Vector3 _hpBarRestLocalPosition;
        private Vector3 _bodyBaseScale = Vector3.one;
        private Vector3 _gfxRestLocalPosition;
        private Vector3 _gfxBaseScale = Vector3.one;
        private Quaternion _gfxBaseRotation = Quaternion.identity;
        private Vector3 _shadowBaseScale = Vector3.one;
        private SpriteRenderer[] _gfxRenderers = Array.Empty<SpriteRenderer>();
        private bool _isSpawning;
        private bool _reachedEnd;

        private float _freezeTimer;
        private float _freezeSlow;
        private float _freezeBonusDamage;

        private int _burnStacks;
        private int _burnTickDamage;
        private float _burnRemaining;
        private float _burnTickTimer;

        private MotionHandle _flashTween;
        private MotionHandle _hitShakeTween;
        private MotionHandle _breatheTween;
        private MotionHandle _spawnTween;
        private MotionHandle _shadowTween;
        private MotionHandle _endShakeTween;
        private MotionHandle _attackFlyTween;
        private readonly CompositeMotionHandle _allMotions = new CompositeMotionHandle();

        private IFXService _fx;
        private IFXHandle _burnHandle;
        private IFXHandle _iceHandle;
        private System.Action _releaseToPool;
        private Transform _attackTarget;

        private MaterialPropertyBlock _propertyBlock;
        private float _hitAmount;

        private void OnEnable()
        {
            // hpBarRoot(GameObject)는 항상 활성 상태로 유지해야 한다. UIDocument가 자식으로 붙어있어,
            // 이 오브젝트를 SetActive로 껐다 켜면 UIDocument가 rootVisualElement를 파괴하고 새로 만들어
            // 여기서 캐싱한 VisualElement 참조가 좀비 참조(할당은 되어 있으나 화면에 반영되지 않음)가 된다.
            // 표시/숨김은 RevealHpBar()에서 style.display로 처리한다.
            var root = hpBarDocument.rootVisualElement;
            _hpBarContainer = root.Q<VisualElement>("monster__hp-bar");
            _hpBarBackground = root.Q<VisualElement>("monster__hp-bar__frame");
            _hpBarFill = root.Q<VisualElement>("monster__hp-bar__fill");
            _hpBarDeco = root.Q<VisualElement>("monster__hp-bar__deco");
        }

        private void Awake()
        {
            // pristine 프리팹 기준값을 한 번만 캡처한다. 사망 연출이 gfx scale/alpha를 0으로 만들기 때문에,
            // Initialize에서 현재 값을 캡처하면 풀 재사용 시 0이 base로 굳어 몬스터가 안 보이는 버그가 생긴다.
            _bodyTransform = bodyRenderer != null ? bodyRenderer.transform : null;
            _gfxTransform = gfxRoot != null ? gfxRoot : _bodyTransform;
            _gfxRestLocalPosition = _gfxTransform != null ? _gfxTransform.localPosition : Vector3.zero;
            _gfxBaseScale = _gfxTransform != null ? _gfxTransform.localScale : Vector3.one;
            _bodyBaseScale = _bodyTransform != null ? _bodyTransform.localScale : Vector3.one;

            if (shadowRenderer != null)
                _shadowBaseScale = shadowRenderer.transform.localScale;

            // 프리팹에 배치된 위치는 1x1(footprintHeight=1) 몬스터 기준값이다.
            // 블록은 pivot이 center라 footprintHeight가 커질수록 center가 위로 올라가므로,
            // SetupHpBarForBlock에서 이 기준값을 anchor 삼아 footprintHeight만큼 보정한다.
            if (hpBarRoot != null)
            {
                _hpBarBaseLocalPosition = hpBarRoot.localPosition;
                _hpBarRestLocalPosition = _hpBarBaseLocalPosition;
            }
        }

        /// <summary>상태이상 FX 풀링에 사용할 FX 서비스를 주입합니다. 스포너가 몬스터 생성 직후 호출합니다.</summary>
        public void SetFXService(IFXService fx)
        {
            _fx = fx;
        }

        /// <summary>하단 도달 시 날아가 공격할 목표(플레이어 Center) 트랜스폼을 지정합니다.</summary>
        public void SetAttackTarget(Transform target)
        {
            _attackTarget = target;
        }

        /// <summary>이 몬스터를 풀로 되돌리는 방법을 연결합니다. 풀이 인스턴스를 최초 생성할 때 한 번만 호출합니다.</summary>
        public void BindPool(System.Action releaseToPool)
        {
            _releaseToPool = releaseToPool;
        }

        private void ReturnToPool()
        {
            if (_releaseToPool != null)
                _releaseToPool();
            else
                Destroy(gameObject);
        }

        /// <summary>사망 또는 하단 도달 여부입니다.</summary>
        public bool IsDead { get; private set; }

        /// <summary>냉동 상태 여부입니다.</summary>
        public bool IsFrozen => _freezeTimer > 0f;

        /// <summary>자수정 단검(전면 치명타) 보너스가 이미 소모되었는지 여부입니다.</summary>
        public bool AmethystConsumed { get; private set; }

        /// <summary>에메랄드 단검(후면 치명타) 보너스가 이미 소모되었는지 여부입니다.</summary>
        public bool EmeraldConsumed { get; private set; }

        /// <summary>하단 도달 시 플레이어에게 주는 피해량입니다.</summary>
        public int AttackDamage => _data.AttackDamage;

        /// <summary>Footprint 기준 가로 폭의 절반(월드 단위)입니다. 같은 열 여부 판정에 사용합니다.</summary>
        public float HalfWidth => _halfWidth;

        /// <summary>Footprint 기준 세로 높이의 절반(월드 단위)입니다. 몬스터 간 정체 간격 계산에 사용합니다.</summary>
        public float HalfHeight => _halfHeight;

        /// <summary>몬스터가 처치되었을 때 발생합니다.</summary>
        public event Action<Monster> Died;

        /// <summary>몬스터가 필드 하단(위험선)에 도달했을 때 발생합니다.</summary>
        public event Action<Monster> ReachedBottom;

        /// <summary>
        /// 몬스터를 초기화하고 스폰 위치에 배치합니다.
        /// </summary>
        /// <param name="data">몬스터 종류 데이터</param>
        /// <param name="hp">시작 체력</param>
        /// <param name="grid">필드</param>
        /// <param name="col">시작 열(가로 레인)</param>
        /// <param name="spawnY">스폰 월드 Y(필드 상단 위)</param>
        /// <param name="descendSpeed">초당 하강 속도(월드 단위)</param>
        /// <param name="rotationDegrees">시계 방향 배치 회전각(0/90/180/270). 짝수 칸이 아닌 footprint도 회전에 맞춰 가로세로가 뒤바뀝니다.</param>
        public void Initialize(MonsterData data, int hp, GridField grid, Vector3 spawnPosition, float descendSpeed, int rotationDegrees = 0)
        {
            // 풀 재사용 대비: 이전 생애의 상태·연출·구독을 완전히 초기화한다.
            ResetRuntimeState();

            _data = data;
            _grid = grid;
            _maxHp = hp;
            _currentHp = hp;
            _descendSpeed = descendSpeed;

            PlacedMonster.GetRotatedFootprint(data, rotationDegrees, out int footprintWidth, out int footprintHeight);
            _bottomOffsetY = (footprintHeight - 1) * grid.CellHeight * 0.5f;
            _halfWidth = footprintWidth * grid.CellWidth * 0.5f;
            _halfHeight = footprintHeight * grid.CellHeight * 0.5f;

            _gfxBaseRotation = Quaternion.Euler(0f, 0f, -rotationDegrees);
            _bodyRestY = data.BodyOffsetY;

            ClearVisualInstance();

            Sprite block = data.BlockSprite != null ? data.BlockSprite : blockRenderer != null ? blockRenderer.sprite : null;
            if (data.VisualPrefab != null)
                BuildVisualFromPrefab(data.VisualPrefab);
            else if (applyDataVisuals)
                ApplyDataVisuals(data, block);

            if (shadowRenderer != null)
            {
                shadowRenderer.sprite = block;
                shadowRenderer.transform.localPosition = new Vector3(0f, ShadowLocalY, 0f);
                shadowRenderer.color = new Color(0f, 0f, 0f, ShadowBaseAlpha);
            }

            SetupHpBarForBlock(grid, footprintWidth, footprintHeight);
            SetupColliderForFootprint(grid, footprintWidth, footprintHeight);
            RefreshGfxRenderers();
            RestoreGfxAlpha();

            // 이전 생애의 상태이상 틴트(냉동/화상 색)가 남지 않도록 셰이더 프로퍼티를 초기화한다.
            UpdateStatusTint();

            if (_gfxTransform != null)
                _gfxTransform.localRotation = _gfxBaseRotation;

            transform.position = spawnPosition;
            UpdateSortingOrder();
            UpdateHpBar();

            // 첫 피격 전까지는 HP 바를 숨겨, 아직 손대지 않은 몬스터인지 한눈에 구분되게 한다.
            // hpBarRoot(GameObject)는 계속 활성 상태로 두고 VisualElement의 display만 끈다.
            // GameObject를 SetActive(false)하면 UIDocument가 rootVisualElement를 재생성해
            // 캐싱해둔 VisualElement 참조가 무효화된다.
            if (_hpBarContainer != null)
                _hpBarContainer.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// 풀에서 재사용될 때 이전 생애의 상태·연출·이벤트 구독을 완전히 초기화합니다.
        /// 트랜스폼은 <see cref="Awake"/>에서 캡처한 pristine 기준값으로 복원합니다.
        /// </summary>
        private void ResetRuntimeState()
        {
            IsDead = false;
            _reachedEnd = false;
            _isSpawning = false;

            _freezeTimer = 0f;
            _freezeSlow = 0f;
            _freezeBonusDamage = 0f;
            _burnStacks = 0;
            _burnTickDamage = 0;
            _burnRemaining = 0f;
            _burnTickTimer = 0f;
            AmethystConsumed = false;
            EmeraldConsumed = false;
            _hitAmount = 0f;

            // 재사용 시 이전 생애의 구독자가 남지 않도록 이벤트를 비운다.
            Died = null;
            ReachedBottom = null;

            _allMotions.Cancel();

            _burnHandle?.Release();
            _burnHandle = null;
            _iceHandle?.Release();
            _iceHandle = null;

            if (bodyCollider != null)
                bodyCollider.enabled = true;

            if (_gfxTransform != null)
            {
                _gfxTransform.localScale = _gfxBaseScale;
                _gfxTransform.localPosition = _gfxRestLocalPosition;
            }

            if (_bodyTransform != null)
                _bodyTransform.localScale = _bodyBaseScale;

            if (shadowRenderer != null)
                shadowRenderer.transform.localScale = _shadowBaseScale;

            if (hpBarRoot != null)
            {
                hpBarRoot.localPosition = _hpBarRestLocalPosition;
                hpBarRoot.localScale = Vector3.one;
            }
        }

        /// <summary>사망 페이드로 0이 된 gfx 렌더러 알파를 다시 1로 복원합니다.</summary>
        private void RestoreGfxAlpha()
        {
            foreach (SpriteRenderer renderer in _gfxRenderers)
            {
                if (renderer == null)
                    continue;

                Color color = renderer.color;
                color.a = 1f;
                renderer.color = color;
            }
        }

        /// <summary>
        /// 스폰 시 페이드 인 연출을 재생하고 아이들 모션을 시작합니다.
        /// </summary>
        /// <param name="extraSpawnDelay">등장 연출 시작을 추가로 늦추는 시간(초). 같은 스텝의 몬스터를
        /// 모두 동시에 생성하면서도 순차적으로 나타나는 것처럼 보이게 할 때 사용합니다.</param>
        public void PlaySpawn(float extraSpawnDelay = 0f)
        {
            _spawnTween.TryCancel();
            _shadowTween.TryCancel();
            _isSpawning = true;

            float initialDelay = extraSpawnDelay + spawnVisualDelay;

            if (_gfxTransform != null)
            {
                _gfxTransform.localPosition = _gfxRestLocalPosition + Vector3.up * spawnDropHeight;
                _gfxTransform.localScale = _gfxBaseScale;

                // 착지(드랍 이동 완료) 즉시 하강을 재개시키고, 스쿼시 펀치는 그 위에서 별도로 재생한다.
                // 착지감 연출(스쿼시)이 끝날 때까지 이동을 묶어두면 착지 후에도 한 박자 멈춰있는 것처럼 보인다.
                MotionHandle dropMotion = LMotion.Create(_gfxRestLocalPosition + Vector3.up * spawnDropHeight, _gfxRestLocalPosition, spawnDropDuration)
                    .WithEase(Ease.InQuad)
                    .WithOnComplete(() => _isSpawning = false)
                    .BindToLocalPosition(_gfxTransform);

                MotionHandle punchMotion = LMotion.Punch.Create(_gfxBaseScale, landingPunchScale, spawnSquashDuration)
                    .WithFrequency(8)
                    .WithDampingRatio(0.8f)
                    .BindToLocalScale(_gfxTransform);

                MotionSequenceBuilder spawnBuilder = LSequence.Create();
                spawnBuilder.AppendInterval(initialDelay);
                spawnBuilder.Append(dropMotion);
                spawnBuilder.Append(punchMotion);
                _spawnTween = spawnBuilder.Run(builder => builder.WithOnComplete(OnSpawnLanded));
                _allMotions.Add(_spawnTween);
            }
            else
            {
                OnSpawnLanded();
            }

            if (shadowRenderer != null)
            {
                Transform shadowTransform = shadowRenderer.transform;
                shadowTransform.localPosition = new Vector3(0f, 0f, shadowTransform.localPosition.z);
                shadowTransform.localScale = _shadowBaseScale * shadowSpawnScale;
                shadowRenderer.color = new Color(0f, 0f, 0f, 0f);

                float shadowDuration = initialDelay + spawnDropDuration;

                MotionSequenceBuilder shadowBuilder = LSequence.Create();
                shadowBuilder.Join(LMotion.Create(0f, ShadowBaseAlpha, 0.16f).BindToColorA(shadowRenderer));
                shadowBuilder.Join(LMotion.Create(_shadowBaseScale * shadowSpawnScale, _shadowBaseScale, shadowDuration)
                    .WithEase(Ease.OutQuad)
                    .BindToLocalScale(shadowTransform));
                shadowBuilder.Join(LMotion.Create(0f, ShadowLocalY, shadowDuration)
                    .WithEase(Ease.OutQuad)
                    .BindToLocalPositionY(shadowTransform));
                _shadowTween = shadowBuilder.Run();
                _allMotions.Add(_shadowTween);
            }
        }

        private void Update()
        {
            if (IsDead || _reachedEnd)
                return;

            float deltaTime = Time.deltaTime;

            UpdateFreeze(deltaTime);
            UpdateBurn(deltaTime);

            // 등장 연출(위에서 낙하하는 gfx 오프셋)은 순전히 시각 효과이며, 실제 필드 하강은
            // 스폰과 동시에 시작해야 한다. 그렇지 않으면 낙하 연출이 끝날 때까지 몬스터가
            // 제자리에 멈춰 있는 것처럼 보인다.
            float effectiveSpeed = _descendSpeed * (IsFrozen ? Mathf.Max(0.05f, 1f - _freezeSlow) : 1f);
            float step = effectiveSpeed * deltaTime;

            Monster blocker = _grid.GetBlockerBelow(this, transform.position.x, _halfWidth);
            if (blocker != null)
            {
                float maxStep = transform.position.y - (blocker.transform.position.y + _halfHeight + blocker.HalfHeight);
                step = Mathf.Clamp(step, 0f, Mathf.Max(0f, maxStep));
            }

            transform.position += Vector3.down * step;

            UpdateSortingOrder();

            if (transform.position.y - _bottomOffsetY <= _grid.DangerY)
                ReachEnd();
        }

        private void OnDestroy()
        {
            _allMotions.Cancel();

            // 부착형 FX가 몬스터와 함께 파괴되어 풀이 오염되지 않도록 반드시 반환한다.
            _burnHandle?.Release();
            _burnHandle = null;
            _iceHandle?.Release();
            _iceHandle = null;
        }

        /// <summary>
        /// 볼 타격 데미지를 적용합니다. 냉동 추가 피해가 여기서 합산됩니다.
        /// </summary>
        /// <param name="amount">적용할 데미지</param>
        /// <param name="isCrit">치명타 여부</param>
        public void TakeDamage(int amount, bool isCrit)
        {
            if (IsDead)
                return;

            RevealHpBar();

            int final = ApplyFrozenBonus(amount);
            _currentHp -= final;

            GameplayEvents.DamagePopupRequested?.Invoke(transform.position, final,
                isCrit ? DamagePopupType.Crit : DamagePopupType.Normal);
            CombatEvents.MonsterHitLanded?.Invoke(transform.position, isCrit);

            UpdateHpBar();
            PlayFlash();

            if (_currentHp <= 0)
                Die();
        }

        /// <summary>
        /// 화상, 레이저, 폭발 등 효과 데미지를 적용합니다. 치명타 판정이 없습니다.
        /// </summary>
        /// <param name="amount">적용할 데미지</param>
        /// <param name="flashTint">true면 물리적 반동 없이 흰색 틴트만 순간 재생합니다. (예: 전기 공격)</param>
        public void TakeEffectDamage(int amount, bool flashTint = false)
        {
            if (IsDead)
                return;

            RevealHpBar();

            int final = ApplyFrozenBonus(amount);
            _currentHp -= final;

            GameplayEvents.DamagePopupRequested?.Invoke(transform.position, final, DamagePopupType.Effect);

            UpdateHpBar();

            if (_currentHp <= 0)
            {
                Die();
                return;
            }

            if (flashTint)
                FlashTint();
        }

        /// <summary>
        /// 화상 스택을 1 추가합니다. 지속시간은 갱신되고 스택은 최대치까지 누적됩니다.
        /// </summary>
        /// <param name="duration">화상 지속시간(초)</param>
        /// <param name="maxStacks">최대 중첩 수</param>
        /// <param name="tickDamage">중첩 하나당 초당 피해</param>
        public void ApplyBurn(float duration, int maxStacks, int tickDamage)
        {
            if (IsDead)
                return;

            _burnStacks = Mathf.Min(_burnStacks + 1, maxStacks);
            _burnTickDamage = Mathf.Max(_burnTickDamage, tickDamage);
            _burnRemaining = Mathf.Max(_burnRemaining, duration);

            UpdateStatusTint();
            PlayBurnParticles();
        }

        /// <summary>
        /// 냉동 효과를 적용합니다. 이동이 느려지고 받는 피해가 증가합니다.
        /// </summary>
        /// <param name="duration">냉동 지속시간(초)</param>
        /// <param name="slowPercent">이동속도 감소율 (0.1 = 10%)</param>
        /// <param name="bonusDamagePercent">받는 피해 증가율 (0.1 = 10%)</param>
        public void ApplyFreeze(float duration, float slowPercent, float bonusDamagePercent)
        {
            if (IsDead)
                return;

            _freezeTimer = Mathf.Max(_freezeTimer, duration);
            _freezeSlow = Mathf.Max(_freezeSlow, slowPercent);
            _freezeBonusDamage = Mathf.Max(_freezeBonusDamage, bonusDamagePercent);

            UpdateStatusTint();
            PlayIceParticles();
        }

        /// <summary>
        /// 자수정 단검 보너스를 소모 처리합니다.
        /// </summary>
        public void MarkAmethystConsumed()
        {
            AmethystConsumed = true;
        }

        /// <summary>
        /// 에메랄드 단검 보너스를 소모 처리합니다.
        /// </summary>
        public void MarkEmeraldConsumed()
        {
            EmeraldConsumed = true;
        }

        private void UpdateFreeze(float deltaTime)
        {
            if (_freezeTimer <= 0f)
                return;

            _freezeTimer -= deltaTime;

            if (_freezeTimer <= 0f)
            {
                _freezeSlow = 0f;
                _freezeBonusDamage = 0f;
                UpdateStatusTint();
                StopIceParticles();
            }
        }

        private void UpdateBurn(float deltaTime)
        {
            if (_burnStacks <= 0)
                return;

            _burnRemaining -= deltaTime;
            _burnTickTimer += deltaTime;

            if (_burnTickTimer >= 1f)
            {
                _burnTickTimer -= 1f;
                TakeEffectDamage(_burnTickDamage * _burnStacks);
            }

            if (_burnRemaining <= 0f && !IsDead)
            {
                _burnStacks = 0;
                _burnTickDamage = 0;
                UpdateStatusTint();
                StopBurnParticles(false);
            }
        }

        /// <summary>
        /// 필드 위험선에 도달하면 즉시 죽지 않고, <see cref="endShakeDuration"/>만큼 제자리에서
        /// 흔들리며 플레이어에게 추가 타격 기회를 준 뒤 플레이어의 Center로 날아가 공격합니다.
        /// 이 흔들림 구간 동안은 여전히 살아있는(타격 가능한) 상태입니다.
        /// </summary>
        private void ReachEnd()
        {
            _reachedEnd = true;

            _endShakeTween = LMotion.Shake.Create(transform.position, Vector3.one * endShakeStrength, endShakeDuration)
                .WithFrequency(endShakeVibrato)
                .WithOnComplete(BeginAttackFly)
                .BindToPosition(transform);
            _allMotions.Add(_endShakeTween);
        }

        /// <summary>
        /// 흔들림 경고가 끝난 뒤 플레이어의 Center 지점으로 날아가 공격합니다.
        /// 이 시점부터 죽은 것으로 취급되어 더 이상 타격할 수 없습니다.
        /// </summary>
        private void BeginAttackFly()
        {
            IsDead = true;

            _grid.Unregister(this);
            if (bodyCollider != null)
                bodyCollider.enabled = false;
            _allMotions.Cancel();
            StopBurnParticles(true);
            StopIceParticles();

            Vector3 targetPosition = ResolveAttackTargetPosition();

            _attackFlyTween = LMotion.Create(transform.position, targetPosition, attackFlyDuration)
                .WithEase(attackFlyEase)
                .WithOnComplete(OnAttackImpact)
                .BindToPosition(transform);
            _allMotions.Add(_attackFlyTween);

            if (shadowRenderer != null)
            {
                MotionHandle shadowFadeOut = LMotion.Create(shadowRenderer.color.a, 0f, attackFlyDuration * 0.5f)
                    .BindToColorA(shadowRenderer);
                _allMotions.Add(shadowFadeOut);
            }
        }

        /// <summary>
        /// 플레이어에게 도달했을 때 실제 피해를 적용하고 자신을 제거합니다.
        /// </summary>
        private void OnAttackImpact()
        {
            ReachedBottom?.Invoke(this);

            FadeGfx(0f, 0.12f, ReturnToPool);
        }

        /// <summary>
        /// 공격 목표 지점(플레이어의 Center)을 반환합니다. 참조를 찾을 수 없으면 현재 위치를 그대로 반환합니다.
        /// </summary>
        private Vector3 ResolveAttackTargetPosition()
        {
            return _attackTarget != null ? _attackTarget.position : transform.position;
        }

        private void Die()
        {
            IsDead = true;

            _grid.Unregister(this);
            if (bodyCollider != null)
                bodyCollider.enabled = false;
            StopBurnParticles(true);
            StopIceParticles();

            Died?.Invoke(this);
            CombatEvents.MonsterKilled?.Invoke(transform.position);

            _allMotions.Cancel();

            if (shadowRenderer != null)
            {
                MotionHandle shadowDeathFade = LMotion.Create(shadowRenderer.color.a, 0f, 0.22f).BindToColorA(shadowRenderer);
                _allMotions.Add(shadowDeathFade);
            }

            if (_gfxTransform != null)
            {
                MotionHandle gfxDeathScale = LMotion.Create(_gfxTransform.localScale, Vector3.zero, 0.22f)
                    .WithEase(Ease.InBack)
                    .BindToLocalScale(_gfxTransform);
                _allMotions.Add(gfxDeathScale);
            }

            FadeGfx(0f, 0.22f, ReturnToPool);
        }

        private void StartIdle()
        {
            if (_bodyTransform == null)
                return;

            _breatheTween.TryCancel();
            _bodyTransform.localScale = _bodyBaseScale;

            Vector3 breatheTarget = new Vector3(_bodyBaseScale.x * idleScaleX, _bodyBaseScale.y * idleScaleY, _bodyBaseScale.z);
            _breatheTween = LMotion.Create(_bodyBaseScale, breatheTarget, idleDuration * 0.9f)
                .WithEase(Ease.InOutSine)
                .WithLoops(-1, LoopType.Yoyo)
                .BindToLocalScale(_bodyTransform);
            _allMotions.Add(_breatheTween);
        }

        private void OnSpawnLanded()
        {
            _isSpawning = false;

            if (_gfxTransform != null)
            {
                _gfxTransform.localPosition = _gfxRestLocalPosition;
                _gfxTransform.localScale = _gfxBaseScale;
            }

            if (shadowRenderer != null)
            {
                Transform shadowTransform = shadowRenderer.transform;
                shadowTransform.localPosition = new Vector3(0f, ShadowLocalY, shadowTransform.localPosition.z);
                shadowTransform.localScale = _shadowBaseScale;
            }

            StartIdle();
        }

        private int ApplyFrozenBonus(int amount)
        {
            if (!IsFrozen)
                return amount;

            return Mathf.Max(1, Mathf.RoundToInt(amount * (1f + _freezeBonusDamage)));
        }

        private void UpdateSortingOrder()
        {
            if (sortingGroup != null)
                sortingGroup.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 4f);
        }

        private void ApplyDataVisuals(MonsterData data, Sprite block)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = true;
                bodyRenderer.sprite = data.Sprite;
                bodyRenderer.transform.localScale = _bodyBaseScale;
                bodyRenderer.transform.localPosition = new Vector3(0f, _bodyRestY, 0f);
            }

            if (blockRenderer != null)
            {
                blockRenderer.enabled = true;
                blockRenderer.sprite = block;
            }
        }

        private void BuildVisualFromPrefab(GameObject visualPrefab)
        {
            if (_gfxTransform == null)
                return;

            if (bodyRenderer != null)
                bodyRenderer.enabled = false;

            if (blockRenderer != null)
                blockRenderer.enabled = false;

            _visualInstance = Instantiate(visualPrefab, _gfxTransform);
            Transform visualTransform = _visualInstance.transform;
            visualTransform.localPosition = Vector3.zero;
            visualTransform.localRotation = Quaternion.identity;
            visualTransform.localScale = Vector3.one;
        }

        private void ClearVisualInstance()
        {
            if (_visualInstance == null)
                return;

            Destroy(_visualInstance);
            _visualInstance = null;
        }

        private void RefreshGfxRenderers()
        {
            if (_gfxTransform == null)
            {
                _gfxRenderers = Array.Empty<SpriteRenderer>();
                return;
            }

            _gfxRenderers = _gfxTransform.GetComponentsInChildren<SpriteRenderer>(true);
        }

        private SpriteRenderer[] GetTintRenderers()
        {
            if (bodyRenderer != null && bodyRenderer.enabled)
                return new[] { bodyRenderer };

            return _gfxRenderers;
        }

        /// <summary>
        /// 현재 상태 틴트/히트 플래시 값을 커스텀 셰이더 프로퍼티로 밀어 넣습니다.
        /// 머티리얼 인스턴스를 만들지 않도록 <see cref="MaterialPropertyBlock"/>을 사용합니다.
        /// </summary>
        private void ApplyTintProperties()
        {
            _propertyBlock ??= new MaterialPropertyBlock();

            Color statusColor = GetStatusTintColor();
            float statusAmount = HasStatusTint ? 1f : 0f;

            foreach (SpriteRenderer renderer in GetTintRenderers())
            {
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(StatusColorId, statusColor);
                _propertyBlock.SetFloat(StatusAmountId, statusAmount);
                _propertyBlock.SetColor(HitColorId, Color.white);
                _propertyBlock.SetFloat(HitAmountId, _hitAmount);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private MotionHandle FadeGfx(float alpha, float duration, Action onComplete)
        {
            MotionSequenceBuilder builder = LSequence.Create();
            bool hasRenderer = false;

            foreach (SpriteRenderer renderer in _gfxRenderers)
            {
                if (renderer == null)
                    continue;

                hasRenderer = true;
                builder.Join(LMotion.Create(renderer.color.a, alpha, duration).BindToColorA(renderer));
            }

            if (!hasRenderer)
                builder.AppendInterval(duration);

            MotionHandle handle = builder.Run(b => b.WithOnComplete(onComplete));
            _allMotions.Add(handle);
            return handle;
        }

        private void SetupColliderForFootprint(GridField grid, int footprintWidth, int footprintHeight)
        {
            if (bodyCollider == null)
                return;

            // 칸 크기에 살짝 못 미치게 잡아(0.934) 인접 칸 몬스터와 시각적으로 겹치지 않게 한다.
            const float colliderMargin = 0.934f;

            bodyCollider.size = new Vector2(
                grid.CellWidth * footprintWidth * colliderMargin,
                grid.CellHeight * footprintHeight * colliderMargin);
        }

        /// <summary>1칸(1x1) 몬스터 기준 frame 폭(px). UXML 기본값과 동일하게 맞춘다.</summary>
        private const float HpBarPixelsPerBlock = 67f;

        /// <summary>가로 폭이 1칸 늘어날 때마다 단순 배수보다 조금 더 길어 보이도록 추가하는 폭(px).</summary>
        private const float HpBarExtraPixelsPerExtraBlock = 10f;

        private void SetupHpBarForBlock(GridField grid, int footprintWidth, int footprintHeight)
        {
            if (!autoLayoutHpBar)
                return;

            if (_hpBarBackground != null)
            {
                float width = footprintWidth * HpBarPixelsPerBlock
                    + (footprintWidth - 1) * HpBarExtraPixelsPerExtraBlock;
                _hpBarBackground.style.width = new StyleLength(width);
            }

            if (hpBarRoot != null)
            {
                // 블록 pivot이 center라 footprintHeight가 커질수록 center가 위로 올라간다.
                // 1x1 기준 프리팹 위치(anchor)에서 늘어난 높이의 절반만큼 아래로 보정하면,
                // 몬스터 크기와 무관하게 HP 바가 항상 같은 스프라이트 영역 위에 표시된다.
                float offsetY = (footprintHeight - 1) * grid.CellHeight * 0.5f;
                hpBarRoot.localPosition = _hpBarBaseLocalPosition + Vector3.down * offsetY;
                _hpBarRestLocalPosition = hpBarRoot.localPosition;
            }
        }

        private void UpdateHpBar()
        {
            if (_hpBarFill == null)
                return;

            float ratio = Mathf.Clamp01((float)_currentHp / _maxHp);

            float value = ratio * 100f;
            
            if (value > 95f)
            {
                _hpBarDeco.style.display = DisplayStyle.Flex;
            }
            else
            {
                _hpBarDeco.style.display = DisplayStyle.None;
            }
            
            _hpBarFill.style.width = new StyleLength(Length.Percent(value));
        }

        /// <summary>
        /// 최초 피격 시 숨겨져 있던 HP 바를 드러냅니다.
        /// </summary>
        private void RevealHpBar()
        {
            if (_hpBarContainer != null)
                _hpBarContainer.style.display = DisplayStyle.Flex;
        }

        private void PlayFlash()
        {
            FlashTint();
            PlayHitReaction();

            // 맞은 방향과 무관하게 좌우로 흔들리는 짧은 회전 반동으로 피격감을 더한다.
        }

        /// <summary>
        /// 흰색으로 순간 번쩍인 뒤 현재 상태 틴트로 돌아옵니다. 물리적 피격 반동 없이 색상만 연출할 때 사용합니다.
        /// </summary>
        private void FlashTint()
        {
            _flashTween.TryCancel();
            _hitAmount = 1f;
            ApplyTintProperties();

            _flashTween = LMotion.Create(1f, 0f, hitFlashDuration)
                .Bind(this, static (value, self) => self.SetHitAmount(value));
            _allMotions.Add(_flashTween);
        }

        private void SetHitAmount(float amount)
        {
            _hitAmount = amount;
            ApplyTintProperties();
        }

        private void PlayHitReaction()
        {
            if (_isSpawning)
                return;

            _hitShakeTween.TryCancel();
            if (_gfxTransform == null)
                return;

            int direction = UnityEngine.Random.value < 0.5f ? -1 : 1;
            float moveX = UnityEngine.Random.Range(hitMoveMin, hitMoveMax) * direction;
            float moveY = UnityEngine.Random.Range(hitLiftMin, hitLiftMax);
            float angle = UnityEngine.Random.Range(hitShakeAngleMin, hitShakeAngleMax) * -direction;
            float scaleAmount = UnityEngine.Random.Range(hitScaleMin, hitScaleMax);
            Vector3 punchMove = new Vector3(moveX, moveY, 0f);
            Vector3 punchRotate = new Vector3(0f, 0f, angle);
            Vector3 punchScale = new Vector3(scaleAmount, -scaleAmount * 0.45f, 0f);

            _gfxTransform.localRotation = _gfxBaseRotation;
            _gfxTransform.localPosition = _gfxRestLocalPosition;
            _gfxTransform.localScale = _gfxBaseScale;

            Vector3 gfxBaseEuler = _gfxTransform.localEulerAngles;

            // DOTween DOPunch의 vibrato는 LitMotion Frequency와 대응하지만, LitMotion Punch는 좌우로
            // 대칭 진동하므로 같은 횟수라도 좌우 튕김이 더 많아 보인다. Frequency를 낮추고 DampingRatio를
            // 높여 첫 타격 뒤 빠르게 잦아들게 한다. 두 값은 인스펙터(hitShakeFrequency/Damping)에서 조절한다.
            MotionSequenceBuilder builder = LSequence.Create();
            builder.Join(LMotion.Punch.Create(_gfxRestLocalPosition, punchMove, hitShakeDuration).WithFrequency(hitShakeFrequency).WithDampingRatio(hitShakeDamping).BindToLocalPosition(_gfxTransform));
            builder.Join(LMotion.Punch.Create(gfxBaseEuler, punchRotate, hitShakeDuration).WithFrequency(hitShakeFrequency).WithDampingRatio(hitShakeDamping).BindToLocalEulerAngles(_gfxTransform));
            builder.Join(LMotion.Punch.Create(_gfxBaseScale, punchScale, hitShakeDuration).WithFrequency(hitShakeFrequency).WithDampingRatio(hitShakeDamping).BindToLocalScale(_gfxTransform));

            if (shadowRenderer != null)
            {
                Transform shadowTransform = shadowRenderer.transform;
                shadowTransform.localPosition = new Vector3(0f, ShadowLocalY, shadowTransform.localPosition.z);
                shadowTransform.localScale = _shadowBaseScale;
                builder.Join(LMotion.Punch.Create(shadowTransform.localPosition, new Vector3(moveX * 0.45f, 0f, 0f), hitShakeDuration).WithFrequency(hitShakeFrequency).WithDampingRatio(hitShakeDamping).BindToLocalPosition(shadowTransform));
                builder.Join(LMotion.Punch.Create(_shadowBaseScale, new Vector3(scaleAmount * 0.6f, scaleAmount * 0.25f, 0f), hitShakeDuration).WithFrequency(hitShakeFrequency).WithDampingRatio(hitShakeDamping).BindToLocalScale(shadowTransform));
            }

            if (hpBarRoot != null)
            {
                // 높이가 흔들리면 안 되므로 스케일 펀치는 x축만 적용한다.
                hpBarRoot.localPosition = _hpBarRestLocalPosition;
                hpBarRoot.localScale = Vector3.one;
                builder.Join(LMotion.Punch.Create(_hpBarRestLocalPosition, punchMove, hitShakeDuration).WithFrequency(hitShakeFrequency).WithDampingRatio(hitShakeDamping).BindToLocalPosition(hpBarRoot));
                builder.Join(LMotion.Punch.Create(Vector3.one, new Vector3(scaleAmount, 0f, 0f), hitShakeDuration).WithFrequency(hitShakeFrequency).WithDampingRatio(hitShakeDamping).BindToLocalScale(hpBarRoot));
            }

            _hitShakeTween = builder.Run();
            _allMotions.Add(_hitShakeTween);
        }

        private void UpdateStatusTint()
        {
            ApplyTintProperties();
        }

        private bool HasStatusTint => IsFrozen || _burnStacks > 0;

        private Color GetStatusTintColor()
        {
            if (IsFrozen)
                return FrozenTint;

            if (_burnStacks > 0)
                return BurnTint;

            return Color.white;
        }

        /// <summary>
        /// 화상 이펙트를 재생합니다. FXService 풀에서 부착형 인스턴스를 꺼내 몬스터에 붙입니다.
        /// </summary>
        private void PlayBurnParticles()
        {
            if (_fx == null || _burnHandle != null)
                return;

            _burnHandle = _fx.PlayAttached(FXId.MonsterBurn, StatusFxParent);
        }

        /// <summary>화상 이펙트를 끄고 풀에 반환합니다.</summary>
        private void StopBurnParticles(bool immediate)
        {
            _burnHandle?.Release();
            _burnHandle = null;
        }

        /// <summary>
        /// 냉동 이펙트를 재생합니다. FXService 풀에서 부착형 인스턴스를 꺼내 몬스터에 붙입니다.
        /// </summary>
        private void PlayIceParticles()
        {
            if (_fx == null || _iceHandle != null)
                return;

            _iceHandle = _fx.PlayAttached(FXId.MonsterFreeze, StatusFxParent);
        }

        /// <summary>냉동 이펙트를 끄고 풀에 반환합니다.</summary>
        private void StopIceParticles()
        {
            _iceHandle?.Release();
            _iceHandle = null;
        }

        /// <summary>상태이상 부착형 FX가 따라다닐 부모 트랜스폼입니다.</summary>
        private Transform StatusFxParent => gfxRoot != null ? gfxRoot : transform;
    }
}
