using System.Collections.Generic;

namespace KusakaFactory.Declavatar2.Data
{
    public sealed class Avatar
    {
        public Avatar(
            Externals externals,
            IReadOnlyList<ExpressionParameter> expressionParameters,
            IReadOnlyList<PlayableController> controllers,
            IReadOnlyList<MenuItem> menu)
        {
            Externals = externals;
            ExpressionParameters = expressionParameters;
            Controllers = controllers;
            Menu = menu;
        }

        public Externals Externals { get; }
        public IReadOnlyList<ExpressionParameter> ExpressionParameters { get; }
        public IReadOnlyList<PlayableController> Controllers { get; }
        public IReadOnlyList<MenuItem> Menu { get; }
    }
}
