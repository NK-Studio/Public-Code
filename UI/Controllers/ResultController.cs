using BounceHeroes.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BounceHeroes.UI
{
    /// <summary>
    /// 결과 팝업의 재시작 입력을 받아 스테이지를 다시 로드합니다.
    /// </summary>
    public sealed class ResultController : MonoBehaviour
    {
        private void OnEnable()
        {
            GameUIEvents.RestartRequested += OnRestartRequested;
        }

        private void OnDisable()
        {
            GameUIEvents.RestartRequested -= OnRestartRequested;
        }

        private void OnRestartRequested()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
