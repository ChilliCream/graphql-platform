using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class InterfaceTypeDefinitionNodeBase
        : IHasDirectives
    {
        protected InterfaceTypeDefinitionNodeBase(
            Location location,
            NameNode name,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<FieldDefinitionNode> fields)
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
        public IReadOnlyCollection<FieldDefinitionNode> Fields { get; }
    }
}
