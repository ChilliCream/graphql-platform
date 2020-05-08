using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class InputUnionTypeDefinition
        : TypeDefinitionBase<InputUnionTypeDefinitionNode>
    {
        public IList<ITypeReference> Types { get; } =
            new List<ITypeReference>();
    }
}
