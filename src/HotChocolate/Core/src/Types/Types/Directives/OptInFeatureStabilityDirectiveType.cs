using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// Sets the stability level of an opt-in feature.
/// </summary>
public sealed class OptInFeatureStabilityDirectiveType
    : DirectiveType<OptInFeatureStability>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<OptInFeatureStability> descriptor)
    {
        descriptor
            .Name(DirectiveNames.OptInFeatureStability.Name)
            .Description(TypeResources.OptInFeatureStabilityDirectiveType_TypeDescription)
            .Location(DirectiveLocation.Schema)
            .Repeatable();

        descriptor
            .Argument(t => t.Feature)
            .Name(DirectiveNames.OptInFeatureStability.Arguments.Feature)
            .Description(TypeResources.OptInFeatureStabilityDirectiveType_FeatureDescription)
            .Type<NonNullType<StringType>>();

        descriptor
            .Argument(t => t.Stability)
            .Name(DirectiveNames.OptInFeatureStability.Arguments.Stability)
            .Description(TypeResources.OptInFeatureStabilityDirectiveType_StabilityDescription)
            .Type<NonNullType<StringType>>();
    }
}
