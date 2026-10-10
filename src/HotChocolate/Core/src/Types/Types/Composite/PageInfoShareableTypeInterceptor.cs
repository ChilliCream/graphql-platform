using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Composite;

/// <summary>
/// Marks the <c>PageInfo</c> and <c>PageCursor</c> types as <c>@shareable</c> when
/// <see cref="IReadOnlySchemaOptions.ApplyShareableToPageInfo"/> is enabled.
/// </summary>
internal sealed class PageInfoShareableTypeInterceptor : TypeInterceptor
{
    private const string PageInfoTypeName = "PageInfo";
    private const string PageCursorTypeName = "PageCursor";

    public override bool IsEnabled(IDescriptorContext context)
        => context.Options.ApplyShareableToPageInfo;

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
        if (configuration is not ObjectTypeConfiguration
            {
                Name: PageInfoTypeName or PageCursorTypeName
            } typeConfiguration)
        {
            return;
        }

        if (typeConfiguration.Directives.Any(t => t.Value is Shareable))
        {
            return;
        }

        typeConfiguration.AddDirective(Shareable.Instance, discoveryContext.TypeInspector);
    }
}
