using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Utilities
{
    internal static class ObjectVariableRewriter
    {
        public static object RewriteVariable(
            NameString schemaName,
            IInputType type,
            object value)
        {
            return RewriteVariableValue(in schemaName, type, value);
        }

        private static IDictionary<string, object> RewriteVariableObject(
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
            in NameString schemaName,
            ListType type,
            IList<object> list)
        {
            INamedType namedType = type.NamedType();

            for (int i = 0; i < list.Count; i++)
            {
                list[i] = RewriteVariableValue(
                    schemaName, namedType, list[i]);
            }
            return list;
        }

        private static object RewriteVariableValue(
            in NameString schemaName,
            IType type,
            object value)
        {
            if (type.IsListType() && value is IList<object> list)
            {
                return RewriteVariableList(
                    in schemaName, type.ListType(), list);
            }
            else if (type.NamedType() is InputObjectType inputObject
                && value is IDictionary<string, object> dict)
            {
                return RewriteVariableObject(
                    in schemaName, inputObject, dict);
            }
            else
            {
                return value;
            }
        }
    }
}
