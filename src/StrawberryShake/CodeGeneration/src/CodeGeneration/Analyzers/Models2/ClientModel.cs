using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.Analyzers.Models2
{
    public class ClientModel
    {
        public ClientModel(
            IReadOnlyList<OperationModel> operations,
            IReadOnlyList<EnumTypeModel> enumTypes,
            IReadOnlyList<InputObjectTypeModel> inputObjectTypes)
        {
            Operations = operations;
            EnumTypes = enumTypes;
            InputObjectTypes = inputObjectTypes;
        }

        public IReadOnlyList<OperationModel> Operations { get; }

        /// <summary>
        /// Gets the leaf types that are used by this operation.
        /// </summary>
        // TODO : expose runtime / serialized types
        public IReadOnlyList<EnumTypeModel> EnumTypes { get; }

        public IReadOnlyList<InputObjectTypeModel> InputObjectTypes { get; }
    }
}
