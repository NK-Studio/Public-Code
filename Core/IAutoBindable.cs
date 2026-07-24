namespace BounceHeroes.Core
{
    /// <summary>
    /// 에디터의 "Auto Bind" 버튼으로 씬/자식 참조를 자동 연결할 수 있는 컴포넌트입니다.
    /// 바인딩은 <b>에디트 타임에만</b> 일어나며 SerializedObject로 직렬화됩니다(런타임 자동 주입이 아님).
    /// </summary>
    public interface IAutoBindable
    {
        /// <summary>
        /// 비어 있는 직렬화 참조를 씬/자식에서 찾아 채웁니다. 에디터에서만 호출됩니다.
        /// 구현은 이미 지정된 참조를 덮어쓰지 않도록 null인 필드만 채워야 합니다.
        /// </summary>
        void AutoBind();
    }
}
