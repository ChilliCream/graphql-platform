using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Pagination;
using HotChocolate.Utilities;
using static HotChocolate.Types.Pagination.CursorPagingArgumentNames;
using static HotChocolate.WellKnownMiddleware;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

/// <summary>
/// This attribute allows to override the global paging options for a specific field.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
public sealed class UseConnectionAttribute : DescriptorAttribute
{
    private int? _defaultPageSize;
    private int? _maxPageSize;
    private bool? _includeTotalCount;
    private bool? _allowBackwardPagination;
    private bool? _requirePagingBoundaries;
    private bool? _inferConnectionNameFromField;
    private bool? _enableRelativeCursors;

    /// <summary>
    /// Overrides the global paging options for the annotated field.
    /// </summary>
    /// <param name="order">
    /// The explicit order priority for this attribute.
    /// </param>
    public UseConnectionAttribute([CallerLineNumber] int order = 0)
    {
        Order = order;
    }

    /// <summary>
    /// Specifies the default page size for this field.
    /// </summary>
    public int DefaultPageSize
    {
        get => _defaultPageSize ?? PagingDefaults.DefaultPageSize;
        set => _defaultPageSize = value;
    }

    /// <summary>
    /// Specifies the maximum allowed page size.
    /// </summary>
    public int MaxPageSize
    {
        get => _maxPageSize ?? PagingDefaults.MaxPageSize;
        set => _maxPageSize = value;
    }

    /// <summary>
    /// Include the total count field to the result type.
    /// </summary>
    public bool IncludeTotalCount
    {
        get => _includeTotalCount ?? PagingDefaults.IncludeTotalCount;
        set => _includeTotalCount = value;
    }

    /// <summary>
    /// Allow backward paging using <c>last</c> and <c>before</c>
    /// </summary>
    public bool AllowBackwardPagination
    {
        get => _allowBackwardPagination ?? PagingDefaults.AllowBackwardPagination;
        set => _allowBackwardPagination = value;
    }

    /// <summary>
    /// Defines if the paging middleware shall require the
    /// API consumer to specify paging boundaries.
    /// </summary>
    public bool RequirePagingBoundaries
    {
        get => _requirePagingBoundaries ?? PagingDefaults.RequirePagingBoundaries;
        set => _requirePagingBoundaries = value;
    }

    /// <summary>
    /// Connection names are by default inferred from the field name to
    /// which they are bound to as opposed to the node type name.
    /// </summary>
    public bool InferConnectionNameFromField
    {
        get => _inferConnectionNameFromField ?? PagingDefaults.InferConnectionNameFromField;
        set => _inferConnectionNameFromField = value;
    }

    /// <summary>
    /// Defines whether relative cursors are allowed.
    /// </summary>
    public bool EnableRelativeCursors
    {
        get => _enableRelativeCursors ?? PagingDefaults.EnableRelativeCursors;
        set => _enableRelativeCursors = value;
    }

