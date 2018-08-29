using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Resolvers
{
    public class FieldResolverDescriptor
    {
        private FieldResolverDescriptor(
            FieldReference field,
            FieldResolverKind kind,
            Type resolverType,
            Type sourceType,
            MemberInfo member,
            IReadOnlyCollection<ArgumentDescriptor> argumentDescriptors,
            bool isAsync,
            bool isMethod)
        {
            Field = field;
            Kind = kind;
            ResolverType = resolverType;
            SourceType = sourceType;
            Member = member;
            ArgumentDescriptors = argumentDescriptors;
            IsAsync = isAsync;
            IsMethod = isMethod;
        }

        public static FieldResolverDescriptor CreateSourceProperty(
            FieldReference field, Type sourceType, PropertyInfo property)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            return new FieldResolverDescriptor(
                field, FieldResolverKind.Source,
                sourceType, sourceType, property,
                Array.Empty<ArgumentDescriptor>(),
                false, false);
        }

        public static FieldResolverDescriptor CreateSourceMethod(
            FieldReference field, Type sourceType,
            MethodInfo method, bool isAsync,
            IEnumerable<ArgumentDescriptor> argumentDescriptors)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (argumentDescriptors == null)
            {
                throw new ArgumentNullException(nameof(argumentDescriptors));
            }

            return new FieldResolverDescriptor(
                field, FieldResolverKind.Source,
                sourceType, sourceType, method,
                argumentDescriptors.ToArray(),
                isAsync, true);
        }

        public static FieldResolverDescriptor CreateCollectionProperty(
            FieldReference field, Type resolverType, Type sourceType,
            PropertyInfo property)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (resolverType == null)
            {
                throw new ArgumentNullException(nameof(resolverType));
            }

            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            return new FieldResolverDescriptor(field,
                FieldResolverKind.Collection,
                resolverType, sourceType, property,
                Array.Empty<ArgumentDescriptor>(),
                false, false);
        }

        public static FieldResolverDescriptor CreateCollectionMethod(
            FieldReference field, Type resolverType, Type sourceType,
            MethodInfo method, bool isAsync,
            IEnumerable<ArgumentDescriptor> argumentDescriptors)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (resolverType == null)
            {
                throw new ArgumentNullException(nameof(resolverType));
            }

            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (argumentDescriptors == null)
            {
                throw new ArgumentNullException(nameof(argumentDescriptors));
            }

            return new FieldResolverDescriptor(field, FieldResolverKind.Collection,
                resolverType, sourceType, method,
                argumentDescriptors.ToArray(),
                isAsync, true);
        }

        /// <summary>
        /// Gets a reference describing to which field
        /// the resolver is bound to.
        /// </summary>
        public FieldReference Field { get; }

        /// <summary>
        /// Defines the resolver type.
        /// </summary>
        public FieldResolverKind Kind { get; }

        /// <summary>
        /// Gets the resolver kind.
        /// </summary>
        public Type ResolverType { get; }

        /// <summary>
        /// Gets the type of the source object.
        /// The source object is the object type providing
        /// the fields for the reslver.
        /// <see cref="IResolverContext.Parent{T}" />
        /// /// </summary>
        public Type SourceType { get; }

        /// <summary>
        /// Gets the member of a collection our source resolver
        /// that shall be bound as field resolver..
        /// </summary>
        public MemberInfo Member { get; }

        /// <summary>
        /// Gets a collection of argument descriptors
        /// defining the structure of the arguments
        /// that the resolver demands.
        /// </summary>
        public IReadOnlyCollection<ArgumentDescriptor> ArgumentDescriptors { get; }

        /// <summary>
        /// Defines if the resolver is an asynchronous resolver.
        /// </summary>
        public bool IsAsync { get; }

        /// <summary>
        /// Defines if the resolver is a method;
        /// otherwise the resolver is expected to be a property.
        /// </summary>
        public bool IsMethod { get; }
    }
}
