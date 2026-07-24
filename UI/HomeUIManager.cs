using System;
using System.Collections.Generic;
using BounceHeroes.Core;
using BounceHeroes.Leaderboard;
using BounceHeroes.UI.UIViews;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VContainer;

namespace BounceHeroes.UI
{
    /// <summary>
    /// Coordinates Home UI views and scene transitions.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HomeUIManager : MonoBehaviour
    {
        private const string LoadingSceneName = "Loading";
        private const int LeaderboardTopCount = 10;

        private const string HomeScreenName = "home-screen";
        private const string RankingOverlayName = "RankingOverlay";
        private const string TransitionOverlayName = "TransitionOverlay";

        private readonly List<UIView> _allViews = new List<UIView>();

        private HomeScreenView _homeScreenView;
        private RankingView _rankingView;
        private TransitionView _transitionView;

        private ILeaderboardService _leaderboard;

        [Inject]
        public void Construct(ILeaderboardService leaderboard)
        {
            _leaderboard = leaderboard;
        }

        private void Start()
        {
            SetupViews(GetComponent<UIDocument>().rootVisualElement);
            RefreshLeaderboardAsync();
        }

        private void OnDisable()
        {
            if (_homeScreenView != null)
            {
                _homeScreenView.GameStartRequested -= OnGameStartRequested;
                _homeScreenView.RankingRequested -= OnRankingRequested;
            }

            if (_rankingView != null)
                _rankingView.BackRequested -= OnRankingBackRequested;

            foreach (UIView view in _allViews)
                view.Dispose();

            _allViews.Clear();
        }

        private void SetupViews(VisualElement root)
        {
            _homeScreenView = new HomeScreenView(root.Q<VisualElement>(HomeScreenName));
            _rankingView = new RankingView(root.Q<VisualElement>(RankingOverlayName));
            _transitionView = new TransitionView(root.Q<VisualElement>(TransitionOverlayName));

            _allViews.Add(_homeScreenView);
            _allViews.Add(_rankingView);
            _allViews.Add(_transitionView);

            foreach (UIView view in _allViews)
                view.Initialize();

            _homeScreenView.Show();

            _homeScreenView.GameStartRequested += OnGameStartRequested;
            _homeScreenView.RankingRequested += OnRankingRequested;
            _rankingView.BackRequested += OnRankingBackRequested;
        }

        private void OnRankingRequested() => _rankingView.Show();

        private void OnRankingBackRequested() => _rankingView.Hide();

        private void OnGameStartRequested()
        {
            HomeUIEvents.GameStartRequested?.Invoke();
            _transitionView.PlayFadeIn(() => SceneManager.LoadScene(LoadingSceneName), 1f);
        }

        private async void RefreshLeaderboardAsync()
        {
            if (_leaderboard == null)
                return;

            try
            {
                IReadOnlyList<LeaderboardEntry> top = await _leaderboard.GetTopAsync(LeaderboardTopCount);
                LeaderboardEntry self = await _leaderboard.GetSelfAsync();
                _rankingView.SetEntries(top, self);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HomeUIManager] Failed to load leaderboard: {e.Message}");
            }
        }
    }
}
