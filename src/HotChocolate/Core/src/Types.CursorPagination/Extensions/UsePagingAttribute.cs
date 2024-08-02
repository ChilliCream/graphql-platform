using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

/// <summary>
/// This attribute adds the cursor paging middleware to the annotated method or property.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
public sealed class UsePagingAttribute : DescriptorAttribute
{
    private string? _connectionName;
    private int? _defaultPageSize;
    private int? _maxPageSize;
    private bool? _includeTotalCount;
    private bool? _allowBackwardPagination;
    private bool? _requirePagingBoundaries;
    private bool? _inferConnectionNameFromField;

    /// <summary>
    /// Applies the cursor paging middleware to the annotated property.
    /// </summary>
    /// <param name="type">
    /// The schema type representing the item type.
    /// </param>
    /// <param name="order">
    /// The explicit order priority for this attribute.
    /// </param>
    public UsePagingAttribute(Type? type = null, [CallerLineNumber] int order = 0)
    {
        Type = type;
        Order = order;
    }

    /// <summary>
    /// The schema type representation of the node type.
    /// </summary>
    public Type? Type { get; private set; }

    /// <summary>
    /// Specifies the connection name.
    /// </summary>
    public string? ConnectionName
    {
        get => _connectionName;
        set => _connectionName = value;
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
    /// Specifies the name of the paging provider that shall be used.
    /// </summary>
    public string? ProviderName { get; set; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (element is not MemberInfo)
        {
            return;
        }

        var connectionName =
            string.IsNullOrEmpty(_connectionName)
                ? default!
                : _connectionName;
        var options =
            new PagingOptions
            {
                DefaultPageSize = _defaultPageSize,
                MaxPageSize = _maxPageSize,
                IncludeTotalCount = _includeTotalCount,
                AllowBackwardPagination = _allowBackwardPagination,
                RequirePagingBoundaries = _requirePagingBoundaries,
                InferConnectionNameFromField = _inferConnectionNameFromField,
                ProviderName = ProviderName
            };

        if (descriptor is IObjectFieldDescriptor ofd)
        {
            ofd.UsePaging(
                Type,
                connectionName: connectionName,
                options: options);
        }
        else if (descriptor is IInterfaceFieldDescriptor ifd)
        {
            ifd.UsePaging(
                Type,
                connectionName: connectionName,
                options: options);
        }
    }
}
