using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@schemaName</c> directive is used to specify the name of a <i>source schema</i>.
/// </summary>
internal sealed class SchemaNameMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public SchemaNameMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(WellKnownDirectiveNames.SchemaName)
    {
        Description = SchemaNameMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Value,
                new NonNullType(stringType))
            {
                Description = SchemaNameMutableDirectiveDefinition_Argument_Value_Description
            });

        Locations = DirectiveLocation.Schema;
    }
}