    public string? Name { get; set; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        if (attributeProvider is not MemberInfo)
        {
            return;
        }

        var options = new PagingOptions
        {
            DefaultPageSize = _defaultPageSize,
            MaxPageSize = _maxPageSize,
            IncludeTotalCount = _includeTotalCount,
            AllowBackwardPagination = _allowBackwardPagination,
            RequirePagingBoundaries = _requirePagingBoundaries,
            InferConnectionNameFromField = _inferConnectionNameFromField,
            ProviderName = Name,
            EnableRelativeCursors = _enableRelativeCursors
        };

        if (descriptor is IObjectFieldDescriptor fieldDesc)
        {
            var definition = fieldDesc.Extend().Configuration;
            definition.MiddlewareConfigurations.Add(
                new FieldMiddlewareConfiguration(
                    CreatePagingValidationMiddleware(),
                    key: Paging));
            definition.Tasks.Add(
                new OnCreateTypeSystemConfigurationTask(
                    (_, d) => d.Features.Set(options), definition));
            definition.Tasks.Add(
                new OnCompleteTypeSystemConfigurationTask<ObjectFieldConfiguration>(
                    (c, d) => ApplyPagingOptions(c.DescriptorContext, d, options),
                    definition,
                    ApplyConfigurationOn.BeforeCompletion));
        }

        static void ApplyPagingOptions(
            IDescriptorContext context,
            ObjectFieldConfiguration definition,
            PagingOptions options)
        {
            options = context.GetPagingOptions(options);
            definition.Features.Set(options);

            if (options.AllowBackwardPagination ?? PagingDefaults.AllowBackwardPagination)
            {
                return;
            }

            var beforeArg = definition.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal("before"));
            if (beforeArg is not null)
            {
                definition.Arguments.Remove(beforeArg);
            }

            var lastArg = definition.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal("last"));
            if (lastArg is not null)
            {
                definition.Arguments.Remove(lastArg);
            }
        }
    }

    private static FieldMiddleware CreatePagingValidationMiddleware()
        => next => context =>
        {
            var options = PagingHelper.GetPagingOptions(context.Schema, context.Selection.Field);
            ValidateContext(context, options);
            PublishPagingArguments(context, options);
            return next(context);
        };

    private static void ValidateContext(
        IMiddlewareContext context,
        PagingOptions options)
    {
        var allowBackwardPagination =
            options.AllowBackwardPagination ?? PagingDefaults.AllowBackwardPagination;
        var requirePagingBoundaries =
            options.RequirePagingBoundaries ?? PagingDefaults.RequirePagingBoundaries;
        var maxPageSize =
            options.MaxPageSize ?? PagingDefaults.MaxPageSize;

        var first = context.ArgumentValue<int?>(First);
        var last = allowBackwardPagination
            ? context.ArgumentValue<int?>(Last)
            : null;

        if (requirePagingBoundaries && first is null && last is null)
        {
            if (allowBackwardPagination)
            {
                throw ThrowHelper.PagingHandler_NoBoundariesSet(
                    context.Selection.Field,
                    context.Path);
            }

            throw ThrowHelper.PagingHandler_FirstValueNotSet(
                context.Selection.Field,
                context.Path);
        }

        if (first < 0)
        {
            throw ThrowHelper.PagingHandler_MinPageSize(
                (int)first,
                context.Selection.Field,
                context.Path);
        }

        if (first > maxPageSize)
        {
            throw ThrowHelper.PagingHandler_MaxPageSize(
                (int)first,
                maxPageSize,
                context.Selection.Field,
                context.Path);
        }

        if (last < 0)
        {
            throw ThrowHelper.PagingHandler_MinPageSize(
                (int)last,
                context.Selection.Field,
                context.Path);
        }

        if (last > maxPageSize)
        {
            throw ThrowHelper.PagingHandler_MaxPageSize(
                (int)last,
                maxPageSize,
                context.Selection.Field,
                context.Path);
        }
    }

    private static void PublishPagingArguments(
        IMiddlewareContext context,
        PagingOptions options)
    {
        var allowBackwardPagination = options.AllowBackwardPagination ?? PagingDefaults.AllowBackwardPagination;
        var maxPageSize = options.MaxPageSize ?? PagingDefaults.MaxPageSize;
        var defaultPageSize = options.DefaultPageSize ?? PagingDefaults.DefaultPageSize;

        if (maxPageSize < defaultPageSize)
        {
            defaultPageSize = maxPageSize;
        }

        var first = context.ArgumentValue<int?>(First);
        var last = allowBackwardPagination
            ? context.ArgumentValue<int?>(Last)
            : null;

        if (first is null && last is null)
        {
            first = defaultPageSize;
        }

        context.SetLocalState(
            WellKnownContextData.PagingArguments,
            new CursorPagingArguments(
                first,
                last,
                context.ArgumentValue<string?>(After),
                allowBackwardPagination
                    ? context.ArgumentValue<string?>(Before)
                    : null));
    }
}
