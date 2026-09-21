using System;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;
using UnityEditor;
using nadena.dev.ndmf.localization;

namespace KusakaFactory.Declavatar2.Localization
{
    internal static class DeclavatarLocalization
    {
        internal static Localizer NdmfLocalizer
        {
            get
            {
                EnsureLocalizerInitialized();
                return _ndmfLocalizer;
            }
        }

        internal static UIElementLocalizer UILocalizer
        {
            get
            {
                EnsureLocalizerInitialized();
                return _uiLocalizer;
            }
        }

        internal static event Action OnNdmfLanguageChanged;

        private static Localizer _ndmfLocalizer = null;
        private static UIElementLocalizer _uiLocalizer = null;

        private static readonly ImmutableList<string> SupportedLanguages = ImmutableList<string>.Empty
            .Add("en-us")
            .Add("ja-jp");
        private static readonly ImmutableDictionary<string, string> LocalizationAssetGuids = ImmutableDictionary<string, string>.Empty
            .Add("en-us", "e1f8683069583b847b4880974eed67aa")
            .Add("ja-jp", "c635746f8e1571e4f8b95095be547e74");

        private static void EnsureLocalizerInitialized()
        {
            if (_ndmfLocalizer != null) return;

            _ndmfLocalizer = new Localizer(SupportedLanguages[0], () =>
            {
                var paths = LocalizationAssetGuids.Select((p) => AssetDatabase.GUIDToAssetPath(p.Value));
                return paths.Select((p) => AssetDatabase.LoadAssetAtPath<LocalizationAsset>(p)).ToList();
            });
            _uiLocalizer = new UIElementLocalizer(_ndmfLocalizer);
            LanguagePrefs.RegisterLanguageChangeCallback(typeof(DeclavatarLocalization), (_) => OnNdmfLanguageChanged?.Invoke());
        }
    }
}
