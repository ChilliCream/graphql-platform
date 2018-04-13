using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class SchemaDefinitionNode
        : ITypeSystemDefinitionNode
    {
        public SchemaDefinitionNode(
            Location location, 
            IReadOnlyCollection<DirectiveNode> directives, 
            IReadOnlyCollection<OperationTypeDefinitionNode> operationTypes)
        {
            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            if (operationTypes == null)
            {
                throw new ArgumentNullException(nameof(operationTypes));
            }

            Location = location;
            Directives = directives;
            OperationTypes = operationTypes;
        }

        public NodeKind Kind { get; } = NodeKind.SchemaDefinition;
        public Location Location { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<OperationTypeDefinitionNode> OperationTypes { get; }
    }
}