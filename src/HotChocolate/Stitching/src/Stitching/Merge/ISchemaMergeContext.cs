using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge;

public interface ISchemaMergeContext
{
    void AddType(ITypeDefinitionNode type);

    void AddDirective(DirectiveDefinitionNode directive);

    bool ContainsType(string typeName);

    bool ContainsDirective(string directiveName);
}
