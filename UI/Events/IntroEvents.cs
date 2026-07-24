using System;

namespace BounceHeroes.UI
{
    /// <summary>
    /// Static event hub for Intro screen UI intents.
    /// </summary>
    public static class IntroEvents
    {
        /// <summary>Raised when the nickname popup confirm button is clicked.</summary>
        public static Action NicknameConfirmClicked;
    }
}
