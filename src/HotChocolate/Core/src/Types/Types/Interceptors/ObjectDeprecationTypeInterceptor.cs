using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Interceptors;

internal sealed class ObjectDeprecationTypeInterceptor : TypeInterceptor
{
    public override bool IsEnabled(IDescriptorContext context)
        => context.Options.EnableObjectDeprecation;

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (configuration is DirectiveTypeConfiguration directiveType
            && directiveType.Name.Equals(DirectiveNames.Deprecated.Name, StringComparison.Ordinal))
        {
            directiveType.Locations |= DirectiveLocation.Object;
        }
    }
}
