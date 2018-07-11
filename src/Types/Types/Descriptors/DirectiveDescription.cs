using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class DirectiveDescription
    {
        public DirectiveDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public HashSet<DirectiveLocation> Locations { get; } =
            new HashSet<DirectiveLocation>();

        public List<ArgumentDescription> Arguments { get; } =
            new List<ArgumentDescription>();
    }
}
