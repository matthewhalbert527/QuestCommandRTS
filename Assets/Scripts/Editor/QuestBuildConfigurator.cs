#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace QuestCommandRTS.Editor
{
    public static class RtsEditorTools
    {
        private const string ScenePath = "Assets/Scenes/Battlefield.unity";

        [MenuItem("Command RTS/Open Battlefield Scene")]
        public static void OpenBattlefieldScene()
        {
            EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
#endif
