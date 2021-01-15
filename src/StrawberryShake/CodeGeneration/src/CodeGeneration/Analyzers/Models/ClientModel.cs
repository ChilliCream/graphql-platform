using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    /// <summary>
    /// The client model represents the client with all its operations and types.
    /// </summary>
    public class ClientModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ClientModel" />.
        /// </summary>
        /// <param name="operations">
        /// The operations that the client has defined.
        /// </param>
        /// <param name="leafTypes">
        /// The leaf types that are used.
        /// </param>
        /// <param name="inputObjectTypes">
        /// The input types that could be passed in.
        /// </param>
        public ClientModel(
            IReadOnlyList<OperationModel> operations,
            IReadOnlyList<LeafTypeModel> leafTypes,
            IReadOnlyList<InputObjectTypeModel> inputObjectTypes)
        {
            Operations = operations ?? 
                throw new ArgumentNullException(nameof(operations));
            LeafTypes = leafTypes ?? 
                throw new ArgumentNullException(nameof(leafTypes));
            InputObjectTypes = inputObjectTypes ?? 
                throw new ArgumentNullException(nameof(inputObjectTypes));
        }

        /// <summary>
        /// Gets the operations
        /// </summary>
        public IReadOnlyList<OperationModel> Operations { get; }

        /// <summary>
        /// Gets the leaf types that are used by this operation.
        /// </summary>
        public IReadOnlyList<LeafTypeModel> LeafTypes { get; }

        /// <summary>
        /// Gets the input objects that are needed.
        /// </summary>
        public IReadOnlyList<InputObjectTypeModel> InputObjectTypes { get; }
    }
}
