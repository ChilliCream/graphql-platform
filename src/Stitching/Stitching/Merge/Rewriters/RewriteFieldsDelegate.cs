using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    internal delegate T RewriteFieldsDelegate<T>(
        IReadOnlyList<FieldDefinitionNode> fields)
        where T : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode;
}
