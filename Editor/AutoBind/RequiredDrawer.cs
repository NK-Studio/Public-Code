using BounceHeroes.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace BounceHeroes.EditorTools
{
    /// <summary>
    /// <see cref="RequiredAttribute"/>가 붙은 필드를 UI Toolkit으로 그립니다.
    /// 값이 비어 있으면 빨간 경고(HelpBox)를 필드 아래에 실시간으로 표시합니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    public sealed class RequiredDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new VisualElement();

            PropertyField field = new PropertyField(property);
            root.Add(field);

            HelpBox warning = new HelpBox(
                "필수 참조가 비어 있습니다. 값을 지정하거나 컴포넌트의 'Auto Bind'를 누르세요.",
                HelpBoxMessageType.Error);
            root.Add(warning);

            UpdateWarning(property, warning);
            field.TrackPropertyValue(property, changed => UpdateWarning(changed, warning));

            return root;
        }

        private static void UpdateWarning(SerializedProperty property, HelpBox warning)
        {
            bool missing = property.propertyType == SerializedPropertyType.ObjectReference
                && property.objectReferenceValue == null;

            warning.style.display = missing ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
