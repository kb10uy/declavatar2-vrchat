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

        private VisualElement _boundElementRoot;
        private HelpBox _statusBox;
        private VisualElement _diagnosticsContainer;
        private Foldout _externsFoldout;
        private ObjectField _relativePathRootField;
        private VisualElement _assetExternsSection;
        private VisualElement _assetExternsContainer;
        private VisualElement _objectExternsSection;
        private VisualElement _objectExternsContainer;
        private VisualElement _componentExternsSection;
        private VisualElement _componentExternsContainer;

        private CompiledDeclaration _compiled;
        private string _failure;
        private string _inputSignature;

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
            _externsFoldout = inspector.Q<Foldout>("FoldoutExterns");
            _relativePathRootField = inspector.Q<ObjectField>("FieldRelativePathRoot");
            _assetExternsSection = inspector.Q<VisualElement>("SectionAssetExterns");
            _assetExternsContainer = inspector.Q<VisualElement>("ContainerAssetExterns");
            _objectExternsSection = inspector.Q<VisualElement>("SectionObjectExterns");
            _objectExternsContainer = inspector.Q<VisualElement>("ContainerObjectExterns");
            _componentExternsSection = inspector.Q<VisualElement>("SectionComponentExterns");
            _componentExternsContainer = inspector.Q<VisualElement>("ContainerComponentExterns");

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
            _assetExternsContainer.Clear();
            _objectExternsContainer.Clear();
            _componentExternsContainer.Clear();

            var externals = _compiled != null && _compiled.Succeeded ? _compiled.Avatar.Externals : null;
            SetVisible(_externsFoldout, externals != null);
            if (externals == null) return;

            SetVisible(_relativePathRootField, externals.NeedsRelativeRoot);

            var resolver = new ExternalResolver(externals, declaration.gameObject, declaration.gameObject, DeclarationAssets.Entries(declaration));
            for (var index = 0; index < externals.Assets.Count; ++index)
            {
                _assetExternsContainer.Add(CreateAssetExternRow(externals.Assets[index], resolver.Asset(new AssetIndex(index))));
            }
            foreach (var entry in externals.ObjectPaths)
            {
                var path = entry.Value.Length > 0 ? entry.Value : Localized("declavatar2.inspector.extern-object-root");
                _objectExternsContainer.Add(CreateReadOnlyExternRow(path, entry.ReferencedAt, null));
            }
            foreach (var entry in externals.ComponentTypes)
            {
                var stateKey = TypeIndex.FindComponent(entry.Value) == null ? "declavatar2.inspector.extern-unresolved" : null;
                _componentExternsContainer.Add(CreateReadOnlyExternRow(entry.Value, entry.ReferencedAt, stateKey));
            }

            SetVisible(_assetExternsSection, externals.Assets.Count > 0);
            SetVisible(_objectExternsSection, externals.ObjectPaths.Count > 0);
            SetVisible(_componentExternsSection, externals.ComponentTypes.Count > 0);
        }

        private VisualElement CreateAssetExternRow(ExternEntry<AssetLocator> entry, UnityEngine.Object resolved)
        {
            var named = entry.Value as AssetLocator.Named;
            var row = CreateRow();
            var field = new ObjectField(named != null ? named.Name : ExternalResolver.Describe(entry.Value))
            {
                objectType = named != null ? TypeIndex.FindObject(named.AssetType) ?? typeof(UnityEngine.Object) : typeof(UnityEngine.Object),
                allowSceneObjects = false,
                value = resolved,
                tooltip = Describe(entry.Value, entry.ReferencedAt),
            };
            field.style.flexGrow = 1.0f;
            row.Add(field);

            var tag = CreateTag(ExternStateKey(named, resolved));
            row.Add(tag);

            if (named != null)
            {
                field.RegisterValueChangedCallback((changed) =>
                {
                    SetAssetOverride(named.Name, changed.newValue);
                    tag.text = Localized(ExternStateKey(named, changed.newValue));
                });
            }
            else
            {
                field.SetEnabled(false);
            }
            return row;
        }

        private VisualElement CreateReadOnlyExternRow(string text, IReadOnlyList<SourceLocation> referencedAt, string stateKey)
        {
            var row = CreateRow();
            var label = WrappedLabel(text);
            label.tooltip = Locations(referencedAt);
            label.style.flexGrow = 1.0f;
            row.Add(label);
            if (stateKey != null) row.Add(CreateTag(stateKey));
            return row;
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

        private string ExternStateKey(AssetLocator.Named named, UnityEngine.Object value)
        {
            if (value == null) return "declavatar2.inspector.extern-unresolved";
            if (named == null) return "declavatar2.inspector.extern-auto";

            var declaration = (DeclavatarDeclaration)target;
            var overridden = (declaration.AssetOverrides ?? Array.Empty<DeclavatarAssetDictionary.Entry>())
                .Any(entry => entry.Name == named.Name && entry.Asset != null);
            return overridden ? "declavatar2.inspector.extern-overridden" : "declavatar2.inspector.extern-auto";
        }

        private void SetStatus(HelpBoxMessageType type, string message)
        {
            _statusBox.messageType = type;
            _statusBox.text = message;
        }

        private static Label CreateTag(string stateKey)
        {
            var tag = new Label(Localized(stateKey));
            tag.style.opacity = 0.6f;
            tag.style.unityTextAlign = TextAnchor.MiddleLeft;
            tag.style.marginLeft = 4.0f;
            return tag;
        }

        private static VisualElement CreateRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            return row;
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

        private static string Describe(AssetLocator locator, IReadOnlyList<SourceLocation> referencedAt)
        {
            return $"{ExternalResolver.Describe(locator)}\n{Locations(referencedAt)}";
        }

        private static string Locations(IReadOnlyList<SourceLocation> referencedAt)
        {
            return referencedAt.Count > 0 ? string.Join(", ", referencedAt) : "?";
        }

        private static string Localized(string key)
        {
            return DeclavatarLocalization.NdmfLocalizer.GetLocalizedString(key);
        }

        private static string Format(string key, params object[] args)
        {
            return string.Format(Localized(key), args);
        }
    }
}
