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
        /// Defines if this field can be executed in parallel with other fields.
        /// </summary>
        bool IsParallelExecutable { get; }

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
        /// Gets a field resolver that can be used to inline the resolver execution into the parent
        /// resolver. Resolvers can only be inlined if they abide to the rules of the pure-resolver.
        /// Further inline resolvers cannot have arguments and do not have access to context data.
        /// </summary>
        InlineFieldDelegate? InlineResolver { get; }

        /// <summary>
        /// Gets the subscription resolver.
        /// </summary>
        SubscribeResolverDelegate? SubscribeResolver { get; }

        /// <summary>
        /// Gets all executable directives that are associated with this field.
        /// </summary>
        IReadOnlyList<IDirective> ExecutableDirectives { get; }

        /// <summary>
        /// Gets the associated member of the runtime type for this field.
        /// This property can be <c>null</c> if this field is not associated to
        /// a concrete member on the runtime type.
        /// </summary>
        MemberInfo? Member { get; }

        /// <summary>
        /// Gets the resolver member of this filed.
        /// If this field has no explicit resolver member
        /// this property will return <see cref="Member"/>.
        /// </summary>
        MemberInfo? ResolverMember { get; }
    }
}
