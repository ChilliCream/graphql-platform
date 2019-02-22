using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ObjectFieldDescriptionBase
        : FieldDescriptionBase
    {
        public FieldDefinitionNode SyntaxNode { get; set; }

        public string DeprecationReason { get; set; }

        public List<ArgumentDescription> Arguments { get; set; } =
            new List<ArgumentDescription>();
    }
}
