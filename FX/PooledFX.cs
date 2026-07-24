using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BounceHeroes.FX
{
    /// <summary>
    /// 풀에서 재생되는 이펙트 인스턴스에 부착되어, 파티클 재시작과(일회성의 경우) 수명 후 자동 반환을 담당합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledFX : MonoBehaviour
    {
        private ParticleSystem[] _particles;
        private CancellationTokenSource _lifetimeCts;
        private bool _cached;

        /// <summary>일회성 재생: 파티클을 재시작하고 <paramref name="lifetime"/> 후 <paramref name="onExpire"/>를 호출합니다.</summary>
        public void PlayOneShot(float lifetime, Action onExpire)
        {
            EnsureCached();
            RestartParticles();

            CancelLifetime();
            _lifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            LifetimeAsync(lifetime, onExpire, _lifetimeCts.Token).Forget();
        }

        /// <summary>부착형 재생: 파티클만 재시작하고 자동 반환하지 않습니다(호출자가 핸들로 반환).</summary>
        public void PlayPersistent()
        {
            EnsureCached();
            RestartParticles();
        }

        /// <summary>풀 반환 시 호출: 진행 중인 수명 처리를 정리하고 파티클을 멈춥니다.</summary>
        public void OnReturned()
        {
            CancelLifetime();
            StopParticles();
        }

        private void EnsureCached()
        {
            if (_cached)
                return;

            _particles = GetComponentsInChildren<ParticleSystem>(true);
            _cached = true;
        }

        private async UniTaskVoid LifetimeAsync(float lifetime, Action onExpire, CancellationToken token)
        {
            if (lifetime > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(lifetime), cancellationToken: token);

            // 정상 완료: 실행 중 표시(CTS)를 정리한 뒤 콜백을 호출한다(코루틴 버전의 _lifetimeRoutine = null과 동일 취지).
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
            onExpire?.Invoke();
        }

        private void CancelLifetime()
        {
            if (_lifetimeCts == null)
                return;

            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
            _lifetimeCts = null;
        }

        private void RestartParticles()
        {
            if (_particles == null)
                return;

            foreach (ParticleSystem ps in _particles)
            {
                if (ps == null)
                    continue;

                ps.Clear(true);
                ps.Play(true);
            }
        }

        private void StopParticles()
        {
            if (_particles == null)
                return;

            foreach (ParticleSystem ps in _particles)
            {
                if (ps == null)
                    continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
