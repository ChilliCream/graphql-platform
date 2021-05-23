﻿using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    /// <summary>
    /// Represents mutable schema options.
    /// </summary>
    public interface ISchemaOptions : IReadOnlySchemaOptions
    {
        /// <summary>
        /// Gets or sets the name of the query type.
        /// </summary>
        new string QueryTypeName { get; set; }

        /// <summary>
        /// Gets or sets the name of the mutation type.
        /// </summary>
        new string MutationTypeName { get; set; }

        /// <summary>
        /// Gets or sets the name of the subscription type.
        /// </summary>
        new string SubscriptionTypeName { get; set; }

        /// <summary>
        /// Defines if the schema allows the query type to be omitted.
        /// </summary>
        new bool StrictValidation { get; set; }

        /// <summary>
        /// Defines if the CSharp XML documentation shall be integrated.
        /// </summary>
        new bool UseXmlDocumentation { get; set; }

        /// <summary>
        /// Defines if fields shall be sorted by name.
        /// Default: <c>false</c>
        /// </summary>
        new bool SortFieldsByName { get; set; }

        /// <summary>
        /// Defines if syntax nodes shall be preserved on the type system objects
        /// </summary>
        new bool PreserveSyntaxNodes { get; set; }

        /// <summary>
        /// Defines if types shall be removed from the schema that are
        /// unreachable from the root types.
        /// </summary>
        new bool RemoveUnreachableTypes { get; set; }

        /// <summary>
        /// Defines the default binding behavior.
        /// </summary>
        new BindingBehavior DefaultBindingBehavior { get; set; }

        /// <summary>
        /// Defines on which fields a middleware pipeline can be applied on.
        /// </summary>
        new FieldMiddlewareApplication FieldMiddleware { get; set; }

        /// <summary>
        /// Defines if the experimental directive introspection feature shall be enabled.
        /// </summary>
        new bool EnableDirectiveIntrospection { get; set; }

        /// <summary>
        /// The default directive visibility when directive introspection is enabled.
        /// </summary>
        new DirectiveVisibility DefaultDirectiveVisibility { get; set; }

        /// <summary>
        /// Defines if field inlining is allowed.
        /// </summary>
        new bool AllowInlining { get; set; }

        /// <summary>
        /// Defines that the default resolver execution strategy. 
        /// </summary>
        new ExecutionStrategy DefaultResolverStrategy { get; set; }
    }
}
