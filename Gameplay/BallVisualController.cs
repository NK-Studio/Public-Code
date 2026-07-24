using BounceHeroes.Core;
using BounceHeroes.Data;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace BounceHeroes.Gameplay
{
    public sealed class BallVisualController : MonoBehaviour, IAutoBindable
    {
        [SerializeField, Required] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private BallVisualProfile visualProfile;
        [SerializeField] private ParticleSystem ambientParticles;
        [SerializeField] private ParticleSystem[] hitBurstVariants;
        [SerializeField] private GameObject hitFxPrefab;
        [SerializeField] private float hitFxLifetime = 1f;
        [SerializeField, Min(0f)] private float hitFxTangentJitter = 0.18f;
        [SerializeField] private Vector2 hitFxNormalJitter = new Vector2(-0.04f, 0.08f);
        [SerializeField] private Vector2 hitFxScaleRange = new Vector2(0.92f, 1.12f);
        [SerializeField] private float hitFxRotationJitter = 18f;

        private Vector3 _baseScale;
        private MotionHandle _scaleMotion;
        private IFXService _fx;

        /// <summary>타격 시 재생되는 FX 프리팹입니다. 미리 생성(prewarm) 대상 조회에 사용됩니다.</summary>
        public GameObject HitFxPrefab => hitFxPrefab;

        /// <summary>타격 FX의 수명(초)입니다.</summary>
        public float HitFxLifetime => hitFxLifetime;

        /// <summary>타격 FX 풀링에 사용할 FX 서비스를 주입합니다. 볼 생성 직후 발사기가 호출합니다.</summary>
        public void SetFXService(IFXService fx)
        {
            _fx = fx;
        }

        /// <summary>진행 중인 스케일 연출을 취소합니다. 풀에서 재사용되기 전 호출됩니다.</summary>
        public void CancelMotions()
        {
            _scaleMotion.TryCancel();
        }

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        /// <summary>같은 게임오브젝트의 렌더러를 찾아 비어 있는 참조만 채웁니다. 에디터 "Auto Bind"에서 호출됩니다.</summary>
        public void AutoBind()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (trail == null)
                trail = GetComponent<TrailRenderer>();
        }

        public void Apply()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;

            ConfigureTrail(visualProfile);
            ConfigureAmbientParticles(visualProfile);

            _scaleMotion.TryCancel();
            transform.localScale = _baseScale;
        }

        /// <summary>
        /// 볼의 스프라이트를 교체합니다. 클러스터 볼 파편처럼 여러 겉모습 중 하나를 무작위로 쓸 때 사용합니다.
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (spriteRenderer != null && sprite != null)
                spriteRenderer.sprite = sprite;
        }

        public void PlayHitBurst(Vector2 position, Vector2? contactNormal = null)
        {
            Vector2 spawnPosition = ApplyHitFxPositionVariation(position, contactNormal);
            Quaternion spawnRotation = Quaternion.Euler(0f, 0f, Random.Range(-hitFxRotationJitter, hitFxRotationJitter));
            float spawnScale = Random.Range(
                Mathf.Min(hitFxScaleRange.x, hitFxScaleRange.y),
                Mathf.Max(hitFxScaleRange.x, hitFxScaleRange.y));

            if (hitFxPrefab != null)
            {
                _fx?.Play(hitFxPrefab, spawnPosition, spawnRotation, spawnScale, hitFxLifetime);
            }
            else
            {
                ParticleSystem variant = PickHitBurstVariant();

                if (variant != null)
                {
                    int burstCount = visualProfile != null ? visualProfile.HitBurstCount : 1;
                    variant.transform.position = spawnPosition;
                    variant.transform.rotation = spawnRotation;
                    variant.Emit(Mathf.Max(1, burstCount));
                }
            }

            _scaleMotion.TryCancel();
            transform.localScale = _baseScale;

            if (visualProfile != null)
            {
                _scaleMotion = LMotion.Punch.Create(_baseScale, Vector3.one * (visualProfile.PulseScale - 1f), visualProfile.PulseDuration)
                    .WithFrequency(6)
                    .WithDampingRatio(0.55f)
                    .BindToLocalScale(transform);
            }
        }

        private Vector2 ApplyHitFxPositionVariation(Vector2 position, Vector2? contactNormal)
        {
            if (!contactNormal.HasValue || contactNormal.Value.sqrMagnitude < 0.001f)
                return position + Random.insideUnitCircle * hitFxTangentJitter;

            Vector2 normal = contactNormal.Value.normalized;
            Vector2 tangent = new Vector2(-normal.y, normal.x);
            float tangentOffset = Random.Range(-hitFxTangentJitter, hitFxTangentJitter);
            float normalOffset = Random.Range(
                Mathf.Min(hitFxNormalJitter.x, hitFxNormalJitter.y),
                Mathf.Max(hitFxNormalJitter.x, hitFxNormalJitter.y));

            return position + tangent * tangentOffset + normal * normalOffset;
        }

        private ParticleSystem PickHitBurstVariant()
        {
            if (hitBurstVariants == null || hitBurstVariants.Length == 0)
                return null;

            return hitBurstVariants[Random.Range(0, hitBurstVariants.Length)];
        }

        private void ConfigureTrail(BallVisualProfile profile)
        {
            if (trail == null)
                return;

            Color head = profile != null ? profile.TrailHeadColor : new Color(1f, 1f, 1f, 0.55f);
            Color tail = profile != null ? profile.TrailTailColor : new Color(1f, 1f, 1f, 0f);

            float startWidth = profile != null ? profile.TrailStartWidth : 0.16f;
            float endWidth = profile != null ? profile.TrailEndWidth : 0.02f;

            trail.Clear();
            trail.time = profile != null ? profile.TrailTime : 0.22f;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, startWidth),
                new Keyframe(1f, endWidth));
            trail.colorGradient = BuildGradient(head, tail);
            trail.emitting = true;
        }

        private void ConfigureAmbientParticles(BallVisualProfile profile)
        {
            if (ambientParticles == null)
                return;

            if (profile == null)
            {
                ambientParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }

            ParticleSystem.MainModule main = ambientParticles.main;
            main.startColor = profile.ParticleColor;

            ambientParticles.Play(true);
        }

        private static Gradient BuildGradient(Color head, Color tail)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(head, 0f), new GradientColorKey(tail, 1f) },
                new[] { new GradientAlphaKey(head.a, 0f), new GradientAlphaKey(tail.a, 1f) });
            return gradient;
        }
    }
}
