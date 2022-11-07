using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    public interface ITypeRewriter
    {
        ITypeDefinitionNode Rewrite(
            ISchemaInfo schema,
            ITypeDefinitionNode typeDefinition);
    }
}
