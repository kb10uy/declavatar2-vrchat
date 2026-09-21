using UnityEngine;
using VRC.SDKBase;

namespace KusakaFactory.Declavatar2.Runtime
{
    [AddComponentMenu("Declavatar 2/Declavatar Declaration")]
    public sealed class DeclavatarDeclaration : MonoBehaviour, IEditorOnly
    {
        public TextAsset Script;
        public string[] Symbols = new string[0];
        public Object[] ModuleRoots = new Object[0];
        public DeclavatarAssetDictionary[] AssetDictionaries = new DeclavatarAssetDictionary[0];
        public GameObject RelativePathRoot;
        public bool MatchAvatarWriteDefaults;
    }
}
