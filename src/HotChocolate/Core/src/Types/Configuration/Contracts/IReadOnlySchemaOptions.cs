﻿using HotChocolate.Execution;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Configuration
{
    /// <summary>
    /// Represents read-only schema options.
    /// </summary>
    public interface IReadOnlySchemaOptions
    {
        /// <summary>
        /// Gets the name of the query type.
        /// </summary>
        string? QueryTypeName { get; }

        /// <summary>
        /// Gets or sets the name of the mutation type.
        /// </summary>
        string? MutationTypeName { get; }

        /// <summary>
        /// Gets or sets the name of the subscription type.
        /// </summary>
        string? SubscriptionTypeName { get; }

        /// <summary>
        /// Defines if the schema allows the query type to be omitted.
        /// </summary>
        bool StrictValidation { get; }

        /// <summary>
        /// Defines if the CSharp XML documentation shall be integrated.
        /// </summary>
        bool UseXmlDocumentation { get; }

        /// <summary>
        /// Defines if fields shall be sorted by name.
        /// Default: <c>false</c>
        /// </summary>
        bool SortFieldsByName { get; }

        /// <summary>
        /// Defines if syntax nodes shall be preserved on the type system objects
        /// </summary>
        bool PreserveSyntaxNodes { get; }

        /// <summary>
        /// Defines if types shall be removed from the schema that are
        /// unreachable from the root types.
        /// </summary>
        bool RemoveUnreachableTypes { get; }

        /// <summary>
        /// Defines the default binding behavior.
        /// </summary>
        BindingBehavior DefaultBindingBehavior { get; }

        /// <summary>
        /// Defines on which fields a middleware pipeline can be applied on.
        /// </summary>
        FieldMiddlewareApplication FieldMiddleware { get; }

        /// <summary>
        /// Defines if the experimental directive introspection feature shall be enabled.
        /// </summary>
        bool EnableDirectiveIntrospection { get; }

        /// <summary>
        /// The default directive visibility when directive introspection is enabled.
        /// </summary>
        DirectiveVisibility DefaultDirectiveVisibility { get; }

        /// <summary>
        /// Defines if field inlining is allowed.
        /// </summary>
        bool AllowInlining { get; }

        /// <summary>
        /// Defines that the default resolver execution strategy. 
        /// </summary>
        ExecutionStrategy DefaultResolverStrategy { get; }
    }
}
