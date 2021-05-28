using System;
using HotChocolate.Language;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types
{
    /// <summary>
    /// Provides configuration methods to <see cref="IObjectFieldDescriptor"/>.
    /// </summary>
    public static class ObjectFieldDescriptorExtensions
    {
        /// <summary>
        /// Marks a field as serial executable which will ensure that the execution engine
        /// synchronizes resolver execution around the marked field and ensures that
        /// no other field is executed in parallel.
        /// </summary>
        public static IObjectFieldDescriptor Serial(this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.Extend().Definition.IsParallelExecutable = false;
            return descriptor;
        }

        /// <summary>
        /// Marks a field as parallel executable which will allow the execution engine
        /// to execute this field in parallel with other resolvers.
        /// </summary>
        public static IObjectFieldDescriptor Parallel(this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.Extend().Definition.IsParallelExecutable = true;
            return descriptor;
        }
    }

    /// <summary>
    /// Provides configuration methods to <see cref="IMemberDescriptor{TDescriptor}"/>.
    /// </summary>
    public static class MemberDescriptorExtensions
    {
        /// <summary>
        /// Defines the type of a field by using GraphQL SDL syntax, e.g. [String!]!.
        /// </summary>
        public static TDescriptor Type<TDescriptor>(
            this TDescriptor descriptor,
            string typeSyntax)
            where TDescriptor : IMemberDescriptor<TDescriptor>, IDescriptor
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrEmpty(typeSyntax))
            {
                throw new ArgumentException(
                    ObjectFieldDescriptorExtensions_Type_TypeSyntaxCannotBeNull,
                    nameof(typeSyntax));
            }

            ITypeNode typeNode = Utf8GraphQLParser.Syntax.ParseTypeReference(typeSyntax);
            return descriptor.Type(typeNode);
        }
    }
}
