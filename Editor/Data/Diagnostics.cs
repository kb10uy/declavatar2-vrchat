using System.Collections.Generic;

namespace KusakaFactory.Declavatar2.Data
{
    public enum DiagnosticStage : byte
    {
        Script = 0,
        Transform = 1,
    }

    public sealed class Diagnostics
    {
        public Diagnostics(DiagnosticStage stage, IReadOnlyList<Diagnostic> items)
        {
            Stage = stage;
            Items = items;
        }

        public DiagnosticStage Stage { get; }
        public IReadOnlyList<Diagnostic> Items { get; }
    }

    public sealed class Diagnostic
    {
        public Diagnostic(SourceLocation at, string message)
        {
            At = at;
            Message = message;
        }

        public SourceLocation At { get; }
        public string Message { get; }

        public override string ToString()
        {
            return At is null ? Message : $"{At}: {Message}";
        }
    }
}
