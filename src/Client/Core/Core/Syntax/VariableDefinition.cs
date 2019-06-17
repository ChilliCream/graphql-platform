using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Client.Core.Syntax
{
    public class VariableDefinition
    {
        public VariableDefinition(Type type, bool isNullable, string name)
        {
            Type = ToTypeName(type, isNullable);
            Name = name;
        }

        public string Type { get; }
        public string Name { get; }

        public static string ToTypeName(Type type, bool isNullable)
        {
            var name = type.Name;

            if (type == typeof(int))
            {
                name = "Int";
            }
            else if (type == typeof(double))
            {
                name = "Float";
            }
            else if (type == typeof(bool))
            {
                name = "Boolean";
            }
            else if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var inner = ToTypeName(type.GenericTypeArguments[0], false);
                name = '[' + inner + ']';
            }

            return name + (isNullable ? "" : "!");
        }
    }
}
