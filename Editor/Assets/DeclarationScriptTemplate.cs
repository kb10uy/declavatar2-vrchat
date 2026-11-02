using UnityEditor;

namespace KusakaFactory.Declavatar2.Assets
{
    internal static class DeclarationScriptTemplate
    {
        private const string TemplateGuid = "e0e5c56cf14b48d18b069f6b14eefdee";

        [MenuItem("Assets/Create/Declavatar 2/Declaration Script")]
        private static void CreateDeclarationScript()
        {
            var templatePath = AssetDatabase.GUIDToAssetPath(TemplateGuid);
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewDeclaration.declua");
        }
    }
}
