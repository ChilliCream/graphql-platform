using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InterfaceFieldDescription
    {
        public FieldDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public TypeReference TypeReference { get; set; }

        public string DeprecationReason { get; set; }

        public bool Ignored { get; set; }

        public List<ArgumentDescription> Arguments { get; set; }
            = new List<ArgumentDescription>();
    }
}
