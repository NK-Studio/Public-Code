using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// 게임 전역 밸런스 수치를 보관하는 설정 에셋입니다.
    /// 난이도(Normal/Hard)마다 이 에셋을 하나씩 따로 만들어 서로 다른 수치를 넣습니다.
    /// (예: SO_GameBalance_Normal, SO_GameBalance_Hard)
    /// </summary>
    [CreateAssetMenu(fileName = "SO_GameBalance", menuName = "BounceHeroes/Game Balance")]
    public class GameBalanceData : ScriptableObject
    {
        [Header("볼 - 기본 성능")]
        [Tooltip("노멀 볼(스킬 미적용 볼)의 충돌 데미지입니다.")]
        [SerializeField] private int normalBallDamage = 8;
        [Tooltip("치명타가 터질 기본 확률입니다. (0~1, 스킬로 추가 상승)")]
        [SerializeField, Range(0f, 1f)] private float baseCritChance = 0f;
        [Tooltip("치명타 시 추가되는 데미지 비율입니다. (0.5 = 데미지 +50%)")]
        [SerializeField] private float critDamageRate = 0.5f;
        [Tooltip("볼이 날아가는 고정 속도(월드 단위/초)입니다. 반사되어도 이 속도를 유지합니다.")]
        [SerializeField] private float ballSpeed = 9f;

        [Header("볼 - 발사")]
        [Tooltip("한 번 조준해서 놓았을 때 순차 발사되는 볼의 총 개수입니다.")]
        [SerializeField] private int volleySize = 10;
        [Tooltip("한 발사 시퀀스 안에서 볼과 볼 사이의 발사 간격(초)입니다.")]
        [SerializeField] private float fireInterval = 0.12f;
        [Tooltip("발사 후 이 시간(초)이 지나면 맞은 것이 없어도 자동으로 회수됩니다.")]
        [SerializeField] private float maxFlightTime = 9f;

        [Header("플레이어")]
        [Tooltip("플레이어(스테이지)의 최대 체력입니다. 몬스터가 바닥에 도달할 때마다 깎입니다.")]
        [SerializeField] private int playerMaxHp = 100;

        [Header("몬스터")]
        [Tooltip("몬스터가 아래로 내려오는 초당 이동 속도(월드 단위)입니다. 값이 클수록 더 빨리 내려옵니다.")]
        [SerializeField] private float descendSpeed = 0.35f;
        [Tooltip("몬스터의 기본 체력(MonsterData.BaseHp)에 곱해지는 배율입니다. " +
            "난이도별로 이 에셋 자체를 통째로 바꿔 끼우는 방식이라, 이 값 하나로 그 난이도의 몬스터 체력이 전부 정해집니다. " +
            "예: Normal 에셋은 1.0, Hard 에셋은 1.4 등.")]
        [SerializeField] private float monsterHpMultiplier = 1f;

        [Header("점수 - 기본")]
        [Tooltip("몬스터 1처치의 기본 점수입니다. 콤보 배수가 곱해집니다.")]
        [SerializeField] private int scorePerKill = 100;
        [Tooltip("치명타 적중 1회당 추가 점수입니다.")]
        [SerializeField] private int critHitBonus = 30;
        [Tooltip("웨이브 1개 클리어당 보너스 점수입니다.")]
        [SerializeField] private int waveClearBonus = 500;

        [Header("점수 - 콤보")]
        [Tooltip("이 시간(초) 안에 다음 처치를 못 하면 콤보가 끊깁니다.")]
        [SerializeField] private float comboTimeWindow = 2.5f;
        [Tooltip("콤보 1스택당 처치 점수 배수 증가량입니다. (0.1 = 스택당 +10%)")]
        [SerializeField] private float comboBonusPerStack = 0.1f;
        [Tooltip("콤보 배수 상한입니다. (3 = 최대 3배)")]
        [SerializeField] private float comboMaxMultiplier = 3f;

        [Header("점수 - 승리 보너스")]
        [Tooltip("승리 시 잔여 체력 전부(100%)일 때 주는 최대 보너스입니다. 잔여 비율만큼 지급됩니다.")]
        [SerializeField] private int hpRemainingBonus = 3000;
        [Tooltip("승리 시 즉시 클리어(0초) 기준 최대 시간 보너스입니다. 오래 걸릴수록 감소합니다.")]
        [SerializeField] private int clearTimeBonusMax = 6000;
        [Tooltip("클리어 시간 1초당 시간 보너스 감소량입니다.")]
        [SerializeField] private float clearTimeBonusPerSecond = 20f;

        [Header("점수 - 등급 임계값")]
        [Tooltip("이 점수 이상이면 S 등급입니다.")]
        [SerializeField] private long gradeThresholdS = 20000;
        [Tooltip("이 점수 이상이면 A 등급입니다.")]
        [SerializeField] private long gradeThresholdA = 14000;
        [Tooltip("이 점수 이상이면 B 등급입니다.")]
        [SerializeField] private long gradeThresholdB = 9000;
        [Tooltip("이 점수 이상이면 C 등급입니다. 미만이면 D 등급입니다.")]
        [SerializeField] private long gradeThresholdC = 4000;

        /// <summary>노멀 볼의 충돌 데미지입니다.</summary>
        public int NormalBallDamage => normalBallDamage;

        /// <summary>기본 치명타 확률입니다. (0~1)</summary>
        public float BaseCritChance => baseCritChance;

        /// <summary>치명타 시 추가 데미지 비율입니다. (0.5 = +50%)</summary>
        public float CritDamageRate => critDamageRate;

        /// <summary>볼의 고정 이동 속도입니다.</summary>
        public float BallSpeed => ballSpeed;

        /// <summary>한 번의 발사에서 쏘는 볼의 총 개수입니다.</summary>
        public int VolleySize => volleySize;

        /// <summary>연속 발사 시 볼 사이의 시간 간격입니다.</summary>
        public float FireInterval => fireInterval;

        /// <summary>이 시간이 지나면 볼이 자동으로 회수 상태로 전환됩니다.</summary>
        public float MaxFlightTime => maxFlightTime;

        /// <summary>플레이어의 최대 체력입니다.</summary>
        public int PlayerMaxHp => playerMaxHp;

        /// <summary>몬스터가 아래로 내려오는 초당 이동 속도(월드 단위)입니다.</summary>
        public float DescendSpeed => descendSpeed;

        /// <summary>몬스터 기본 체력에 곱해지는 배율입니다. 난이도별 체력 차이는 이 값 하나로 결정됩니다.</summary>
        public float MonsterHpMultiplier => monsterHpMultiplier;

        /// <summary>몬스터 1처치의 기본 점수입니다.</summary>
        public int ScorePerKill => scorePerKill;

        /// <summary>치명타 적중 1회당 추가 점수입니다.</summary>
        public int CritHitBonus => critHitBonus;

        /// <summary>웨이브 1개 클리어당 보너스 점수입니다.</summary>
        public int WaveClearBonus => waveClearBonus;

        /// <summary>콤보가 끊기는 시간창(초)입니다.</summary>
        public float ComboTimeWindow => comboTimeWindow;

        /// <summary>콤보 1스택당 처치 점수 배수 증가량입니다.</summary>
        public float ComboBonusPerStack => comboBonusPerStack;

        /// <summary>콤보 배수 상한입니다.</summary>
        public float ComboMaxMultiplier => comboMaxMultiplier;

        /// <summary>승리 시 잔여 체력 100% 기준 최대 보너스입니다.</summary>
        public int HpRemainingBonus => hpRemainingBonus;

        /// <summary>승리 시 0초 기준 최대 시간 보너스입니다.</summary>
        public int ClearTimeBonusMax => clearTimeBonusMax;

        /// <summary>클리어 시간 1초당 시간 보너스 감소량입니다.</summary>
        public float ClearTimeBonusPerSecond => clearTimeBonusPerSecond;

        /// <summary>S 등급 점수 임계값입니다.</summary>
        public long GradeThresholdS => gradeThresholdS;

        /// <summary>A 등급 점수 임계값입니다.</summary>
        public long GradeThresholdA => gradeThresholdA;

        /// <summary>B 등급 점수 임계값입니다.</summary>
        public long GradeThresholdB => gradeThresholdB;

        /// <summary>C 등급 점수 임계값입니다.</summary>
        public long GradeThresholdC => gradeThresholdC;
    }
}
