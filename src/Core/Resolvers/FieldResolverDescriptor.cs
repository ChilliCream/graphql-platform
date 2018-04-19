using System;
using System.Collections.Generic;

namespace HotChocolate.Resolvers
{
    public class FieldResolverDescriptor
    {
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
        public IReadOnlyCollection<FieldResolverArgumentDescriptor> Arguments { get; }

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