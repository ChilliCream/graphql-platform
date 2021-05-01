using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a field of an <see cref="IObjectType"/>.
    /// </summary>
    public interface IObjectField : IOutputField
    {
        /// <summary>
        /// Gets the type that declares this field.
        /// </summary>
        new IObjectType DeclaringType { get; }

        /// <summary>
        /// Gets the field resolver middleware.
        /// </summary>
        FieldDelegate Middleware { get; }

        /// <summary>
        /// Gets the field resolver.
        /// </summary>
        FieldResolverDelegate? Resolver { get; }

        /// <summary>
        /// Gets the pure field resolver. The pure field resolver is only available if this field
        /// can be resolved without side-effects. The execution engine will prefer this resolver
        /// variant if it is available and there are no executable directives that add a middleware
        /// to this field.
        /// </summary>
        PureFieldDelegate? PureResolver { get; }

        /// <summary>
        /// Gets the subscription resolver.
        /// </summary>
        SubscribeResolverDelegate? SubscribeResolver { get; }

        /// <summary>
        /// Gets all executable directives that are associated with this field.
        /// </summary>
        IReadOnlyList<IDirective> ExecutableDirectives { get; }

        /// <summary>
        /// Gets the associated .net type member of this field.
        /// This member can be <c>null</c>.
        /// </summary>
        MemberInfo? Member { get; }
    }
}
