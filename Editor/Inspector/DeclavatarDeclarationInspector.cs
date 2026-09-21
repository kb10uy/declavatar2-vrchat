using System;
using System.Collections.Generic;
using System.Linq;
using KusakaFactory.Declavatar2.Compilation;
using KusakaFactory.Declavatar2.Data;
using KusakaFactory.Declavatar2.Localization;
using KusakaFactory.Declavatar2.Resolution;
using KusakaFactory.Declavatar2.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace KusakaFactory.Declavatar2.Inspector
{
    [CustomEditor(typeof(DeclavatarDeclaration))]
    internal sealed class DeclavatarDeclarationInspector : Editor
    {
        private const string InspectorUxmlGuid = "ac8c3f41d05705e46b423754cacc6eb3";
        private const float ExternItemHeight = 20.0f;

        private VisualElement _boundElementRoot;
        private HelpBox _statusBox;
        private VisualElement _diagnosticsContainer;
        private ObjectField _relativePathRootField;
        private VisualElement _assetExternsSection;
        private ListView _assetExternsList;

        private CompiledDeclaration _compiled;
        private string _failure;
        private string _inputSignature;
        private readonly List<AssetExtern> _assetExterns = new List<AssetExtern>();

        public override VisualElement CreateInspectorGUI()
        {
            if (_boundElementRoot == null)
            {
                _boundElementRoot = new VisualElement();
                DeclavatarLocalization.OnNdmfLanguageChanged += RebuildUI;
            }
            _boundElementRoot.Clear();
            _boundElementRoot.Add(CreateInspectorGUIImpl());
            return _boundElementRoot;
        }

        private void OnDestroy()
        {
            DeclavatarLocalization.OnNdmfLanguageChanged -= RebuildUI;
        }

        private void RebuildUI()
        {
            CreateInspectorGUI();
        }

        private VisualElement CreateInspectorGUIImpl()
        {
            var visualTree = DeclavatarResources.LoadVisualTreeByGuid(InspectorUxmlGuid);
            var inspector = visualTree.CloneTree();
            DeclavatarLocalization.UILocalizer.ApplyLocalizationFor(inspector);
            inspector.Bind(serializedObject);

            _statusBox = inspector.Q<HelpBox>("BoxStatus");
            _diagnosticsContainer = inspector.Q<VisualElement>("ContainerDiagnostics");
            _relativePathRootField = inspector.Q<ObjectField>("FieldRelativePathRoot");
            _assetExternsSection = inspector.Q<VisualElement>("SectionAssetExterns");

            _assetExternsList = inspector.Q<ListView>("FieldAssetExterns");
            _assetExternsList.fixedItemHeight = ExternItemHeight;
            _assetExternsList.selectionType = SelectionType.None;
            _assetExternsList.itemsSource = _assetExterns;
            _assetExternsList.makeItem = MakeAssetExternItem;
            _assetExternsList.bindItem = BindAssetExternItem;

            inspector.Q<Button>("ButtonRecompile").clicked += () =>
            {
                DeclarationCache.Invalidate((DeclavatarDeclaration)target);
                Refresh(true);
            };
            inspector.TrackSerializedObjectValue(serializedObject, (_) => Refresh(false));

            Refresh(true);
            return inspector;
        }

        private void Refresh(bool force)
        {
            var declaration = (DeclavatarDeclaration)target;
            var signature = InputSignature(declaration);
            if (!force && signature == _inputSignature) return;
            _inputSignature = signature;

            _compiled = null;
            _failure = null;
            try
            {
                _compiled = DeclarationCache.GetOrCompile(declaration);
            }
            catch (Exception e)
            {
                _failure = e.Message;
            }
            UpdateStatus();
            UpdateExterns(declaration);
        }

        private static string InputSignature(DeclavatarDeclaration declaration)
        {
            var script = InstanceId(declaration.Script);
            var symbols = string.Join("\0", declaration.Symbols ?? Array.Empty<string>());
            var moduleRoots = string.Join("\0", (declaration.ModuleRoots ?? Array.Empty<UnityEngine.Object>()).Select(InstanceId));
            var dictionaries = string.Join("\0", (declaration.AssetDictionaries ?? Array.Empty<DeclavatarAssetDictionary>()).Select(InstanceId));
            var overrides = string.Join("\0", (declaration.AssetOverrides ?? Array.Empty<DeclavatarAssetDictionary.Entry>())
                .Select(entry => $"{entry.Name}={InstanceId(entry.Asset)}"));
            return string.Join("\n", new[] { script, symbols, moduleRoots, dictionaries, overrides });
        }

        private static string InstanceId(UnityEngine.Object asset)
        {
            return asset != null ? asset.GetInstanceID().ToString() : "";
        }

        private void UpdateStatus()
        {
            _diagnosticsContainer.Clear();
            if (_failure != null)
            {
                SetStatus(HelpBoxMessageType.Error, _failure);
            }
            else if (_compiled == null)
            {
                SetStatus(HelpBoxMessageType.Info, Localized("declavatar2.inspector.status-no-script"));
            }
            else if (!_compiled.Succeeded)
            {
                var stage = Localized(_compiled.Diagnostics.Stage == DiagnosticStage.Script
                    ? "declavatar2.inspector.stage-script"
                    : "declavatar2.inspector.stage-transform");
                SetStatus(HelpBoxMessageType.Error, Format("declavatar2.inspector.status-failed", stage, _compiled.Diagnostics.Items.Count));
                foreach (var item in _compiled.Diagnostics.Items) _diagnosticsContainer.Add(WrappedLabel(item.ToString()));
            }
            else
            {
                var avatar = _compiled.Avatar;
                SetStatus(HelpBoxMessageType.Info, Format(
                    "declavatar2.inspector.status-compiled",
                    avatar.ExpressionParameters.Count,
                    avatar.Controllers.Count,
                    avatar.Controllers.Sum(controller => controller.Layers.Count),
                    avatar.Menu.Count
                ));
            }
        }

        private void UpdateExterns(DeclavatarDeclaration declaration)
        {
            _assetExterns.Clear();
            var externals = _compiled != null && _compiled.Succeeded ? _compiled.Avatar.Externals : null;
            SetVisible(_relativePathRootField, externals != null && externals.NeedsRelativeRoot);

            if (externals != null)
            {
                var resolver = new ExternalResolver(externals, declaration.gameObject, declaration.gameObject, DeclarationAssets.Entries(declaration));
                for (var index = 0; index < externals.Assets.Count; ++index)
                {
                    _assetExterns.Add(new AssetExtern(externals.Assets[index], resolver.Asset(new AssetIndex(index))));
                }
            }

            SetVisible(_assetExternsSection, _assetExterns.Count > 0);
            _assetExternsList.style.height = _assetExterns.Count * ExternItemHeight + 2.0f;
            _assetExternsList.Rebuild();
        }

        private VisualElement MakeAssetExternItem()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var state = new Label { name = "LabelState" };
            state.style.width = 64.0f;
            state.style.flexShrink = 0.0f;
            state.style.opacity = 0.6f;
            row.Add(state);

            var key = new Label { name = "LabelKey" };
            key.style.width = Length.Percent(30.0f);
            key.style.flexShrink = 0.0f;
            key.style.overflow = Overflow.Hidden;
            key.style.textOverflow = TextOverflow.Ellipsis;
            row.Add(key);

            var field = new ObjectField { name = "FieldAsset", allowSceneObjects = false };
            field.style.flexGrow = 1.0f;
            field.style.marginRight = 0.0f;
            field.RegisterValueChangedCallback((changed) => OnAssetExternChanged(row, changed.newValue));
            row.Add(field);

            return row;
        }

        private void BindAssetExternItem(VisualElement row, int index)
        {
            var item = _assetExterns[index];
            row.userData = index;

            var state = row.Q<Label>("LabelState");
            state.text = Localized(StateKey(item));

            var key = row.Q<Label>("LabelKey");
            key.text = item.Key;
            key.tooltip = Describe(item);

            var field = row.Q<ObjectField>("FieldAsset");
            field.objectType = item.AssetType;
            field.tooltip = Describe(item);
            field.SetValueWithoutNotify(item.Resolved);
            field.SetEnabled(item.Named != null);
        }

        private void OnAssetExternChanged(VisualElement row, UnityEngine.Object value)
        {
            if (!(row.userData is int index) || index >= _assetExterns.Count) return;

            var item = _assetExterns[index];
            if (item.Named == null) return;

            item.Resolved = value;
            row.Q<Label>("LabelState").text = Localized(StateKey(item));
            SetAssetOverride(item.Named.Name, value);
        }

        private void SetAssetOverride(string name, UnityEngine.Object asset)
        {
            var overrides = serializedObject.FindProperty(nameof(DeclavatarDeclaration.AssetOverrides));
            serializedObject.Update();

            var existing = -1;
            for (var index = 0; index < overrides.arraySize; ++index)
            {
                var element = overrides.GetArrayElementAtIndex(index);
                if (element.FindPropertyRelative(nameof(DeclavatarAssetDictionary.Entry.Name)).stringValue == name)
                {
                    existing = index;
                    break;
                }
            }

            if (asset == null)
            {
                if (existing >= 0) overrides.DeleteArrayElementAtIndex(existing);
            }
            else
            {
                if (existing < 0)
                {
                    existing = overrides.arraySize;
                    overrides.InsertArrayElementAtIndex(existing);
                    overrides.GetArrayElementAtIndex(existing).FindPropertyRelative(nameof(DeclavatarAssetDictionary.Entry.Name)).stringValue = name;
                }
                overrides.GetArrayElementAtIndex(existing).FindPropertyRelative(nameof(DeclavatarAssetDictionary.Entry.Asset)).objectReferenceValue = asset;
            }
            serializedObject.ApplyModifiedProperties();
        }

        private string StateKey(AssetExtern item)
        {
            if (item.Resolved == null) return "declavatar2.inspector.extern-unresolved";
            if (item.Named == null) return "declavatar2.inspector.extern-auto";

            var declaration = (DeclavatarDeclaration)target;
            var overridden = (declaration.AssetOverrides ?? Array.Empty<DeclavatarAssetDictionary.Entry>())
                .Any(entry => entry.Name == item.Named.Name && entry.Asset != null);
            return overridden ? "declavatar2.inspector.extern-overridden" : "declavatar2.inspector.extern-auto";
        }

        private void SetStatus(HelpBoxMessageType type, string message)
        {
            _statusBox.messageType = type;
            _statusBox.text = message;
        }

        private static Label WrappedLabel(string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static string Describe(AssetExtern item)
        {
            var locations = item.Entry.ReferencedAt.Count > 0 ? string.Join(", ", item.Entry.ReferencedAt) : "?";
            return $"{ExternalResolver.Describe(item.Entry.Value)}\n{locations}";
        }

        private static string Localized(string key)
        {
            return DeclavatarLocalization.NdmfLocalizer.GetLocalizedString(key);
        }

        private static string Format(string key, params object[] args)
        {
            return string.Format(Localized(key), args);
        }

        private sealed class AssetExtern
        {
            public AssetExtern(ExternEntry<AssetLocator> entry, UnityEngine.Object resolved)
            {
                Entry = entry;
                Named = entry.Value as AssetLocator.Named;
                AssetType = Named != null ? TypeIndex.FindObject(Named.AssetType) ?? typeof(UnityEngine.Object) : typeof(UnityEngine.Object);
                Key = Named != null ? Named.Name : ExternalResolver.Describe(entry.Value);
                Resolved = resolved;
            }

            public ExternEntry<AssetLocator> Entry { get; }
            public AssetLocator.Named Named { get; }
            public Type AssetType { get; }
            public string Key { get; }
            public UnityEngine.Object Resolved { get; set; }
        }
    }
}
