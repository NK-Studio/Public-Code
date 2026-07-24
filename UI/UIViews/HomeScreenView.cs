using System;
using UnityEngine.UIElements;

namespace BounceHeroes.UI
{
    /// <summary>
    /// Home 화면의 메인 뷰입니다. 배경/데코와 게임 시작·랭킹 버튼의 입력만 담당하며,
    /// 실제 씬 전환·화면 전환은 상위(HomeUIManager)가 결정합니다.
    /// </summary>
    public sealed class HomeScreenView : UIView
    {
        private Button _gameStartButton;
        private Button _rankingButton;

        /// <summary>게임 시작 버튼을 눌렀을 때 발생합니다.</summary>
        public event Action GameStartRequested;

        /// <summary>랭킹 버튼을 눌렀을 때 발생합니다.</summary>
        public event Action RankingRequested;

        public HomeScreenView(VisualElement root) : base(root)
        {
        }

        protected override void SetVisualElements()
        {
            _gameStartButton = _root.Q<Button>("home-screen__game-start-button");
            _rankingButton = _root.Q<Button>("home-screen__ranking-button");
        }

        protected override void RegisterCallbacks()
        {
            _gameStartButton.clicked += OnGameStartClicked;
            _rankingButton.clicked += OnRankingClicked;
        }

        public override void Dispose()
        {
            _gameStartButton.clicked -= OnGameStartClicked;
            _rankingButton.clicked -= OnRankingClicked;
        }

        private void OnGameStartClicked() => GameStartRequested?.Invoke();

        private void OnRankingClicked() => RankingRequested?.Invoke();
    }
}
