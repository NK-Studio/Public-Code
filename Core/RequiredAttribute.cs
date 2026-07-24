using UnityEngine;

namespace BounceHeroes.Core
{
    /// <summary>
    /// 이 직렬화 필드가 비어 있으면 인스펙터에 빨간 경고를 표시합니다.
    /// 런타임에 스스로 채우지 않고, 에디터에서 지정하거나 "Auto Bind" 버튼으로 채우도록 강제합니다.
    /// </summary>
    public sealed class RequiredAttribute : PropertyAttribute
    {
    }
}
