using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

public abstract class SchemaDefinitionNodeBase
    : IHasDirectives
{
    protected SchemaDefinitionNodeBase(
        Location? location,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
    {
        Directives = directives
            ?? throw new ArgumentNullException(nameof(directives));
        OperationTypes = operationTypes
            ?? throw new ArgumentNullException(nameof(operationTypes));

        Location = location;
        Directives = directives;
        OperationTypes = operationTypes;
    }

    public abstract SyntaxKind Kind { get; }

    public Location? Location { get; }

    public IReadOnlyList<DirectiveNode> Directives { get; }

    public IReadOnlyList<OperationTypeDefinitionNode> OperationTypes { get; }
}
