using System.Collections.Generic;

namespace HotChocolate.Language;

public interface IHasWithFields<out TNode>
{
    TNode WithFields(IReadOnlyList<FieldDefinitionNode> fields);
}
