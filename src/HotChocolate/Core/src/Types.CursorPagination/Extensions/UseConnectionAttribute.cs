using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Pagination;
using HotChocolate.Utilities;

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
        ICustomAttributeProvider element)
    {
        if (element is not MemberInfo)
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
            if(beforeArg is not null)
            {
                definition.Arguments.Remove(beforeArg);
            }

            var lastArg = definition.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal("last"));
            if(lastArg is not null)
            {
                definition.Arguments.Remove(lastArg);
            }
        }
    }
}
