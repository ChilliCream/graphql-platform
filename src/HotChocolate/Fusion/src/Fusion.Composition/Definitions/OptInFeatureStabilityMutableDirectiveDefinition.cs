using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class OptInFeatureStabilityMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public OptInFeatureStabilityMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(DirectiveNames.OptInFeatureStability.Name)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                DirectiveNames.OptInFeatureStability.Arguments.Feature,
                new NonNullType(stringType)));

        Arguments.Add(
            new MutableInputFieldDefinition(
                DirectiveNames.OptInFeatureStability.Arguments.Stability,
                new NonNullType(stringType)));

        IsRepeatable = true;

        Locations = DirectiveLocation.Schema;
    }

    public static OptInFeatureStabilityMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(
            SpecScalarNames.String.Name,
            out var stringType))
        {
            stringType = BuiltIns.String.Create();
        }

        return new OptInFeatureStabilityMutableDirectiveDefinition(stringType);
    }
}
