using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Introspection
{
    internal static partial class IntrospectionTypes
    {
        private const string _schemaName = "__Schema";
        private const string _typeName = "__Type";
        private const string _typeKindName = "__TypeKind";
        private const string _fieldName = "__Field";
        private const string _enumValueName = "__EnumValue";
        private const string _directiveName = "__Directive";
        private const string _directiveLocationName = "__DirectiveLocation";
        private const string _inputValueName = "__InputValue";

        private static bool Contains<T>(IReadOnlyCollection<T> collection, T item)
        {
            foreach (T element in collection)
            {
                if (element.Equals(item))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
