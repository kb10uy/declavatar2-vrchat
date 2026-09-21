using System;
using KusakaFactory.Declavatar2.Data;
using nadena.dev.ndmf;
using UnityEditor;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class GenericValueWriter
    {
        private readonly GenerationContext _context;
        private readonly string _typeName;

        public GenericValueWriter(GenerationContext context, string typeName)
        {
            _context = context;
            _typeName = typeName;
        }

        public void Write(SerializedProperty property, GenericValue value)
        {
            switch (value)
            {
                case GenericValue.Bool b when property.propertyType == SerializedPropertyType.Boolean:
                    property.boolValue = b.Value;
                    return;
                case GenericValue.Int i when property.propertyType == SerializedPropertyType.Integer:
                    property.longValue = i.Value;
                    return;
                case GenericValue.Int i when property.propertyType == SerializedPropertyType.Enum:
                    property.intValue = (int)i.Value;
                    return;
                case GenericValue.Int i when property.propertyType == SerializedPropertyType.Float:
                    property.doubleValue = i.Value;
                    return;
                case GenericValue.Float f when property.propertyType == SerializedPropertyType.Float:
                    property.doubleValue = f.Value;
                    return;
                case GenericValue.String s when property.propertyType == SerializedPropertyType.String:
                    property.stringValue = s.Value;
                    return;
                case GenericValue.String s when property.propertyType == SerializedPropertyType.Enum:
                    var index = Array.IndexOf(property.enumNames, s.Value);
                    if (index >= 0)
                    {
                        property.enumValueIndex = index;
                        return;
                    }
                    break;
                case GenericValue.List list when property.isArray && property.propertyType != SerializedPropertyType.String:
                    property.arraySize = list.Values.Count;
                    for (var i = 0; i < list.Values.Count; i++) Write(property.GetArrayElementAtIndex(i), list.Values[i]);
                    return;
                case GenericValue.Map map when property.propertyType == SerializedPropertyType.Generic && !property.isArray:
                    foreach (var field in map.Values)
                    {
                        var child = property.FindPropertyRelative(field.Key);
                        if (child == null)
                        {
                            _context.Report(ErrorSeverity.Error, "declavatar2.generate.behaviour_field", _typeName, property.propertyPath + "." + field.Key);
                            continue;
                        }
                        Write(child, field.Value);
                    }
                    return;
            }

            _context.Report(ErrorSeverity.Error, "declavatar2.generate.behaviour_value", _typeName, property.propertyPath, Describe(value));
        }

        private static string Describe(GenericValue value)
        {
            switch (value)
            {
                case GenericValue.Bool b: return b.Value ? "true" : "false";
                case GenericValue.Int i: return i.Value.ToString();
                case GenericValue.Float f: return f.Value.ToString("R");
                case GenericValue.String s: return $"\"{s.Value}\"";
                case GenericValue.List l: return $"a list of {l.Values.Count}";
                case GenericValue.Map m: return $"a map of {m.Values.Count}";
                default: return value.ToString();
            }
        }
    }
}
