using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public abstract class SchemaDefinitionNodeBase
        : IHasDirectives
    {
        protected SchemaDefinitionNodeBase(
            Location location,
            IReadOnlyCollection<DirectiveNode> directives,
            IReadOnlyCollection<OperationTypeDefinitionNode> operationTypes)
        {
            Location = location;
            Directives = directives 
                ?? throw new ArgumentNullException(nameof(directives));
            OperationTypes = operationTypes 
                ?? throw new ArgumentNullException(nameof(operationTypes));
        }

        public abstract NodeKind Kind { get; }

        public Location Location { get; }

        public IReadOnlyCollection<DirectiveNode> Directives { get; }

        public IReadOnlyCollection<OperationTypeDefinitionNode> OperationTypes
        { get; }
    }
}
