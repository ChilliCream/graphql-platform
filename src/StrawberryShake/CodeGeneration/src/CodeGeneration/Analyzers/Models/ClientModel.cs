using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public class ClientModel
    {
        public ClientModel(
            IReadOnlyList<OperationModel> operations,
            IReadOnlyList<LeafTypeModel> leafTypes,
            IReadOnlyList<InputObjectTypeModel> inputObjectTypes)
        {
            Operations = operations;
            LeafTypes = leafTypes;
            InputObjectTypes = inputObjectTypes;
        }

        public IReadOnlyList<OperationModel> Operations { get; }

        /// <summary>
        /// Gets the leaf types that are used by this operation.
        /// </summary>
        public IReadOnlyList<LeafTypeModel> LeafTypes { get; }

        public IReadOnlyList<InputObjectTypeModel> InputObjectTypes { get; }
    }
}
