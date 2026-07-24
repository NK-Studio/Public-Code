using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BounceHeroes.Utility
{
    public sealed class LoadingSceneController : MonoBehaviour
    {
        [SerializeField] private SceneReference nextSceneName;

        private void Start()
        {
            LoadNextSceneAsync().Forget();
        }

        private async UniTaskVoid LoadNextSceneAsync()
        {
            await SceneManager.LoadSceneAsync(nextSceneName.Path)
                .ToUniTask(cancellationToken: destroyCancellationToken);
        }
    }
}
