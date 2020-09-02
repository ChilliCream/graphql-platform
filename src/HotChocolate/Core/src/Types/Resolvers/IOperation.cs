using HotChocolate.Language;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers
{
    public interface IOperation
    {
        /// <summary>
        /// Gets the parsed query document that contains the
        /// operation-<see cref="Definition" />.
        /// </summary>
        /// <value></value>
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
        /// Gets the name of the operation.
        /// </summary>
        NameString? Name { get; }

        /// <summary>
        /// Gets the operation type (Query, Mutation, Subscription).
        /// </summary>
        OperationType Type { get; }
    }
}
