using System;
using HotChocolate;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public class LeafTypeModel : ITypeModel
    {
        public LeafTypeModel(
            NameString name,
            string? description,
            ILeafType type,
            string serializationType,
            string runtimeType)
        {
            Name = name.EnsureNotEmpty(nameof(name));
            Description = description;
            Type = type ?? 
                throw new ArgumentNullException(nameof(type));
            SerializationType = serializationType ?? 
                throw new ArgumentNullException(nameof(serializationType));
            RuntimeType = runtimeType ?? 
                throw new ArgumentNullException(nameof(runtimeType));
        }

        /// <summary>
        /// Gets the enum name.
        /// </summary>
        public NameString Name { get; }

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
