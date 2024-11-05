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

    public string? QueryTypeName { get; set; }

    public string? MutationTypeName { get; set; }

    public string? SubscriptionTypeName { get; set; }

    public bool StrictValidation { get; set; } = true;

    public bool UseXmlDocumentation { get; set; } = true;

    public Func<Assembly, string>? ResolveXmlDocumentationFileName { get; set; }

    public bool SortFieldsByName { get; set; }

    public bool RemoveUnreachableTypes { get; set; }

    public bool RemoveUnusedTypeSystemDirectives { get; set; } = true;

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

    public FieldMiddlewareApplication FieldMiddleware { get; set; } =
        FieldMiddlewareApplication.UserDefinedFields;

    public bool EnableDirectiveIntrospection { get; set; }

    public DirectiveVisibility DefaultDirectiveVisibility { get; set; } =
        DirectiveVisibility.Public;

    public ExecutionStrategy DefaultResolverStrategy { get; set; } =
        ExecutionStrategy.Parallel;

    public bool ValidatePipelineOrder { get; set; } = true;

    public bool StrictRuntimeTypeValidation { get; set; }

    public IsOfTypeFallback? DefaultIsOfTypeCheck { get; set; }

    public bool EnableOneOf { get; set; } = true;

    public bool EnsureAllNodesCanBeResolved { get; set; } = true;

    public bool EnableFlagEnums { get; set; }

    public bool EnableDefer { get; set; }

    public bool EnableStream { get; set; }

    public bool EnableSemanticNonNull { get; set; }

    public int MaxAllowedNodeBatchSize { get; set; } = 50;

    public bool StripLeadingIFromInterface { get; set; }

    public bool EnableTag { get; set; } = true;

    public DependencyInjectionScope DefaultQueryDependencyInjectionScope { get; set; } =
        DependencyInjectionScope.Resolver;

    public DependencyInjectionScope DefaultMutationDependencyInjectionScope { get; set; } =
        DependencyInjectionScope.Request;

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
            DefaultQueryDependencyInjectionScope = options.DefaultQueryDependencyInjectionScope,
            DefaultMutationDependencyInjectionScope = options.DefaultMutationDependencyInjectionScope,
        };
    }
}
