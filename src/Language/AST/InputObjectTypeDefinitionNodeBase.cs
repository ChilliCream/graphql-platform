using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class InputObjectTypeDefinitionNodeBase
        : IHasDirectives
    {
        protected InputObjectTypeDefinitionNodeBase(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<InputValueDefinitionNode> fields)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            Location = location;
            Name = name;
            Directives = directives;
            Fields = fields;
        }

        public abstract NodeKind Kind { get; }
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<InputValueDefinitionNode> Fields { get; }
    }
}
