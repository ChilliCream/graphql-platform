using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    public interface IPreparedOperation
    {
        /// <summary>
        /// Gets the internal unique identifier for this operation.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the name of the operation.
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Gets the parsed query document that contains the
        /// operation-<see cref="Definition" />.
        /// </summary>
        DocumentNode Document { get; }

        /// <summary>
        /// Gets the syntax node representing the operation definition.
        /// </summary>
        OperationDefinitionNode Definition { get; }

        /// <summary>
        /// Gets the root type on which the operation is executed.
        /// </summary>
        ObjectType RootType { get; }

        /// <summary>
        /// Gets the operation type (Query, Mutation, Subscription).
        /// </summary>
        OperationType Type { get; }

        IReadOnlyList<IPreparedSelection> GetFields(ObjectType type, SelectionSetNode selectionSet);
    }
}
