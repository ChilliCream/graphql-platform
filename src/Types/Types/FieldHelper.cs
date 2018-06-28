using System;
using System.Collections.Generic;
using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    internal static class FieldHelper
    {
        public static T ResolveFieldType<T>(
            this IField field,
            ITypeRegistry typeRegistry,
            Action<SchemaError> reportError,
            TypeReference typeReference)
            where T : IType
        {
            T type = default(T);
            if (typeReference != null)
            {
                type = typeRegistry.GetType<T>(typeReference);

            }

            if (ReferenceEquals(type, default(T)))
            {
                reportError(new SchemaError(
                    $"The type `{typeReference}` of field " +
                    $"`{field.DeclaringType.Name}.{field.Name}` could not be resolved " +
                    "to a valid schema type.", field.DeclaringType));
            }

            return type;
        }
    }
}
