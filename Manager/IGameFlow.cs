using BounceHeroes.Data;

namespace BounceHeroes.Managers
{
    /// <summary>
    /// 게임 흐름(상태 전환·플레이어 피해·선택/종료)의 추상화입니다.
    /// 소비자는 <c>GameManager.Instance</c> 정적 참조 대신 이 인터페이스를 주입받습니다.
    /// </summary>
    public interface IGameFlow
    {
        /// <summary>현재 게임 상태입니다.</summary>
        GameState State { get; }

        /// <summary>플레이어에게 피해를 입힙니다.</summary>
        void TakePlayerDamage(int amount);

        /// <summary>3택지 선택 상태로 전환합니다.</summary>
        void EnterSelection(SkillChoice[] choices);

        /// <summary>선택을 마치고 플레이 상태로 복귀합니다.</summary>
        void ResumeFromSelection();

        /// <summary>게임을 종료하고 결과를 표시합니다.</summary>
        void EndGame(bool won);
    }
}
