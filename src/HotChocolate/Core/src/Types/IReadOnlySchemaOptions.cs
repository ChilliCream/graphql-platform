using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate;

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
    /// A delegate which resolves the name of the XML documentation file to be read.
    /// Only used if <seealso cref="UseXmlDocumentation"/> is true.
    /// </summary>
    Func<Assembly, string>? ResolveXmlDocumentationFileName { get; }

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

    /// <summary>
    /// Defines if the order of important middleware components shall be validated.
    /// </summary>
    bool ValidatePipelineOrder { get; }

    /// <summary>
    /// Defines if the runtime types of types shall be validated.
    /// </summary>
    bool StrictRuntimeTypeValidation { get; }

    /// <summary>
    /// Defines a delegate that determines if a runtime
    /// is an instance of an <see cref="ObjectType{T}"/>.
    /// </summary>
    IsOfTypeFallback? DefaultIsOfTypeCheck { get; }

    /// <summary>
    /// Defines if the OneOf spec RFC is enabled. This feature is experimental.
    /// </summary>
    bool EnableOneOf { get; }
}
