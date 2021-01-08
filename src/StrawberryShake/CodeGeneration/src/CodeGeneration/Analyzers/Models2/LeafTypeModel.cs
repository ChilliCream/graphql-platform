using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models2
{
    public class LeafTypeModel : ITypeModel
    {
        public LeafTypeModel(
            string name,
            string? description,
            ILeafType type,
            string serializationType,
            string runtimeType)
        {
            Type = type;
            SerializationType = serializationType;
            RuntimeType = runtimeType;
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Gets the enum name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the enum xml documentation summary.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the leaf type.
        /// </summary>
        public ILeafType Type { get; }

        INamedType ITypeModel.Type => Type;

        public string SerializationType { get; }

        public string RuntimeType { get; }
    }
}
