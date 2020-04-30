using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using PSS = HotChocolate.Execution.Utilities.PreparedSelectionSet;

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

        IReadOnlyList<IPreparedSelection> GetSelections(
            SelectionSetNode selectionSet,
            ObjectType typeContext);
    }

    internal sealed class PreparedOperation : IPreparedOperation
    {
        private static IReadOnlyList<IPreparedSelection> _empty = new IPreparedSelection[0];
        private readonly IReadOnlyDictionary<SelectionSetNode, PSS> _selectionSets;

        public string Id { get; }

        public string? Name { get; }

        public DocumentNode Document { get; }

        public OperationDefinitionNode Definition { get; }

        public ObjectType RootType { get; }

        public OperationType Type { get; }

        public IReadOnlyList<IPreparedSelection> GetSelections(
            SelectionSetNode selectionSet,
            ObjectType typeContext)
        {
            if (_selectionSets.TryGetValue(selectionSet, out PSS? preparedSelectionSet))
            {
                return preparedSelectionSet.GetSelections(typeContext);
            }
            return _empty;
        }
    }
}
