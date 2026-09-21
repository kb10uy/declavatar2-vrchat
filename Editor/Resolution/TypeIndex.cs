using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KusakaFactory.Declavatar2.Resolution
{
    public static class TypeIndex
    {
        private static Dictionary<string, Type> _components;
        private static Dictionary<string, Type> _behaviours;
        private static Dictionary<string, Type> _objects;

        public static Type FindComponent(string fullName)
        {
            return Find(ref _components, typeof(Component), fullName);
        }

        public static Type FindStateMachineBehaviour(string fullName)
        {
            return Find(ref _behaviours, typeof(StateMachineBehaviour), fullName);
        }

        public static Type FindObject(string fullName)
        {
            return Find(ref _objects, typeof(UnityEngine.Object), fullName);
        }

        private static Type Find(ref Dictionary<string, Type> index, Type baseType, string fullName)
        {
            if (index == null)
            {
                index = new Dictionary<string, Type>(StringComparer.Ordinal) { [baseType.FullName] = baseType };
                foreach (var type in TypeCache.GetTypesDerivedFrom(baseType))
                {
                    if (type.FullName != null && !index.ContainsKey(type.FullName)) index.Add(type.FullName, type);
                }
            }
            return fullName != null && index.TryGetValue(fullName, out var found) ? found : null;
        }
    }
}
