using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding.Rewriters;

public interface ITypeRewriter
{
    ITypeDefinitionNode Rewrite(
        ISchemaInfo schema,
        ITypeDefinitionNode typeDefinition);
}
