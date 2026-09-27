using System.Collections.Generic;
using KusakaFactory.Declavatar2.Data;

namespace KusakaFactory.Declavatar2.Resolution
{
    public enum ResolutionErrorKind
    {
        ObjectNotFound,
        ComponentTypeNotFound,
        BehaviourTypeNotFound,
        AssetTypeUnknown,
        AssetNotFound,
        AssetAmbiguous,
        AssetTypeMismatch,
    }

    public sealed class ResolutionError
    {
        public ResolutionError(ResolutionErrorKind kind, IReadOnlyList<string> arguments, IReadOnlyList<SourceLocation> referencedAt)
        {
            Kind = kind;
            Arguments = arguments;
            ReferencedAt = referencedAt;
        }

        public ResolutionErrorKind Kind { get; }
        public IReadOnlyList<string> Arguments { get; }
        public IReadOnlyList<SourceLocation> ReferencedAt { get; }

        public string Locations => ReferencedAt.Count == 0 ? "?" : string.Join(", ", ReferencedAt);
    }
}
