using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace KusakaFactory.Declavatar2.Assets
{
    [ScriptedImporter(1, "lua")]
    public sealed class LuaScriptImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = new TextAsset(File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset("script", asset);
            ctx.SetMainObject(asset);
        }
    }
}
