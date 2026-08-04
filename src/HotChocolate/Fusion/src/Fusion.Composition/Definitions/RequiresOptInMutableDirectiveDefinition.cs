using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class RequiresOptInMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public RequiresOptInMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(DirectiveNames.RequiresOptIn.Name)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                DirectiveNames.RequiresOptIn.Arguments.Feature,
                new NonNullType(stringType)));

        IsRepeatable = true;

        Locations =
            DirectiveLocation.FieldDefinition
            | DirectiveLocation.ArgumentDefinition
            | DirectiveLocation.InputFieldDefinition
            | DirectiveLocation.EnumValue
            | DirectiveLocation.DirectiveDefinition;
    }

    public static RequiresOptInMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(
            SpecScalarNames.String.Name,
            out var stringType))
        {
            stringType = BuiltIns.String.Create();
        }

        return new RequiresOptInMutableDirectiveDefinition(stringType);
    }
}
