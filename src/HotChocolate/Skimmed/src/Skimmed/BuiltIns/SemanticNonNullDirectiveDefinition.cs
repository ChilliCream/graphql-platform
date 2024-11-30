using HotChocolate.Language;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Skimmed;

public sealed class SemanticNonNullDirectiveDefinition : DirectiveDefinition
{
    internal SemanticNonNullDirectiveDefinition(ScalarTypeDefinition intType)
        : base(BuiltIns.SemanticNonNull.Name)
    {
        var levelsArgument = new InputFieldDefinition(BuiltIns.SemanticNonNull.Levels, new ListTypeDefinition(intType));
        levelsArgument.DefaultValue = new ListValueNode(new IntValueNode(0));
        Arguments.Add(levelsArgument);

        Locations = DirectiveLocation.FieldDefinition;
    }
}
