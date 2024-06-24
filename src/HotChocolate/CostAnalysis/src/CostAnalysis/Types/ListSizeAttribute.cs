using System.Collections.Immutable;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.CostAnalysis.Types;

/// <summary>
/// Applies the <c>@listSize</c> directive. The purpose of the <c>@listSize</c> directive is to
/// either inform the static analysis of the size of returned lists (if that information is
/// statically available), or to point the analysis to where to find that information.
/// </summary>
public sealed class ListSizeAttribute : ObjectFieldDescriptorAttribute
{
    private readonly int? _assumedSize;

    /// <summary>
    /// The maximum length of the list returned by this field.
    /// </summary>
    public int AssumedSize
    {
        get => _assumedSize ?? 0;
        init => _assumedSize = value;
    }

    /// <summary>
    /// The arguments of this field with numeric type that are slicing arguments. Their value
    /// determines the size of the returned list.
    /// </summary>
    public string[]? SlicingArguments { get; init; }

    /// <summary>
    /// The subfield(s) that the list size applies to.
    /// </summary>
    public string[]? SizedFields { get; init; }

    /// <summary>
    /// Whether to require a single slicing argument in the query. If that is not the case (i.e., if
    /// none or multiple slicing arguments are present), the static analysis will throw an error.
    /// </summary>
    public bool RequireOneSlicingArgument { get; init; } = true;

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        descriptor.Directive(
            new ListSizeDirective(
                _assumedSize,
                SlicingArguments?.ToImmutableArray(),
                SizedFields?.ToImmutableArray(),
                RequireOneSlicingArgument));
    }
}
