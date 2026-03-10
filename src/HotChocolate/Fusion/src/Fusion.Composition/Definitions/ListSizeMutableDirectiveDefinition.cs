using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class ListSizeMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public ListSizeMutableDirectiveDefinition(
        MutableScalarTypeDefinition intType,
        MutableScalarTypeDefinition stringType,
        MutableScalarTypeDefinition booleanType)
        : base(DirectiveNames.ListSize.Name)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                DirectiveNames.ListSize.Arguments.AssumedSize,
                intType));

        Arguments.Add(
            new MutableInputFieldDefinition(
                DirectiveNames.ListSize.Arguments.SlicingArguments,
                new ListType(new NonNullType(stringType))));

        Arguments.Add(
            new MutableInputFieldDefinition(
                DirectiveNames.ListSize.Arguments.SlicingArgumentDefaultValue,
                intType));

        Arguments.Add(
            new MutableInputFieldDefinition(
                DirectiveNames.ListSize.Arguments.SizedFields,
                new ListType(new NonNullType(stringType))));

        Arguments.Add(
            new MutableInputFieldDefinition(
                DirectiveNames.ListSize.Arguments.RequireOneSlicingArgument,
                booleanType));

        Locations = DirectiveLocation.FieldDefinition;
    }

    public static ListSizeMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.Int.Name, out var intType))
        {
            intType = BuiltIns.Int.Create();
        }

        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.String.Name, out var stringType))
        {
            stringType = BuiltIns.String.Create();
        }

        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(SpecScalarNames.Boolean.Name, out var booleanType))
        {
            booleanType = BuiltIns.Boolean.Create();
        }

        return new ListSizeMutableDirectiveDefinition(intType, stringType, booleanType);
    }
}
