using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.Properties.CompositionResources;
using ArgumentNames = HotChocolate.Types.DirectiveNames.RequiresOptIn.Arguments;

namespace HotChocolate.Fusion.Directives;

internal sealed class RequiresOptInDirective(string feature)
{
    public string Feature { get; } = feature;

    public static RequiresOptInDirective From(IDirective directive)
    {
        if (!directive.Arguments.TryGetValue(ArgumentNames.Feature, out var featureArg)
            || featureArg is not StringValueNode feature)
        {
            throw new InvalidOperationException(RequiresOptInDirective_FeatureArgument_Invalid);
        }

        return new RequiresOptInDirective(feature.Value);
    }
}
