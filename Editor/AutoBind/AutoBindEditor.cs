using BounceHeroes.Core;
using BounceHeroes.Gameplay;
using BounceHeroes.Managers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BounceHeroes.EditorTools
{
    /// <summary>
    /// <see cref="IAutoBindable"/> 컴포넌트 인스펙터 상단에 "Auto Bind" 버튼을 추가하는 UI Toolkit 에디터 베이스입니다.
    /// 버튼 클릭 시 대상의 <see cref="IAutoBindable.AutoBind"/>를 호출하고 Undo/Dirty로 직렬화합니다(에디트 타임 바인딩).
    /// </summary>
    public abstract class AutoBindEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            if (target is IAutoBindable)
            {
                Button bindButton = new Button(AutoBindTargets) { text = "Auto Bind" };
                bindButton.style.marginBottom = 4;
                bindButton.style.marginTop = 2;
                root.Add(bindButton);
            }

            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }

        private void AutoBindTargets()
        {
            foreach (Object obj in targets)
            {
                if (obj is not IAutoBindable bindable)
                    continue;

                Undo.RecordObject(obj, "Auto Bind");
                bindable.AutoBind();
                EditorUtility.SetDirty(obj);
            }

            serializedObject.Update();
        }
    }

    [CustomEditor(typeof(BallLauncher))]
    [CanEditMultipleObjects]
    public sealed class BallLauncherEditor : AutoBindEditor
    {
    }

    [CustomEditor(typeof(BallVisualController))]
    [CanEditMultipleObjects]
    public sealed class BallVisualControllerEditor : AutoBindEditor
    {
    }

    [CustomEditor(typeof(JuiceManager))]
    [CanEditMultipleObjects]
    public sealed class JuiceManagerEditor : AutoBindEditor
    {
    }

    [CustomEditor(typeof(global::Utility.CameraFitter))]
    [CanEditMultipleObjects]
    public sealed class CameraFitterEditor : AutoBindEditor
    {
    }
}
