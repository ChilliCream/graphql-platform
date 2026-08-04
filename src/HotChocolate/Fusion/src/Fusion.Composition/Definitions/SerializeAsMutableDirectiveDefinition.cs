using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class SerializeAsMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public SerializeAsMutableDirectiveDefinition(
        MutableEnumTypeDefinition scalarSerializationTypeType,
        MutableScalarTypeDefinition stringType)
        : base(DirectiveNames.SerializeAs.Name)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                DirectiveNames.SerializeAs.Arguments.Type,
                new NonNullType(new ListType(new NonNullType(scalarSerializationTypeType)))));
        Arguments.Add(new MutableInputFieldDefinition(BuiltIns.SerializeAs.Pattern, stringType));
        Locations = DirectiveLocation.Scalar;
    }

    public static SerializeAsMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableEnumTypeDefinition>(
            WellKnownTypeNames.ScalarSerializationType,
            out var scalarSerializationTypeType))
        {
            scalarSerializationTypeType = ScalarSerializationTypeMutableEnumTypeDefinition.Create();
        }

        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(
            SpecScalarNames.String.Name,
            out var stringType))
        {
            stringType = BuiltIns.String.Create();
        }

        return new SerializeAsMutableDirectiveDefinition(scalarSerializationTypeType, stringType);
    }
}
