using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types;

/// <summary>
/// This attribute adds the offset paging middleware to the annotated method or property.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public class UseOffsetPagingAttribute : DescriptorAttribute
{
    private int? _defaultPageSize;
    private int? _maxPageSize;
    private bool? _includeTotalCount;
    private bool? _requirePagingBoundaries;

    /// <summary>
    /// Applies the offset paging middleware to the annotated property.
    /// </summary>
    /// <param name="type">
    /// The schema type representing the item type.
    /// </param>
    /// <param name="order">
    /// The explicit order priority for this attribute.
    /// </param>
    public UseOffsetPagingAttribute(Type? type = null, [CallerLineNumber] int order = 0)
    {
        Type = type;
        Order = order;
    }


    /// <summary>
    /// The schema type representation of the item type.
    /// </summary>
    public Type? Type { get; private set; }

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
    /// Defines if the paging middleware shall require the
    /// API consumer to specify paging boundaries.
    /// </summary>
    public bool RequirePagingBoundaries
    {
        get => _requirePagingBoundaries ?? PagingDefaults.AllowBackwardPagination;
        set => _requirePagingBoundaries = value;
    }

    /// <summary>
    /// Specifies the name of the paging provider that shall be used.
    /// </summary>
    public string? ProviderName { get; set; }

    protected override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (descriptor is IObjectFieldDescriptor odf)
        {
            odf.UseOffsetPaging(
                Type,
                options: new PagingOptions
                {
                    DefaultPageSize = _defaultPageSize,
                    MaxPageSize = _maxPageSize,
                    IncludeTotalCount = _includeTotalCount,
                    RequirePagingBoundaries = _requirePagingBoundaries,
                    ProviderName = ProviderName
                });
        }

        if (descriptor is IInterfaceFieldDescriptor idf)
        {
            idf.UseOffsetPaging(
                Type,
                options: new PagingOptions
                {
                    DefaultPageSize = _defaultPageSize,
                    MaxPageSize = _maxPageSize,
                    IncludeTotalCount = _includeTotalCount,
                    RequirePagingBoundaries = _requirePagingBoundaries,
                    ProviderName = ProviderName
                });
        }
    }
}
