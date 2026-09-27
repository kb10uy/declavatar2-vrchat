using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KusakaFactory.Declavatar2.Runtime;
using UnityEditor;
using UnityEngine;

namespace KusakaFactory.Declavatar2.Compilation
{
    public sealed class CompilationInput
    {
        private CompilationInput(
            string scriptPath,
            string chunkName,
            string source,
            IReadOnlyList<string> symbols,
            IReadOnlyList<string> moduleRootPaths,
            IReadOnlyList<string> libraryPaths
        )
        {
            ScriptPath = scriptPath;
            ChunkName = chunkName;
            Source = source;
            SourceHash = Hash128.Compute(source);
            Symbols = symbols;
            ModuleRootPaths = moduleRootPaths;
            LibraryPaths = libraryPaths;
            LibraryFingerprint = Fingerprint(libraryPaths);
            CacheKey = string.Join("\n", new[] { scriptPath, string.Join("\0", symbols), string.Join("\0", moduleRootPaths) });
        }

        public string ScriptPath { get; }
        public string ChunkName { get; }
        public string Source { get; }
        public Hash128 SourceHash { get; }
        public IReadOnlyList<string> Symbols { get; }
        public IReadOnlyList<string> ModuleRootPaths { get; }
        public IReadOnlyList<string> LibraryPaths { get; }
        public Hash128 LibraryFingerprint { get; }
        public string CacheKey { get; }

        public static CompilationInput From(DeclavatarDeclaration declaration)
        {
            if (declaration == null || declaration.Script == null) return null;

            var scriptPath = AssetDatabase.GetAssetPath(declaration.Script) ?? "";
            var chunkName = scriptPath.Length > 0 ? scriptPath : declaration.Script.name + ".lua";
            var symbols = (declaration.Symbols ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();
            var moduleRootPaths = (declaration.ModuleRoots ?? Array.Empty<UnityEngine.Object>())
                .Where(r => r != null)
                .Select(AssetDatabase.GetAssetPath)
                .Where(p => !string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p))
                .Distinct()
                .ToArray();

            var libraryPaths = new List<string>();
            if (scriptPath.Length > 0) libraryPaths.Add(Path.GetDirectoryName(PhysicalPath(scriptPath)));
            libraryPaths.AddRange(moduleRootPaths.Select(PhysicalPath));

            return new CompilationInput(scriptPath, chunkName, declaration.Script.text, symbols, moduleRootPaths, libraryPaths);
        }

        private static string PhysicalPath(string assetPath)
        {
            return Path.GetFullPath(FileUtil.GetPhysicalPath(assetPath));
        }

        private static Hash128 Fingerprint(IReadOnlyList<string> libraryPaths)
        {
            var hash = new Hash128();
            foreach (var root in libraryPaths.OrderBy(p => p, StringComparer.Ordinal))
            {
                hash.Append(root);
                if (!Directory.Exists(root)) continue;
                foreach (var file in Directory.EnumerateFiles(root, "*.lua", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
                {
                    var info = new FileInfo(file);
                    hash.Append(file);
                    hash.Append(info.LastWriteTimeUtc.Ticks.ToString());
                    hash.Append(info.Length.ToString());
                }
            }
            return hash;
        }
    }
}
