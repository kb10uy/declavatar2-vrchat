using System;
using UnityEngine;

namespace KusakaFactory.Declavatar2.Runtime
{
    [CreateAssetMenu(fileName = "DeclavatarAssets", menuName = "Declavatar 2/Asset Dictionary")]
    public sealed class DeclavatarAssetDictionary : ScriptableObject
    {
        public Entry[] Entries = new Entry[0];

        [Serializable]
        public struct Entry
        {
            public string Name;
            public UnityEngine.Object Asset;
        }
    }
}
