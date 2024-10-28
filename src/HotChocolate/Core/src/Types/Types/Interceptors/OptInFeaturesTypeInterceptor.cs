using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Interceptors;

internal sealed class OptInFeaturesTypeInterceptor : TypeInterceptor
{
    internal override bool IsEnabled(IDescriptorContext context)
        => context.Options.EnableOptInFeatures;

    private readonly OptInFeatures _optInFeatures = [];

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is SchemaTypeDefinition schema)
        {
            schema.Features.Set(_optInFeatures);
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        switch (definition)
        {
            case EnumTypeDefinition enumType:
                _optInFeatures.UnionWith(enumType.Values.SelectMany(v => v.GetOptInFeatures()));

                break;

            case InputObjectTypeDefinition inputType:
                _optInFeatures.UnionWith(inputType.Fields.SelectMany(f => f.GetOptInFeatures()));

                break;

            case ObjectTypeDefinition objectType:
                _optInFeatures.UnionWith(objectType.Fields.SelectMany(f => f.GetOptInFeatures()));

                _optInFeatures.UnionWith(objectType.Fields.SelectMany(
                    f => f.Arguments.SelectMany(a => a.GetOptInFeatures())));

                break;
        }
    }
}

file static class Extensions
{
    public static IEnumerable<string> GetOptInFeatures(this IHasDirectiveDefinition definition)
    {
        return definition.Directives
            .Select(d => d.Value)
            .OfType<RequiresOptInDirective>()
            .Select(r => r.Feature);
    }
}

internal sealed class OptInFeatures : SortedSet<string>;
