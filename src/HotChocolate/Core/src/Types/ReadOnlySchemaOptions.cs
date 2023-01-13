using System;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Configuration;

/// <summary>
/// Represents read-only schema options.
/// </summary>
public class ReadOnlySchemaOptions : IReadOnlySchemaOptions
{
    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlySchemaOptions"/>.
    /// </summary>
    /// <param name="options">
    /// The options that shall be wrapped as read-only options.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/> is <c>null</c>.
    /// </exception>
    public ReadOnlySchemaOptions(IReadOnlySchemaOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        QueryTypeName = options.QueryTypeName ?? "Query";
        MutationTypeName = options.MutationTypeName ?? "Mutation";
        SubscriptionTypeName = options.SubscriptionTypeName ?? "Subscription";
        StrictValidation = options.StrictValidation;
        SortFieldsByName = options.SortFieldsByName;
        UseXmlDocumentation = options.UseXmlDocumentation;
        ResolveXmlDocumentationFileName = options.ResolveXmlDocumentationFileName;
        RemoveUnreachableTypes = options.RemoveUnreachableTypes;
        DefaultBindingBehavior = options.DefaultBindingBehavior;
        DefaultFieldBindingFlags = options.DefaultFieldBindingFlags;
        FieldMiddleware = options.FieldMiddleware;
        PreserveSyntaxNodes = options.PreserveSyntaxNodes;
        EnableDirectiveIntrospection = options.EnableDirectiveIntrospection;
        DefaultDirectiveVisibility = options.DefaultDirectiveVisibility;
        DefaultResolverStrategy = options.DefaultResolverStrategy;
        ValidatePipelineOrder = options.ValidatePipelineOrder;
        StrictRuntimeTypeValidation = options.StrictRuntimeTypeValidation;
        DefaultIsOfTypeCheck = options.DefaultIsOfTypeCheck;
        EnableOneOf = options.EnableOneOf;
        EnsureAllNodesCanBeResolved = options.EnsureAllNodesCanBeResolved;
        EnableFlagEnums = options.EnableFlagEnums;
        EnableDefer = options.EnableDefer;
        EnableStream = options.EnableStream;
    }

    /// <inheritdoc />
    public string QueryTypeName { get; }

    /// <inheritdoc />
    public string MutationTypeName { get; }

    /// <inheritdoc />
    public string SubscriptionTypeName { get; }

    /// <inheritdoc />
    public bool StrictValidation { get; }

    /// <inheritdoc />
    public bool UseXmlDocumentation { get; }

    /// <inheritdoc />
    public Func<Assembly, string>? ResolveXmlDocumentationFileName { get; }

    /// <inheritdoc />
    public bool SortFieldsByName { get; }

    /// <inheritdoc />
    public bool PreserveSyntaxNodes { get; }

    /// <inheritdoc />
    public bool RemoveUnreachableTypes { get; }

    /// <inheritdoc />
    public BindingBehavior DefaultBindingBehavior { get; }

    /// <inheritdoc />
    public FieldBindingFlags DefaultFieldBindingFlags { get; }

    /// <inheritdoc />
    public FieldMiddlewareApplication FieldMiddleware { get; }

    /// <inheritdoc />
    public bool EnableDirectiveIntrospection { get; }

    /// <inheritdoc />
    public DirectiveVisibility DefaultDirectiveVisibility { get; }

    public ExecutionStrategy DefaultResolverStrategy { get; }

    /// <inheritdoc />
    public bool ValidatePipelineOrder { get; }

    /// <inheritdoc />
    public bool StrictRuntimeTypeValidation { get; }

    /// <inheritdoc />
    public IsOfTypeFallback? DefaultIsOfTypeCheck { get; }

    /// <inheritdoc />
    public bool EnableOneOf { get; }

    /// <inheritdoc />
    public bool EnsureAllNodesCanBeResolved { get; }

    /// <inheritdoc />
    public bool EnableFlagEnums { get; }

    /// <inheritdoc />
    public bool EnableDefer { get; }

    /// <inheritdoc />
    public bool EnableStream { get; }
}
