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

        public abstract NodeKind Kind { get; }

        public Location Location { get; }

        public IReadOnlyCollection<DirectiveNode> Directives { get; }

        public IReadOnlyCollection<OperationTypeDefinitionNode> OperationTypes
        { get; }
    }
}
