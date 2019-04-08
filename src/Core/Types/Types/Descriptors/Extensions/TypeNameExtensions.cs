using System.Reflection.Metadata;
using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    public static class TypeNameExtensions
    {
        public static IObjectTypeNameDependencyDescriptor Name(
            this IObjectTypeDescriptor descriptor,
            Func<INamedType, NameString> createName)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (createName == null)
            {
                throw new ArgumentNullException(nameof(createName));
            }

            return new ObjectTypeNameDependencyDescriptor(
                descriptor, createName);
        }

        public static IObjectTypeNameDependencyDescriptor<T> Name<T>(
            this IObjectTypeDescriptor<T> descriptor,
            Func<INamedType, NameString> createName)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (createName == null)
            {
                throw new ArgumentNullException(nameof(createName));
            }

            return new ObjectTypeNameDependencyDescriptor<T>(
                descriptor, createName);
        }
    }
}
