using System;
using KusakaFactory.Declavatar2.Data;
using nadena.dev.modular_avatar.core;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class MenuGenerator
    {
        private readonly GenerationContext _context;

        public MenuGenerator(GenerationContext context)
        {
            _context = context;
        }

        public void Generate()
        {
            var items = _context.Avatar.Menu;
            if (items.Count == 0) return;

            var declaration = _context.Declaration;
            var root = declaration.gameObject;
            if (root.GetComponent<ModularAvatarMenuInstaller>() == null) root.AddComponent<ModularAvatarMenuInstaller>();

            var group = root.GetComponent<ModularAvatarMenuGroup>();
            GameObject parent;
            if (group == null)
            {
                group = root.AddComponent<ModularAvatarMenuGroup>();
                parent = GenerationContext.CreateChild(root, "Declavatar Menu");
                group.targetObject = parent;
            }
            else
            {
                parent = group.targetObject != null ? group.targetObject : root;
            }

            foreach (var item in items) Build(parent, item);
        }

        private void Build(GameObject parent, MenuItem item)
        {
            var target = GenerationContext.CreateChild(parent, item.Name);
            var component = target.AddComponent<ModularAvatarMenuItem>();
            var control = new VRCExpressionsMenu.Control
            {
                name = item.Name,
                parameter = Parameter(""),
                subParameters = Array.Empty<VRCExpressionsMenu.Control.Parameter>(),
                labels = Array.Empty<VRCExpressionsMenu.Control.Label>(),
            };

            switch (item)
            {
                case MenuItem.SubMenu subMenu:
                    control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                    component.MenuSource = SubmenuSource.Children;
                    foreach (var child in subMenu.Items) Build(target, child);
                    break;
                case MenuItem.Toggle toggle:
                    control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                    control.parameter = Parameter(toggle.Parameter);
                    control.value = Values.Scalar(toggle.Value);
                    break;
                case MenuItem.Button button:
                    control.type = VRCExpressionsMenu.Control.ControlType.Button;
                    control.parameter = Parameter(button.Parameter);
                    control.value = Values.Scalar(button.Value);
                    break;
                case MenuItem.Radial radial:
                    control.type = VRCExpressionsMenu.Control.ControlType.RadialPuppet;
                    control.subParameters = new[] { Parameter(radial.Parameter) };
                    break;
                case MenuItem.TwoAxis twoAxis:
                    control.type = VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet;
                    control.subParameters = new[] { Parameter(twoAxis.Horizontal.Parameter), Parameter(twoAxis.Vertical.Parameter) };
                    control.labels = new[]
                    {
                        Label(twoAxis.Vertical.Positive),
                        Label(twoAxis.Horizontal.Positive),
                        Label(twoAxis.Vertical.Negative),
                        Label(twoAxis.Horizontal.Negative),
                    };
                    break;
                case MenuItem.FourAxis fourAxis:
                    control.type = VRCExpressionsMenu.Control.ControlType.FourAxisPuppet;
                    control.subParameters = new[]
                    {
                        Parameter(fourAxis.Up.Parameter),
                        Parameter(fourAxis.Right.Parameter),
                        Parameter(fourAxis.Down.Parameter),
                        Parameter(fourAxis.Left.Parameter),
                    };
                    control.labels = new[]
                    {
                        Label(fourAxis.Up.Label),
                        Label(fourAxis.Right.Label),
                        Label(fourAxis.Down.Label),
                        Label(fourAxis.Left.Label),
                    };
                    break;
                default:
                    throw new InvalidOperationException($"unknown menu item {item}");
            }

            component.Control = control;
        }

        private static VRCExpressionsMenu.Control.Parameter Parameter(string name)
        {
            return new VRCExpressionsMenu.Control.Parameter { name = name ?? "" };
        }

        private static VRCExpressionsMenu.Control.Label Label(string name)
        {
            return new VRCExpressionsMenu.Control.Label { name = name ?? "" };
        }
    }
}
