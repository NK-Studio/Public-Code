using BounceHeroes.Core;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace BounceHeroes.Managers
{
    /// <summary>
    /// 히트스톱, 카메라 셰이크, 슬로모, 전투 VFX 등 타격감 연출을 담당합니다.
    /// timeScale 변경은 직접 하지 않고 <see cref="GameTime"/>에 위임합니다.
    /// </summary>
    public sealed class JuiceManager : MonoBehaviour, IAutoBindable
    {
        [SerializeField, Required] private Transform cameraRig;
        [SerializeField] private Sprite flashSprite;
        [SerializeField, Required] private Volume postProcessVolume;
        [SerializeField] private float hitVignetteBoost = 0.04f;
        [SerializeField] private float hitBloomBoost = 0.22f;
        [SerializeField] private float critVignetteBoost = 0.08f;
        [SerializeField] private float critBloomBoost = 0.45f;
        [SerializeField] private float postPulseDuration = 0.11f;
        [SerializeField] private float muzzleFlashShake = 0.015f;

        private Vignette _vignette;
        private Bloom _bloom;
        private float _baseVignetteIntensity;
        private float _baseBloomIntensity;
        private MotionHandle _postPulseTween;
        private MotionHandle _cameraShakeMotion;
        private IFXService _fx;

        [Inject]
        public void Construct(IFXService fx)
        {
            _fx = fx;
        }

        private void Awake()
        {
            InitializePostProcessing();
        }

        private void OnEnable()
        {
            CombatEvents.MonsterHitLanded += OnMonsterHitLanded;
            CombatEvents.MonsterKilled += OnMonsterKilled;
            CombatEvents.RowLaserFired += OnRowLaserFired;
            CombatEvents.ExplosionFired += OnExplosionFired;
            CombatEvents.WaveLastKill += OnWaveLastKill;
            CombatEvents.BallFired += OnBallFired;
            CombatEvents.PlayerHit += OnPlayerHit;
        }

        private void OnDisable()
        {
            CombatEvents.MonsterHitLanded -= OnMonsterHitLanded;
            CombatEvents.MonsterKilled -= OnMonsterKilled;
            CombatEvents.RowLaserFired -= OnRowLaserFired;
            CombatEvents.ExplosionFired -= OnExplosionFired;
            CombatEvents.WaveLastKill -= OnWaveLastKill;
            CombatEvents.BallFired -= OnBallFired;
            CombatEvents.PlayerHit -= OnPlayerHit;

            _postPulseTween.TryCancel();
            RestorePostProcessing();
        }

        private void OnMonsterHitLanded(Vector3 position, bool isCrit)
        {
            if (!isCrit)
            {
                ShakeCamera(0.025f);
                PulsePostProcessing(hitVignetteBoost, hitBloomBoost);
                return;
            }

            GameTime.PlayEffect(0.05f, 0.045f);
            ShakeCamera(0.08f);
            PulsePostProcessing(critVignetteBoost, critBloomBoost);
        }

        private void OnMonsterKilled(Vector3 position)
        {
            GameTime.PlayEffect(0.05f, 0.05f);
            ShakeCamera(0.12f);
            PulsePostProcessing(critVignetteBoost, critBloomBoost);
            SpawnFlash(position, new Color(1f, 0.9f, 0.5f), Vector3.one * 0.4f, Vector3.one * 1.5f);
        }

        private void OnRowLaserFired(float worldY)
        {
            _fx.Play(FXId.Electricity, new Vector3(0f, worldY, 0f));
        }

        private void OnPlayerHit(Vector3 position)
        {
            ShakeCamera(0.1f);
            PulsePostProcessing(hitVignetteBoost, hitBloomBoost);

            _fx.Play(FXId.PlayerHit, position);
        }

        private void OnExplosionFired(Vector3 position, float radius)
        {
            SpawnFlash(position, new Color(1f, 0.55f, 0.2f, 0.9f), Vector3.one * 0.3f, Vector3.one * radius * 1.6f);
            ShakeCamera(0.1f);
            PulsePostProcessing(critVignetteBoost, critBloomBoost);

            _fx.Play(FXId.Explosion, position);
        }

        private void OnBallFired(Vector3 position, Vector2 direction)
        {
            SpawnFlash(position, new Color(1f, 0.95f, 0.6f, 0.9f), Vector3.one * 0.12f, Vector3.one * 0.45f);
            ShakeCamera(muzzleFlashShake);
        }

        private void OnWaveLastKill(Vector3 position)
        {
            GameTime.PlayEffect(0.35f, 0.55f);
            ShakeCamera(0.18f);
            PulsePostProcessing(critVignetteBoost * 1.4f, critBloomBoost * 1.2f);
        }

        /// <summary>씬의 포스트 프로세스 Volume을 찾아 비어 있으면 채웁니다. 에디터 "Auto Bind"에서 호출됩니다.</summary>
        public void AutoBind()
        {
            if (postProcessVolume == null)
                postProcessVolume = FindFirstObjectByType<Volume>();
        }

        private void InitializePostProcessing()
        {
            if (postProcessVolume == null || postProcessVolume.sharedProfile == null)
                return;

            postProcessVolume.profile = Instantiate(postProcessVolume.sharedProfile);

            if (postProcessVolume.profile.TryGet(out _vignette))
                _baseVignetteIntensity = _vignette.intensity.value;

            if (postProcessVolume.profile.TryGet(out _bloom))
                _baseBloomIntensity = _bloom.intensity.value;
        }

        private void PulsePostProcessing(float vignetteBoost, float bloomBoost)
        {
            if (_vignette == null && _bloom == null)
                return;

            _postPulseTween.TryCancel();

            float startVignette = _baseVignetteIntensity + vignetteBoost;
            float startBloom = _baseBloomIntensity + bloomBoost;

            if (_vignette != null)
                _vignette.intensity.value = startVignette;

            if (_bloom != null)
                _bloom.intensity.value = startBloom;

            MotionHandle vignetteMotion = LMotion.Create(startVignette, _baseVignetteIntensity, postPulseDuration)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(_vignette, static (value, vignette) =>
                {
                    if (vignette != null)
                        vignette.intensity.value = value;
                });

            MotionHandle bloomMotion = LMotion.Create(startBloom, _baseBloomIntensity, postPulseDuration)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(_bloom, static (value, bloom) =>
                {
                    if (bloom != null)
                        bloom.intensity.value = value;
                });

            _postPulseTween = LSequence.Create()
                .Join(vignetteMotion)
                .Join(bloomMotion)
                .Run();
        }

        private void RestorePostProcessing()
        {
            if (_vignette != null)
                _vignette.intensity.value = _baseVignetteIntensity;

            if (_bloom != null)
                _bloom.intensity.value = _baseBloomIntensity;
        }

        private void ShakeCamera(float strength)
        {
            if (cameraRig == null)
                return;

            _cameraShakeMotion.TryComplete();
            _cameraShakeMotion = LMotion.Shake.Create(cameraRig.localPosition, Vector3.one * strength, 0.18f)
                .WithFrequency(24)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalPosition(cameraRig);
        }

        private void SpawnFlash(Vector3 position, Color color, Vector3 fromScale, Vector3 toScale)
        {
            GameObject flash = CreateFlashObject(position, color);
            flash.transform.localScale = fromScale;

            SpriteRenderer renderer = flash.GetComponent<SpriteRenderer>();

            LMotion.Create(fromScale, toScale, 0.25f)
                .WithEase(Ease.OutQuad)
                .BindToLocalScale(flash.transform);

            LMotion.Create(color.a, 0f, 0.25f)
                .WithOnComplete(() => Destroy(flash))
                .BindToColorA(renderer);
        }

        private GameObject CreateFlashObject(Vector3 position, Color color)
        {
            GameObject flash = new GameObject("Flash");
            flash.transform.position = position;

            SpriteRenderer renderer = flash.AddComponent<SpriteRenderer>();
            renderer.sprite = flashSprite;
            renderer.color = color;
            renderer.sortingOrder = 40;

            return flash;
        }
    }
}
