using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// Sets the stability level of an opt-in feature.
/// </summary>
public sealed class OptInFeatureStabilityDirectiveType
    : DirectiveType<OptInFeatureStabilityDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<OptInFeatureStabilityDirective> descriptor)
    {
        descriptor
            .Name(WellKnownDirectives.OptInFeatureStability)
            .Description(TypeResources.OptInFeatureStabilityDirectiveType_TypeDescription)
            .Location(DirectiveLocation.Schema)
            .Repeatable();

        descriptor
            .Argument(t => t.Feature)
            .Name(WellKnownDirectives.OptInFeatureStabilityFeatureArgument)
            .Description(TypeResources.OptInFeatureStabilityDirectiveType_FeatureDescription)
            .Type<NonNullType<StringType>>();

        descriptor
            .Argument(t => t.Stability)
            .Name(WellKnownDirectives.OptInFeatureStabilityStabilityArgument)
            .Description(TypeResources.OptInFeatureStabilityDirectiveType_StabilityDescription)
            .Type<NonNullType<StringType>>();
    }
}
