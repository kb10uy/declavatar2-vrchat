using UnityEngine;
using UnityEditor;

namespace KusakaFactory.Declavatar2
{
    [FilePath("ProjectSettings/Declavatar2Settings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class DeclavatarSettings : ScriptableSingleton<DeclavatarSettings>
    {
        private const string MenuPrefix = "Tools/Declavatar 2/";

        [SerializeField] private bool _debugSymbolInPlayMode;

        internal bool DebugSymbolInPlayMode => _debugSymbolInPlayMode;

        [MenuItem(MenuPrefix + "Debug mode (add _DEBUG symbol in Play)")]
        private static void ToggleDebugSymbolInPlayMode()
        {
            instance._debugSymbolInPlayMode = !instance._debugSymbolInPlayMode;
            instance.Save(true);
        }

        [MenuItem(MenuPrefix + "Debug mode (add _DEBUG symbol in Play)", true)]
        private static bool ValidateDebugSymbolInPlayMode()
        {
            Menu.SetChecked(MenuPrefix + "Debug mode (add _DEBUG symbol in Play)", instance._debugSymbolInPlayMode);
            return true;
        }
    }
}
