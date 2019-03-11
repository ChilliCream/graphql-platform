using System;
using HotChocolate.Properties;

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

            T type = default;
            if (typeReference != null)
            {
                type = context.GetType<T>(typeReference);
            }

            if (ReferenceEquals(type, default(T)))
            {
                type = context.GetType<T>(typeReference);
                INamedType namedType = field.DeclaringType as INamedType;
                context.ReportError(new SchemaError(
                    TypeResourceHelper.Field_Cannot_ResolveType(
                        field.DeclaringType.Name,
                        field.Name,
                        typeReference),
                    namedType));
            }

            return type;
        }
    }
}
