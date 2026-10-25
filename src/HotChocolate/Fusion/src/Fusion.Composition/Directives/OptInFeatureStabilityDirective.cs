using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.Properties.CompositionResources;
using ArgumentNames = HotChocolate.Types.DirectiveNames.OptInFeatureStability.Arguments;

namespace HotChocolate.Fusion.Directives;

internal sealed class OptInFeatureStabilityDirective(string feature, string stability)
{
    public string Feature { get; } = feature;

    public string Stability { get; } = stability;

    public static OptInFeatureStabilityDirective From(IDirective directive)
    {
        if (!directive.Arguments.TryGetValue(ArgumentNames.Feature, out var featureArg)
            || featureArg is not StringValueNode feature
            || !directive.Arguments.TryGetValue(ArgumentNames.Stability, out var stabilityArg)
            || stabilityArg is not StringValueNode stability)
        {
            throw new InvalidOperationException(OptInFeatureStabilityDirective_Arguments_Invalid);
        }

        return new OptInFeatureStabilityDirective(feature.Value, stability.Value);
    }
}
