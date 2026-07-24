using BounceHeroes.Core;
using UnityEngine;
using VContainer;

namespace BounceHeroes.UI
{
    /// <summary>
    /// Handles Home scene audio decisions and delegates playback to the injected audio service.
    /// </summary>
    public sealed class HomeController : MonoBehaviour
    {
        private const string BgmDeadParameter = "Dead";

        [SerializeField, Tooltip("SFX id played when the game start button is clicked.")]
        private AudioId gameStartSfx = AudioId.Transition;

        private IAudioService _audio;

        public IAudioService Audio => _audio;

        [Inject]
        public void Construct(IAudioService audio)
        {
            _audio = audio;
            _audio.PlayBgm(AudioId.BgmGameplay);
            _audio.SetBgmParameter(BgmDeadParameter, 0f);
        }

        private void OnEnable()
        {
            HomeUIEvents.GameStartRequested += PlayGameStartSfx;
        }

        private void OnDisable()
        {
            HomeUIEvents.GameStartRequested -= PlayGameStartSfx;
        }

        private void PlayGameStartSfx() => _audio?.PlaySfx(gameStartSfx);
    }
}
