using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    public interface ISchemaMergeContext
    {
        void AddType(ITypeDefinitionNode type);

        void AddDirective(DirectiveDefinitionNode directive);

        bool ContainsType(NameString typeName);

        bool ContainsDirective(NameString directiveName);
    }
}
