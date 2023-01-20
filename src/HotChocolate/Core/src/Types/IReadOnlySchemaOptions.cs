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
    /// <inheritdoc cref="SchemaOptions.QueryTypeName"/>
    string? QueryTypeName { get; }

    /// <inheritdoc cref="SchemaOptions.MutationTypeName"/>
    string? MutationTypeName { get; }

    /// <inheritdoc cref="SchemaOptions.SubscriptionTypeName"/>
    string? SubscriptionTypeName { get; }

    /// <inheritdoc cref="SchemaOptions.StrictValidation"/>
    bool StrictValidation { get; }

    /// <inheritdoc cref="SchemaOptions.UseXmlDocumentation"/>
    bool UseXmlDocumentation { get; }

    /// <inheritdoc cref="SchemaOptions.ResolveXmlDocumentationFileName"/>
    Func<Assembly, string>? ResolveXmlDocumentationFileName { get; }

    /// <inheritdoc cref="SchemaOptions.SortFieldsByName"/>
    bool SortFieldsByName { get; }

    /// <inheritdoc cref="SchemaOptions.PreserveSyntaxNodes"/>
    bool PreserveSyntaxNodes { get; }

    /// <inheritdoc cref="SchemaOptions.RemoveUnreachableTypes"/>
    bool RemoveUnreachableTypes { get; }

    /// <inheritdoc cref="SchemaOptions.DefaultBindingBehavior"/>
    BindingBehavior DefaultBindingBehavior { get; }

    /// <inheritdoc cref="SchemaOptions.DefaultFieldBindingFlags"/>
    FieldBindingFlags DefaultFieldBindingFlags { get; }

    /// <inheritdoc cref="SchemaOptions.FieldMiddleware"/>
    FieldMiddlewareApplication FieldMiddleware { get; }

    /// <inheritdoc cref="SchemaOptions.EnableDirectiveIntrospection"/>
    bool EnableDirectiveIntrospection { get; }

    /// <inheritdoc cref="SchemaOptions.DefaultDirectiveVisibility"/>
    DirectiveVisibility DefaultDirectiveVisibility { get; }

    /// <inheritdoc cref="SchemaOptions.DefaultResolverStrategy"/>
    ExecutionStrategy DefaultResolverStrategy { get; }

    /// <inheritdoc cref="SchemaOptions.ValidatePipelineOrder"/>
    bool ValidatePipelineOrder { get; }

    /// <inheritdoc cref="SchemaOptions.StrictRuntimeTypeValidation"/>
    bool StrictRuntimeTypeValidation { get; }

    /// <inheritdoc cref="SchemaOptions.DefaultIsOfTypeCheck"/>
    IsOfTypeFallback? DefaultIsOfTypeCheck { get; }

    /// <inheritdoc cref="SchemaOptions.EnableOneOf"/>
    bool EnableOneOf { get; }

    /// <inheritdoc cref="SchemaOptions.EnsureAllNodesCanBeResolved"/>
    bool EnsureAllNodesCanBeResolved { get; }

    /// <inheritdoc cref="SchemaOptions.EnableFlagEnums"/>
    bool EnableFlagEnums { get; }

    /// <inheritdoc cref="SchemaOptions.EnableDefer"/>
    bool EnableDefer { get; }

    /// <inheritdoc cref="SchemaOptions.EnableStream"/>
    bool EnableStream { get; }
}
