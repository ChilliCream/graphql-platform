using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate;

/// <summary>
/// Represents read-only schema options.
/// </summary>
public interface IReadOnlySchemaOptions
{
    /// <inheritdoc cref="ISchemaOptions.QueryTypeName"/>
    string? QueryTypeName { get; }

    /// <inheritdoc cref="ISchemaOptions.MutationTypeName"/>
    string? MutationTypeName { get; }

    /// <inheritdoc cref="ISchemaOptions.SubscriptionTypeName"/>
    string? SubscriptionTypeName { get; }

    /// <inheritdoc cref="ISchemaOptions.StrictValidation"/>
    bool StrictValidation { get; }

    /// <inheritdoc cref="ISchemaOptions.UseXmlDocumentation"/>
    bool UseXmlDocumentation { get; }

    /// <inheritdoc cref="ISchemaOptions.ResolveXmlDocumentationFileName"/>
    Func<Assembly, string>? ResolveXmlDocumentationFileName { get; }

    /// <inheritdoc cref="ISchemaOptions.SortFieldsByName"/>
    bool SortFieldsByName { get; }

    /// <inheritdoc cref="ISchemaOptions.PreserveSyntaxNodes"/>
    bool PreserveSyntaxNodes { get; }

    /// <inheritdoc cref="ISchemaOptions.RemoveUnreachableTypes"/>
    bool RemoveUnreachableTypes { get; }

    /// <inheritdoc cref="ISchemaOptions.DefaultBindingBehavior"/>
    BindingBehavior DefaultBindingBehavior { get; }

    /// <inheritdoc cref="ISchemaOptions.FieldMiddleware"/>
    FieldMiddlewareApplication FieldMiddleware { get; }

    /// <inheritdoc cref="ISchemaOptions.EnableDirectiveIntrospection"/>
    bool EnableDirectiveIntrospection { get; }

    /// <inheritdoc cref="ISchemaOptions.DefaultDirectiveVisibility"/>
    DirectiveVisibility DefaultDirectiveVisibility { get; }

    /// <inheritdoc cref="ISchemaOptions.AllowInlining"/>
    bool AllowInlining { get; }

    /// <inheritdoc cref="ISchemaOptions.DefaultResolverStrategy"/>
    ExecutionStrategy DefaultResolverStrategy { get; }

    /// <inheritdoc cref="ISchemaOptions.ValidatePipelineOrder"/>
    bool ValidatePipelineOrder { get; }

    /// <inheritdoc cref="ISchemaOptions.StrictRuntimeTypeValidation"/>
    bool StrictRuntimeTypeValidation { get; }

    /// <inheritdoc cref="ISchemaOptions.DefaultIsOfTypeCheck"/>
    IsOfTypeFallback? DefaultIsOfTypeCheck { get; }

    /// <inheritdoc cref="ISchemaOptions.EnableOneOf"/>
    bool EnableOneOf { get; }

    /// <inheritdoc cref="ISchemaOptions.EnableFlagEnums"/>
    bool EnableFlagEnums { get; }
}
