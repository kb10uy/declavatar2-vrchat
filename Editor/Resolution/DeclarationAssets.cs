using System;
using System.Collections.Generic;
using System.Linq;
using KusakaFactory.Declavatar2.Runtime;

namespace KusakaFactory.Declavatar2.Resolution
{
    public static class DeclarationAssets
    {
        public static IReadOnlyList<DeclavatarAssetDictionary.Entry> Entries(DeclavatarDeclaration declaration)
        {
            var overrides = (declaration.AssetOverrides ?? Array.Empty<DeclavatarAssetDictionary.Entry>())
                .Where(entry => !string.IsNullOrEmpty(entry.Name) && entry.Asset != null);
            var dictionaries = (declaration.AssetDictionaries ?? Array.Empty<DeclavatarAssetDictionary>())
                .Where(dictionary => dictionary != null)
                .SelectMany(dictionary => dictionary.Entries ?? Array.Empty<DeclavatarAssetDictionary.Entry>());
            return overrides.Concat(dictionaries).ToArray();
        }
    }
}
