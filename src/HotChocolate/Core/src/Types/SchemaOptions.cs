using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate;

/// <summary>
/// Represents mutable schema options.
/// </summary>
public class SchemaOptions : ISchemaOptions
{
    /// <inheritdoc cref="ISchemaOptions.QueryTypeName"/>
    public string? QueryTypeName { get; set; }

    /// <inheritdoc cref="ISchemaOptions.MutationTypeName"/>
    public string? MutationTypeName { get; set; }

    /// <inheritdoc cref="ISchemaOptions.SubscriptionTypeName"/>
    public string? SubscriptionTypeName { get; set; }

    /// <inheritdoc cref="ISchemaOptions.StrictValidation"/>
    public bool StrictValidation { get; set; } = true;

    /// <inheritdoc cref="ISchemaOptions.UseXmlDocumentation"/>
    public bool UseXmlDocumentation { get; set; } = true;

    /// <inheritdoc cref="ISchemaOptions.ResolveXmlDocumentationFileName"/>
    public Func<Assembly, string>? ResolveXmlDocumentationFileName { get; set; } = null;

    /// <inheritdoc cref="ISchemaOptions.SortFieldsByName"/>
    public bool SortFieldsByName { get; set; }

    /// <inheritdoc cref="ISchemaOptions.PreserveSyntaxNodes"/>
    public bool PreserveSyntaxNodes { get; set; }

    /// <inheritdoc cref="ISchemaOptions.RemoveUnreachableTypes"/>
    public bool RemoveUnreachableTypes { get; set; }

    /// <inheritdoc cref="ISchemaOptions.DefaultBindingBehavior"/>
    public BindingBehavior DefaultBindingBehavior { get; set; } =
        BindingBehavior.Implicit;

    /// <inheritdoc cref="ISchemaOptions.FieldMiddleware"/>
    public FieldMiddlewareApplication FieldMiddleware { get; set; } =
        FieldMiddlewareApplication.UserDefinedFields;

    /// <inheritdoc cref="ISchemaOptions.EnableDirectiveIntrospection"/>
    public bool EnableDirectiveIntrospection { get; set; }

    /// <inheritdoc cref="ISchemaOptions.DefaultDirectiveVisibility"/>
    public DirectiveVisibility DefaultDirectiveVisibility { get; set; } =
        DirectiveVisibility.Public;

    /// <inheritdoc cref="ISchemaOptions.AllowInlining"/>
    public bool AllowInlining { get; set; } = true;

    /// <inheritdoc cref="ISchemaOptions.DefaultResolverStrategy"/>
    public ExecutionStrategy DefaultResolverStrategy { get; set; } =
        ExecutionStrategy.Parallel;

    /// <inheritdoc cref="ISchemaOptions.ValidatePipelineOrder"/>
    public bool ValidatePipelineOrder { get; set; } = true;

    /// <inheritdoc cref="ISchemaOptions.StrictRuntimeTypeValidation"/>
    public bool StrictRuntimeTypeValidation { get; set; }

    /// <inheritdoc cref="ISchemaOptions.DefaultIsOfTypeCheck"/>
    public IsOfTypeFallback? DefaultIsOfTypeCheck { get; set; }

    /// <inheritdoc cref="ISchemaOptions.EnableOneOf"/>
    public bool EnableOneOf { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.EnableFlagEnums"/>
    public bool EnableFlagEnums { get; set; }

    /// <summary>
    /// Creates a mutable options object from a read-only options object.
    /// </summary>
    /// <param name="options">The read-only options object.</param>
    /// <returns>Returns a new mutable options object.</returns>
    public static SchemaOptions FromOptions(IReadOnlySchemaOptions options)
    {
        return new()
        {
            QueryTypeName = options.QueryTypeName,
            MutationTypeName = options.MutationTypeName,
            SubscriptionTypeName = options.SubscriptionTypeName,
            StrictValidation = options.StrictValidation,
            UseXmlDocumentation = options.UseXmlDocumentation,
            ResolveXmlDocumentationFileName = options.ResolveXmlDocumentationFileName,
            FieldMiddleware = options.FieldMiddleware,
            DefaultBindingBehavior = options.DefaultBindingBehavior,
            PreserveSyntaxNodes = options.PreserveSyntaxNodes,
            EnableDirectiveIntrospection = options.EnableDirectiveIntrospection,
            DefaultDirectiveVisibility = options.DefaultDirectiveVisibility,
            AllowInlining = options.AllowInlining,
            DefaultResolverStrategy = options.DefaultResolverStrategy,
            ValidatePipelineOrder = options.ValidatePipelineOrder,
            StrictRuntimeTypeValidation = options.StrictRuntimeTypeValidation,
            RemoveUnreachableTypes = options.RemoveUnreachableTypes,
            SortFieldsByName = options.SortFieldsByName,
            DefaultIsOfTypeCheck = options.DefaultIsOfTypeCheck,
            EnableOneOf = options.EnableOneOf,
            EnableFlagEnums = options.EnableFlagEnums
        };
    }
}
