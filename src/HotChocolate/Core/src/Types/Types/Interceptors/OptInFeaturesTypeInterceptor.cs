using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Interceptors;

internal sealed class OptInFeaturesTypeInterceptor : TypeInterceptor
{
    public override bool IsEnabled(IDescriptorContext context)
        => context.Options.EnableOptInFeatures;

    private readonly OptInFeatures _optInFeatures = [];

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (configuration is SchemaTypeConfiguration schema)
        {
            schema.Features.Set(_optInFeatures);
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        switch (configuration)
        {
            case EnumTypeConfiguration enumType:
                _optInFeatures.UnionWith(enumType.Values.SelectMany(v => v.GetOptInFeatures()));

                break;

            case InputObjectTypeConfiguration inputType:
                _optInFeatures.UnionWith(inputType.Fields.SelectMany(f => f.GetOptInFeatures()));

                break;

            case ObjectTypeConfiguration objectType:
                _optInFeatures.UnionWith(objectType.Fields.SelectMany(f => f.GetOptInFeatures()));

                _optInFeatures.UnionWith(objectType.Fields.SelectMany(
                    f => f.Arguments.SelectMany(a => a.GetOptInFeatures())));

                break;
        }
    }
}

file static class Extensions
{
    public static IEnumerable<string> GetOptInFeatures(
        this IDirectiveConfigurationProvider configuration)
    {
        return configuration.Directives
            .Select(d => d.Value)
            .OfType<RequiresOptInDirective>()
            .Select(r => r.Feature);
    }
}

internal sealed class OptInFeatures : SortedSet<string>;
