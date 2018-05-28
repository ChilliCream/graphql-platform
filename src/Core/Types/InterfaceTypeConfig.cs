using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InterfaceTypeConfig
    {
        public InterfaceTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<Field> Fields { get; set; }

        public ResolveAbstractType ResolveAbstractType { get; set; }
    }
}
