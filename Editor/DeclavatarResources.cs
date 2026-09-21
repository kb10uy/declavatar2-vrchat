using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace KusakaFactory.Declavatar2
{
    internal static class DeclavatarResources
    {
        private static readonly Dictionary<string, VisualTreeAsset> VisualTreeAssetCache = new Dictionary<string, VisualTreeAsset>();

        internal static VisualTreeAsset LoadVisualTreeByGuid(string guid)
        {
            if (VisualTreeAssetCache.TryGetValue(guid, out var cached) && cached != null) return cached;

            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) return null;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
            VisualTreeAssetCache[guid] = visualTree;
            return visualTree;
        }
    }
}
