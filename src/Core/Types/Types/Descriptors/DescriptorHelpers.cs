using System;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    internal static class DescriptorHelpers
    {
        internal static T ExecuteFactory<T>(
            Func<T> descriptionFactory)
        {
            if (descriptionFactory == null)
            {
                throw new ArgumentNullException(nameof(descriptionFactory));
            }

            return descriptionFactory();
        }

        internal static void AcquireNonNullStatus(
            this FieldDescriptionBase fieldDescription,
            MemberInfo member)
        {
            if (member.IsDefined(typeof(GraphQLNonNullTypeAttribute)))
            {
                var attribute =
                    member.GetCustomAttribute<GraphQLNonNullTypeAttribute>();
                fieldDescription.IsNullable = attribute.IsNullable;
                fieldDescription.IsElementNullable = attribute.IsElementNullable;
            }
        }

        internal static void RewriteClrType(
            this FieldDescriptionBase fieldDescription,
            Func<Type, TypeReference> createContext)
        {
            if (fieldDescription.IsNullable.HasValue
                    && fieldDescription.TypeReference.IsClrTypeReference())
            {
                fieldDescription.TypeReference = createContext(
                    DotNetTypeInfoFactory.Rewrite(
                        fieldDescription.TypeReference.ClrType,
                        !fieldDescription.IsNullable.Value,
                        !fieldDescription.IsElementNullable.Value));
            }
        }
    }
}
