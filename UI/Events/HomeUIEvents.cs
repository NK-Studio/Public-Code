using System;

namespace BounceHeroes.UI
{
    /// <summary>
    /// Home 화면의 UI 입력 의도를 전달하는 정적 이벤트 허브입니다. GameUIEvents와 동일한 역할이지만,
    /// Home 씬 전용 이벤트를 Main 씬의 GameUIEvents와 의미가 섞이지 않도록 분리했습니다.
    /// </summary>
    public static class HomeUIEvents
    {
        /// <summary>Home 화면에서 게임 시작 버튼을 눌렀을 때 호출됩니다.</summary>
        public static Action GameStartRequested;
    }
}
