using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
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
        /// Gets the value representing the instance of the
        /// <see cref="RootType" />
        /// </summary>
        object RootValue { get; }

        /// <summary>
        /// Gets the root type on which the operation is executed.
        /// </summary>
        ObjectType RootType { get; }

        /// <summary>
        /// Gets the name of the operation.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the operation type (Query, Mutation, Subscription).
        /// </summary>
        OperationType Type { get; }

        /// <summary>
        /// Gets the variable values for this operation.
        /// </summary>
        /// <value></value>
        IVariableValueCollection Variables { get; }
    }
}
