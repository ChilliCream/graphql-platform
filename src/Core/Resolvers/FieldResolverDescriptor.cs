using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Resolvers
{
    public class FieldResolverDescriptor
    {
        private FieldResolverDescriptor(
            FieldReference field,
            FieldResolverKind kind,
            Type resolverType,
            Type sourceType,
            string memberName,
            IReadOnlyCollection<FieldResolverArgumentDescriptor> argumentDescriptors,
            bool isAsync,
            bool isMethod)
        {
            Field = field;
            Kind = kind;
            ResolverType = resolverType;
            SourceType = sourceType;
            MemberName = memberName;
            ArgumentDescriptors = argumentDescriptors;
            IsAsync = isAsync;
            IsMethod = isMethod;
        }

        public static FieldResolverDescriptor CreateSourceProperty(
            FieldReference field, Type sourceType,
            string propertyName)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            return new FieldResolverDescriptor(field, FieldResolverKind.Source,
                sourceType, sourceType, propertyName,
                Array.Empty<FieldResolverArgumentDescriptor>(),
                false, false);
        }

        public static FieldResolverDescriptor CreateSourceMethod(
            FieldReference field, Type sourceType,
            string methodName, bool isAsync,
            IEnumerable<FieldResolverArgumentDescriptor> argumentDescriptors)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            if (argumentDescriptors == null)
            {
                throw new ArgumentNullException(nameof(argumentDescriptors));
            }

            return new FieldResolverDescriptor(field, FieldResolverKind.Source,
                sourceType, sourceType, methodName,
                argumentDescriptors.ToArray(),
                isAsync, true);
        }

        public static FieldResolverDescriptor CreateCollectionProperty(
            FieldReference field, Type resolverType, Type sourceType,
            string propertyName)
        {
            return new FieldResolverDescriptor(field, FieldResolverKind.Collection,
                resolverType, sourceType, propertyName,
                Array.Empty<FieldResolverArgumentDescriptor>(),
                false, false);
        }

        public static FieldResolverDescriptor CreateCollectionMethod(
            FieldReference field, Type resolverType, Type sourceType,
            string propertyName, bool isAsync,
            IEnumerable<FieldResolverArgumentDescriptor> argumentDescriptors)
        {
            return new FieldResolverDescriptor(field, FieldResolverKind.Collection,
                resolverType, sourceType, propertyName,
                argumentDescriptors.ToArray(),
                isAsync, true);
        }


        /// <summary>
        /// Gets a reference describing to which field the resolver is bound to.
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
        /// Gets the relevant member name if the resolver is not a delegeate; 
        /// otherwise, this property is null.
        /// </summary>
        public string MemberName { get; }

        /// <summary>
        /// Gets a collection of argument descriptors 
        /// defining the structure of the arguments 
        /// that the resolver demands.
        /// </summary>
        public IReadOnlyCollection<FieldResolverArgumentDescriptor> ArgumentDescriptors { get; }

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