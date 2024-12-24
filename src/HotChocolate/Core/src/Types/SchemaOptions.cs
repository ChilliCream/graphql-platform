using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate;

/// <summary>
/// Represents mutable schema options.
/// </summary>
public class SchemaOptions : IReadOnlySchemaOptions
{
    private BindingBehavior _defaultBindingBehavior = BindingBehavior.Implicit;
    private FieldBindingFlags _defaultFieldBindingFlags = FieldBindingFlags.Instance;

    /// <inheritdoc cref="IReadOnlySchemaOptions.QueryTypeName"/>
    public string? QueryTypeName { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.MutationTypeName"/>
    public string? MutationTypeName { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.SubscriptionTypeName"/>
    public string? SubscriptionTypeName { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.StrictValidation"/>
    public bool StrictValidation { get; set; } = true;

    /// <inheritdoc cref="IReadOnlySchemaOptions.UseXmlDocumentation"/>
    public bool UseXmlDocumentation { get; set; } = true;

    /// <inheritdoc cref="IReadOnlySchemaOptions.ResolveXmlDocumentationFileName"/>
    public Func<Assembly, string>? ResolveXmlDocumentationFileName { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.SortFieldsByName"/>
    public bool SortFieldsByName { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.RemoveUnreachableTypes"/>
    public bool RemoveUnreachableTypes { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.RemoveUnusedTypeSystemDirectives"/>
    public bool RemoveUnusedTypeSystemDirectives { get; set; } = true;

    /// <inheritdoc cref="IReadOnlySchemaOptions.DefaultBindingBehavior"/>
    public BindingBehavior DefaultBindingBehavior
    {
        get => _defaultBindingBehavior;
        set
        {
            _defaultBindingBehavior = value;

            if (value is BindingBehavior.Explicit)
            {
                _defaultFieldBindingFlags = FieldBindingFlags.Default;
            }
        }
    }

    /// <inheritdoc cref="IReadOnlySchemaOptions.DefaultFieldBindingFlags"/>
    public FieldBindingFlags DefaultFieldBindingFlags
    {
        get => _defaultFieldBindingFlags;
        set
        {
            _defaultFieldBindingFlags = value;

            if (value is not FieldBindingFlags.Default)
            {
                _defaultBindingBehavior = BindingBehavior.Implicit;
            }
        }
    }

    /// <inheritdoc cref="IReadOnlySchemaOptions.FieldMiddleware"/>
    public FieldMiddlewareApplication FieldMiddleware { get; set; } =
        FieldMiddlewareApplication.UserDefinedFields;

    /// <inheritdoc cref="IReadOnlySchemaOptions.EnableDirectiveIntrospection"/>
    public bool EnableDirectiveIntrospection { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.DefaultDirectiveVisibility"/>
    public DirectiveVisibility DefaultDirectiveVisibility { get; set; } =
        DirectiveVisibility.Public;

    /// <inheritdoc cref="IReadOnlySchemaOptions.DefaultResolverStrategy"/>
    public ExecutionStrategy DefaultResolverStrategy { get; set; } =
        ExecutionStrategy.Parallel;

    /// <inheritdoc cref="IReadOnlySchemaOptions.ValidatePipelineOrder"/>
    public bool ValidatePipelineOrder { get; set; } = true;

    /// <inheritdoc cref="IReadOnlySchemaOptions.StrictRuntimeTypeValidation"/>
    public bool StrictRuntimeTypeValidation { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.DefaultIsOfTypeCheck"/>
    public IsOfTypeFallback? DefaultIsOfTypeCheck { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.EnableOneOf"/>
    public bool EnableOneOf { get; set; } = true;

    /// <inheritdoc cref="IReadOnlySchemaOptions.EnsureAllNodesCanBeResolved"/>
    public bool EnsureAllNodesCanBeResolved { get; set; } = true;

    /// <inheritdoc cref="IReadOnlySchemaOptions.EnableFlagEnums"/>
    public bool EnableFlagEnums { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.EnableDefer"/>
    public bool EnableDefer { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.EnableStream"/>
    public bool EnableStream { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.EnableSemanticNonNull"/>
    public bool EnableSemanticNonNull { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.MaxAllowedNodeBatchSize"/>
    public int MaxAllowedNodeBatchSize { get; set; } = 50;

    /// <inheritdoc cref="IReadOnlySchemaOptions.StripLeadingIFromInterface"/>
    public bool StripLeadingIFromInterface { get; set; }

    /// <inheritdoc cref="IReadOnlySchemaOptions.EnableTag"/>
    public bool EnableTag { get; set; } = true;

    /// <inheritdoc cref="IReadOnlySchemaOptions.ErrorOnAspNetCoreAuthorizationAttributes"/>
    public bool ErrorOnAspNetCoreAuthorizationAttributes { get; set; } = true;

    /// <inheritdoc cref="IReadOnlySchemaOptions.DefaultQueryDependencyInjectionScope"/>
    public DependencyInjectionScope DefaultQueryDependencyInjectionScope { get; set; } =
        DependencyInjectionScope.Resolver;

    /// <inheritdoc cref="IReadOnlySchemaOptions.DefaultMutationDependencyInjectionScope"/>
    public DependencyInjectionScope DefaultMutationDependencyInjectionScope { get; set; } =
        DependencyInjectionScope.Request;

    /// <inheritdoc cref="IReadOnlySchemaOptions.PublishRootFieldPagesToPromiseCache"/>
    public bool PublishRootFieldPagesToPromiseCache { get; set; } = true;

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
            EnableDirectiveIntrospection = options.EnableDirectiveIntrospection,
            DefaultDirectiveVisibility = options.DefaultDirectiveVisibility,
            DefaultResolverStrategy = options.DefaultResolverStrategy,
            ValidatePipelineOrder = options.ValidatePipelineOrder,
            StrictRuntimeTypeValidation = options.StrictRuntimeTypeValidation,
            RemoveUnreachableTypes = options.RemoveUnreachableTypes,
            RemoveUnusedTypeSystemDirectives = options.RemoveUnusedTypeSystemDirectives,
            SortFieldsByName = options.SortFieldsByName,
            DefaultIsOfTypeCheck = options.DefaultIsOfTypeCheck,
            EnableOneOf = options.EnableOneOf,
            EnsureAllNodesCanBeResolved = options.EnsureAllNodesCanBeResolved,
            EnableFlagEnums = options.EnableFlagEnums,
            EnableDefer = options.EnableDefer,
            EnableStream = options.EnableStream,
            EnableSemanticNonNull = options.EnableSemanticNonNull,
            DefaultFieldBindingFlags = options.DefaultFieldBindingFlags,
            MaxAllowedNodeBatchSize = options.MaxAllowedNodeBatchSize,
            StripLeadingIFromInterface = options.StripLeadingIFromInterface,
            EnableTag = options.EnableTag,
            ErrorOnAspNetCoreAuthorizationAttributes = options.ErrorOnAspNetCoreAuthorizationAttributes,
            DefaultQueryDependencyInjectionScope = options.DefaultQueryDependencyInjectionScope,
            DefaultMutationDependencyInjectionScope = options.DefaultMutationDependencyInjectionScope,
        };
    }
}
