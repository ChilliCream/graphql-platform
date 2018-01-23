using System;
using System.Collections.Generic;
using System.Linq;

namespace Zeus.Abstractions
{
    internal static class TypeDefinitionMergeHelpers
    {
        public static IReadOnlyDictionary<string, FieldDefinition> MergeFields(IObjectTypeDefinition x, IObjectTypeDefinition y)
        {
            if (x.Name.Equals(y.Name, StringComparison.Ordinal))
            {
                throw new ArgumentException("The names of the two object type "
                    + "definitions have to match in order to merge them.",
                    nameof(x));
            }

            return MergeFields(x.Fields.Values, y.Fields.Values);
        }

        public static IReadOnlyDictionary<string, InputValueDefinition> MergeFields(InputObjectTypeDefinition x, InputObjectTypeDefinition y)
        {
            if (x.Name.Equals(y.Name, StringComparison.Ordinal))
            {
                throw new ArgumentException("The names of the two input object type "
                    + "definitions have to match in order to merge them.",
                    nameof(x));
            }

            return MergeFields(x.Fields.Values, y.Fields.Values);
        }

        private static IReadOnlyDictionary<string, T> MergeFields<T>(IEnumerable<T> x, IEnumerable<T> y)
            where T : IFieldDefinition
        {
            Dictionary<string, T> fields = x.ToDictionary(t => t.Name, StringComparer.Ordinal);
            foreach (T field in y)
            {
                fields[field.Name] = field;
            }
            return fields;
        }
    }
}
