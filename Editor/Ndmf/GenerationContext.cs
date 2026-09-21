using System.Linq;
using KusakaFactory.Declavatar2.Data;
using KusakaFactory.Declavatar2.Resolution;
using KusakaFactory.Declavatar2.Runtime;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEditor;
using UnityEngine;
using Avatar = KusakaFactory.Declavatar2.Data.Avatar;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class GenerationContext
    {
        public GenerationContext(BuildContext build, DeclavatarDeclaration declaration, Avatar avatar)
        {
            Build = build;
            Declaration = declaration;
            Avatar = avatar;
            RelativeRoot = declaration.RelativePathRoot != null ? declaration.RelativePathRoot : declaration.gameObject;
            Resolver = new ExternalResolver(
                avatar.Externals,
                build.AvatarRootObject,
                RelativeRoot,
                DeclarationAssets.Entries(declaration)
            );
        }

        public BuildContext Build { get; }
        public AnimatorServicesContext Services => Build.Extension<AnimatorServicesContext>();
        public DeclavatarDeclaration Declaration { get; }
        public Avatar Avatar { get; }
        public GameObject RelativeRoot { get; }
        public ExternalResolver Resolver { get; }

        public static GameObject CreateChild(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        public string ObjectPath(PathMode mode, ObjectPathIndex index)
        {
            var target = Resolver.Object(mode, index);
            var root = mode == PathMode.Relative ? RelativeRoot : Build.AvatarRootObject;
            return target == null ? null : AnimationUtility.CalculateTransformPath(target.transform, root.transform);
        }

        public string VirtualPath(PathMode mode, string path)
        {
            var root = mode == PathMode.Relative ? RelativeRoot : Build.AvatarRootObject;
            var target = path.Length == 0 ? root.transform : root.transform.Find(path);
            var remapper = Services.ObjectPathRemapper;
            if (target != null) return remapper.GetVirtualPathForObject(target);
            var prefix = remapper.GetVirtualPathForObject(root);
            return prefix.Length == 0 ? path : prefix + "/" + path;
        }

        public void Report(ErrorSeverity severity, string key, params object[] args)
        {
            DeclavatarErrors.Report(severity, key, args.Concat(new object[] { Declaration }).ToArray());
        }

        public void FlushResolutionErrors()
        {
            foreach (var error in Resolver.Errors) DeclavatarErrors.ReportResolution(Declaration, error);
        }
    }
}
