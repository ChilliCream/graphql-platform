using System;

namespace HotChocolate.Types
{
    internal static class FieldHelper
    {
        public static T ResolveFieldType<T>(
            this ITypeInitializationContext context,
            IField field,
            TypeReference typeReference)
            where T : IType
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            T type = default(T);
            if (typeReference != null)
            {
                type = context.GetType<T>(typeReference);
            }

            if (ReferenceEquals(type, default(T)))
            {
                context.ReportError(new SchemaError(
                    TypeResources.Field_Cannot_ResolveType(
                        field.DeclaringType.Name,
                        field.Name,
                        typeReference),
                    field.DeclaringType));
            }

            return type;
        }
    }
}
