using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models2
{
    /// <summary>
    /// Represents an enum type model.
    /// </summary>
    public sealed class EnumTypeModel : LeafTypeModel
    {
        public EnumTypeModel(
            string name,
            string? description,
            EnumType type,
            string? underlyingType,
            IReadOnlyList<EnumValueModel> values)
            : base(name, description, type, TypeNames.SystemString, name)
        {
            Name = name;
            Description = description;
            Type = type;
            UnderlyingType = underlyingType;
            Values = values;
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
        /// Gets the enum type.
        /// </summary>
        public new EnumType Type { get; }

        /// <summary>
        /// Gets the underlying type name.
        /// </summary>
        public string? UnderlyingType { get; }

        /// <summary>
        /// Gets the enum values models.
        /// </summary>
        public IReadOnlyList<EnumValueModel> Values { get; }
    }
}
