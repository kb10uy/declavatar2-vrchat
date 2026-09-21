using System;
using System.Collections.Generic;
using System.Linq;
using KusakaFactory.Declavatar2.Data;
using KusakaFactory.Declavatar2.Runtime;
using UnityEditor;
using UnityEngine;

namespace KusakaFactory.Declavatar2.Resolution
{
    public sealed class ExternalResolver
    {
        private readonly Externals _externals;
        private readonly Type[] _componentTypes;
        private readonly UnityEngine.Object[] _assets;
        private readonly Dictionary<(PathMode, int), GameObject> _objects = new Dictionary<(PathMode, int), GameObject>();
        private readonly HashSet<(int, Type)> _reportedMismatches = new HashSet<(int, Type)>();
        private readonly List<ResolutionError> _errors = new List<ResolutionError>();

        public ExternalResolver(Externals externals, GameObject absoluteRoot, GameObject relativeRoot, IReadOnlyList<DeclavatarAssetDictionary> dictionaries)
        {
            _externals = externals;
            AbsoluteRoot = absoluteRoot;
            RelativeRoot = relativeRoot;
            _componentTypes = externals.ComponentTypes.Select(ResolveComponentType).ToArray();
            _assets = externals.Assets.Select(entry => ResolveAsset(entry, dictionaries)).ToArray();
        }

        public GameObject AbsoluteRoot { get; }
        public GameObject RelativeRoot { get; }
        public IReadOnlyList<ResolutionError> Errors => _errors;

        public Type ComponentType(ComponentTypeIndex index)
        {
            return _componentTypes[index.Index];
        }

        public UnityEngine.Object Asset(AssetIndex index)
        {
            return _assets[index.Index];
        }

        public T Asset<T>(AssetIndex index) where T : UnityEngine.Object
        {
            var asset = _assets[index.Index];
            if (asset == null) return null;
            if (asset is T typed) return typed;
            if (_reportedMismatches.Add((index.Index, typeof(T))))
            {
                var entry = _externals[index];
                Error(ResolutionErrorKind.AssetTypeMismatch, entry.ReferencedAt, Describe(entry.Value), asset.GetType().FullName, typeof(T).FullName);
            }
            return null;
        }

        public GameObject Object(PathMode mode, ObjectPathIndex index)
        {
            var key = (mode, index.Index);
            if (_objects.TryGetValue(key, out var cached)) return cached;

            var root = mode == PathMode.Relative ? RelativeRoot : AbsoluteRoot;
            var entry = _externals[index];
            var transform = entry.Value.Length == 0 ? root.transform : root.transform.Find(entry.Value);
            var found = transform != null ? transform.gameObject : null;
            if (found == null)
            {
                Error(ResolutionErrorKind.ObjectNotFound, entry.ReferencedAt, entry.Value, root.name, mode == PathMode.Relative ? "relative" : "absolute");
            }
            _objects[key] = found;
            return found;
        }

        private Type ResolveComponentType(ExternEntry<string> entry)
        {
            var type = TypeIndex.FindComponent(entry.Value);
            if (type == null) Error(ResolutionErrorKind.ComponentTypeNotFound, entry.ReferencedAt, entry.Value);
            return type;
        }

        private UnityEngine.Object ResolveAsset(ExternEntry<AssetLocator> entry, IReadOnlyList<DeclavatarAssetDictionary> dictionaries)
        {
            switch (entry.Value)
            {
                case AssetLocator.Guid guid:
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid.Value);
                    var asset = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadMainAssetAtPath(path);
                    if (asset == null) Error(ResolutionErrorKind.AssetNotFound, entry.ReferencedAt, Describe(entry.Value));
                    return asset;
                }
                case AssetLocator.Path path:
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(path.Value);
                    if (asset == null) Error(ResolutionErrorKind.AssetNotFound, entry.ReferencedAt, Describe(entry.Value));
                    return asset;
                }
                case AssetLocator.Named named:
                    return ResolveNamed(entry, named, dictionaries);
                default:
                    throw new InvalidOperationException($"unknown asset locator {entry.Value}");
            }
        }

        private UnityEngine.Object ResolveNamed(ExternEntry<AssetLocator> entry, AssetLocator.Named named, IReadOnlyList<DeclavatarAssetDictionary> dictionaries)
        {
            var type = TypeIndex.FindObject(named.AssetType);
            if (type == null)
            {
                Error(ResolutionErrorKind.AssetTypeUnknown, entry.ReferencedAt, named.AssetType, named.Name);
                return null;
            }

            foreach (var dictionary in dictionaries)
            {
                if (dictionary == null || dictionary.Entries == null) continue;
                foreach (var candidate in dictionary.Entries)
                {
                    if (candidate.Name == named.Name && candidate.Asset != null && type.IsInstanceOfType(candidate.Asset)) return candidate.Asset;
                }
            }

            var found = new List<UnityEngine.Object>();
            foreach (var guid in AssetDatabase.FindAssets($"{named.Name} t:{type.Name}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset != null && asset.name == named.Name && type.IsInstanceOfType(asset)) found.Add(asset);
                }
            }
            if (found.Count == 1) return found[0];

            if (found.Count == 0)
            {
                Error(ResolutionErrorKind.AssetNotFound, entry.ReferencedAt, Describe(named));
            }
            else
            {
                Error(ResolutionErrorKind.AssetAmbiguous, entry.ReferencedAt, Describe(named), found.Count.ToString());
            }
            return null;
        }

        private void Error(ResolutionErrorKind kind, IReadOnlyList<SourceLocation> referencedAt, params string[] arguments)
        {
            _errors.Add(new ResolutionError(kind, arguments, referencedAt));
        }

        public static string Describe(AssetLocator locator)
        {
            switch (locator)
            {
                case AssetLocator.Guid guid: return $"guid {guid.Value}";
                case AssetLocator.Path path: return $"path {path.Value}";
                case AssetLocator.Named named: return $"{named.AssetType} `{named.Name}`";
                default: return locator.ToString();
            }
        }
    }
}
