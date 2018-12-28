using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public interface IOperation
    {

        /// <summary>
        /// Gets the value representing the instance of the
        /// <see cref="RootType" />
        /// </summary>
        /// <value></value>
        object RootValue { get; }

        /// <summary>
        /// Gets the root type on which the operation is executed.
        /// </summary>
        ObjectType RootType { get; }

        /// <summary>
        /// Gets the parsed query document that contains the
        /// operation-<see cref="Node" />.
        /// </summary>
        /// <value></value>
        DocumentNode Query { get; }

        /// <summary>
        /// Gets the syntax node representing the operation definition.
        /// </summary>
        OperationDefinitionNode Node { get; }
    }
}
