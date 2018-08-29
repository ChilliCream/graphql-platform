using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Resolvers
{
    interface IFieldResolverDescriptor
    {
        /// <summary>
        /// Gets the type of the source object.
        /// The source object is the object type providing
        /// the fields for the reslver.
        /// <see cref="IResolverContext.Parent{T}" />
        /// /// </summary>
        Type SourceType { get; }

        /// <summary>
        /// Gets a field reference with the member that
        /// shall be act as resolver.
        /// </summary>
        FieldMember Field { get; }

        /// <summary>
        /// Gets a collection of argument descriptors
        /// defining the structure of the arguments
        /// that the resolver demands.
        /// </summary>
        IReadOnlyCollection<ArgumentDescriptor> Arguments { get; }

        /// <summary>
        /// Defines if the resolver is an asynchronous resolver.
        /// </summary>
        bool IsAsync { get; }

        /// <summary>
        /// Defines if the resolver is a method;
        /// otherwise the resolver is expected to be a property.
        /// </summary>
        bool IsMethod { get; }

        /// <summary>
        /// Defines if the resolver is a property;
        /// otherwise the resolver is expected to be a method.
        /// </summary>
        bool IsProperty { get; }
    }
}
