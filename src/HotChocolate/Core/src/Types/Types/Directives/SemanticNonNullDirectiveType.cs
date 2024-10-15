#nullable enable
namespace HotChocolate.Types;

public class SemanticNonNullDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor
            .Name(Names.SemanticNonNull)
            .Description("")
            .Location(DirectiveLocation.FieldDefinition);

        descriptor
            .Argument(Names.Levels)
            .Description("")
            .Type<ListType<IntType>>()
            .DefaultValueSyntax("[0]");
    }

    public static class Names
    {
        public const string SemanticNonNull = "semanticNonNull";
        public const string Levels = "levels";
    }
}
