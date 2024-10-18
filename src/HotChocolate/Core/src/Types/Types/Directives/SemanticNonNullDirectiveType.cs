#nullable enable
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types;

public class SemanticNonNullDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor
            .Name(WellKnownDirectives.SemanticNonNull)
            .Description(SemanticNonNullDirectiveType_Description)
            .Location(DirectiveLocation.FieldDefinition);

        descriptor
            .Argument(WellKnownDirectives.Levels)
            .Description(SemanticNonNullDirectiveType_Levels_Description)
            .Type<ListType<IntType>>()
            .DefaultValueSyntax("[0]");
    }
}
