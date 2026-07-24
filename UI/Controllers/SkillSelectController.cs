using BounceHeroes.Core;
using BounceHeroes.Managers;
using UnityEngine;
using VContainer;

namespace BounceHeroes.UI
{
    /// <summary>
    /// 3택지 카드 선택 입력을 받아 스킬 적용과 게임 재개를 실행합니다.
    /// </summary>
    public sealed class SkillSelectController : MonoBehaviour
    {
        private IGameFlow _gameFlow;
        private ISkillService _skills;

        [Inject]
        public void Construct(IGameFlow gameFlow, ISkillService skills)
        {
            _gameFlow = gameFlow;
            _skills = skills;
        }

        private void OnEnable()
        {
            GameUIEvents.SkillChoiceSelected += OnSkillChoiceSelected;
        }

        private void OnDisable()
        {
            GameUIEvents.SkillChoiceSelected -= OnSkillChoiceSelected;
        }

        private void OnSkillChoiceSelected(int index)
        {
            _skills.ApplyChoice(index);
            _gameFlow.ResumeFromSelection();
        }
    }
}
