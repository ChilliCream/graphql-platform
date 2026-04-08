using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class CostMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public CostMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(DirectiveNames.Cost.Name)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                DirectiveNames.Cost.Arguments.Weight,
                new NonNullType(stringType)));

        Locations =
            DirectiveLocation.ArgumentDefinition
            | DirectiveLocation.Enum
            | DirectiveLocation.FieldDefinition
            | DirectiveLocation.InputFieldDefinition
            | DirectiveLocation.Object
            | DirectiveLocation.Scalar;
    }

    public static CostMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.String.Name, out var stringType))
        {
            stringType = BuiltIns.String.Create();
        }

        return new CostMutableDirectiveDefinition(stringType);
    }
}
