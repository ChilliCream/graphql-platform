using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Utilities
{
    internal static class ObjectVariableRewriter
    {
        public static object RewriteVariable(
            ITypeConversion conversion,
            NameString schemaName,
            IInputType type,
            object value)
        {
            return RewriteVariableValue(conversion, in schemaName, type, value);
        }

        private static IDictionary<string, object> RewriteVariableObject(
            ITypeConversion conversion,
            in NameString schemaName,
            InputObjectType type,
            IDictionary<string, object> dict)
        {
            foreach (IInputField field in type.Fields)
            {
                if (field.TryGetSourceDirective(schemaName,
                    out SourceDirective sourceDirective)
                    && !sourceDirective.Name.Equals(field.Name)
                    && dict.TryGetValue(field.Name, out object o))
                {
                    dict.Remove(field.Name);
                    dict.Add(sourceDirective.Name, o);
                }
            }
            return dict;
        }

        private static IList<object> RewriteVariableList(
            ITypeConversion conversion,
            in NameString schemaName,
            ListType type,
            IList<object> list)
        {
            INamedType namedType = type.NamedType();

            for (int i = 0; i < list.Count; i++)
            {
                list[i] = RewriteVariableValue(
                    conversion, schemaName, namedType, list[i]);
            }
            return list;
        }

        private static object RewriteVariableValue(
            ITypeConversion conversion,
            in NameString schemaName,
            IType type,
            object value)
        {
            if (type.IsListType() && value is IList<object> list)
            {
                return RewriteVariableList(
                    conversion, in schemaName, type.ListType(), list);
            }
            else if (type.NamedType() is InputObjectType inputObject
                && value is IDictionary<string, object> dict)
            {
                return RewriteVariableObject(
                    conversion, in schemaName, inputObject, dict);
            }
            else if (type.NamedType() is ISerializableType s
                && type.NamedType() is IHasClrType c)
            {
                if (!c.ClrType.IsInstanceOfType(value)
                    && conversion.TryConvert(
                        typeof(object), c.ClrType,
                        value, out object converted))
                {
                    return s.Serialize(converted);
                }
                else
                {
                    return s.Serialize(value);
                }
            }
            else
            {
                throw new NotSupportedException(
                    "The type is not supported.");
            }
        }
    }
}
