using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InterfaceTypeDescription
    {
        public InterfaceTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ResolveAbstractType ResolveAbstractType { get; set; }

        public List<InterfaceFieldDescription> Fields { get; set; } =
            new List<InterfaceFieldDescription>();
    }
}
