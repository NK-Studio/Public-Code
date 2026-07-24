using System;

namespace BounceHeroes.Core
{
    /// <summary>
    /// UI 입력 의도를 게임 로직으로 전달하는 이벤트 허브입니다.
    /// </summary>
    public static class GameUIEvents
    {
        /// <summary>인트로 트랜지션(페이드 아웃)이 끝나고 지정한 지연이 지나 게임 플레이를 시작해도 될 때 호출됩니다.</summary>
        public static Action GameStartRequested;

        /// <summary>3택지에서 선택지를 골랐을 때 호출됩니다. (선택 인덱스)</summary>
        public static Action<int> SkillChoiceSelected;

        /// <summary>결과 팝업에서 재시작 버튼을 눌렀을 때 호출됩니다.</summary>
        public static Action RestartRequested;

        /// <summary>HUD의 일시정지 버튼을 눌렀을 때 호출됩니다.</summary>
        public static Action PauseRequested;

        /// <summary>일시정지 팝업 바깥을 클릭하거나 이어하기 버튼을 눌러 팝업을 닫을 때 호출됩니다.</summary>
        public static Action PauseCloseRequested;

        /// <summary>일시정지 팝업에서 홈으로 나가기를 눌렀을 때 호출됩니다.</summary>
        public static Action ExitToHomeRequested;

        /// <summary>BGM 켬/끔 토글이 변경되었을 때 호출됩니다. (켜짐 여부)</summary>
        public static Action<bool> BgmEnabledChanged;

        /// <summary>SFX 켬/끔 토글이 변경되었을 때 호출됩니다. (켜짐 여부)</summary>
        public static Action<bool> SfxEnabledChanged;
    }
}
