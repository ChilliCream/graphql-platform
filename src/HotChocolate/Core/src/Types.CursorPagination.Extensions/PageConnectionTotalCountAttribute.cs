using System.ComponentModel;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Pagination;

internal interface IPageConnectionTotalCountProvider
{
    int? TotalCount { get; }
}

/// <summary>
/// Configures the total count field of a page connection.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class PageConnectionTotalCountAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
        => descriptor
            .Extend()
            .Configuration
            .FormatterConfigurations
            .Add(
                new ResultFormatterConfiguration(
                    static (context, result) => result is -1
                        ? context.Parent<IPageConnectionTotalCountProvider>().TotalCount
                        : result,
                    isRepeatable: false,
                    key: "HotChocolate.Types.Pagination.PageConnectionTotalCount"));
}
