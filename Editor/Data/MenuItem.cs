using System.Collections.Generic;

namespace KusakaFactory.Declavatar2.Data
{
    public abstract class MenuItem
    {
        private MenuItem(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public sealed class SubMenu : MenuItem
        {
            public SubMenu(string name, IReadOnlyList<MenuItem> items) : base(name)
            {
                Items = items;
            }

            public IReadOnlyList<MenuItem> Items { get; }
        }

        public sealed class Toggle : MenuItem
        {
            public Toggle(string name, string parameter, AnimatedValue value) : base(name)
            {
                Parameter = parameter;
                Value = value;
            }

            public string Parameter { get; }
            public AnimatedValue Value { get; }
        }

        public sealed class Button : MenuItem
        {
            public Button(string name, string parameter, AnimatedValue value) : base(name)
            {
                Parameter = parameter;
                Value = value;
            }

            public string Parameter { get; }
            public AnimatedValue Value { get; }
        }

        public sealed class Radial : MenuItem
        {
            public Radial(string name, string parameter) : base(name)
            {
                Parameter = parameter;
            }

            public string Parameter { get; }
        }

        public sealed class TwoAxis : MenuItem
        {
            public TwoAxis(string name, MenuAxis horizontal, MenuAxis vertical) : base(name)
            {
                Horizontal = horizontal;
                Vertical = vertical;
            }

            public MenuAxis Horizontal { get; }
            public MenuAxis Vertical { get; }
        }

        public sealed class FourAxis : MenuItem
        {
            public FourAxis(string name, MenuDirection up, MenuDirection down, MenuDirection left, MenuDirection right) : base(name)
            {
                Up = up;
                Down = down;
                Left = left;
                Right = right;
            }

            public MenuDirection Up { get; }
            public MenuDirection Down { get; }
            public MenuDirection Left { get; }
            public MenuDirection Right { get; }
        }
    }

    public sealed class MenuAxis
    {
        public MenuAxis(string parameter, string positive, string negative)
        {
            Parameter = parameter;
            Positive = positive;
            Negative = negative;
        }

        public string Parameter { get; }
        public string Positive { get; }
        public string Negative { get; }
    }

    public sealed class MenuDirection
    {
        public MenuDirection(string parameter, string label)
        {
            Parameter = parameter;
            Label = label;
        }

        public string Parameter { get; }
        public string Label { get; }
    }
}
