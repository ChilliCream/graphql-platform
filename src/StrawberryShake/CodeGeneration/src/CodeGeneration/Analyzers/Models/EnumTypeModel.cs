using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public sealed class EnumTypeModel
        : ITypeModel
    {
        public EnumTypeModel(
            string name,
            string? description,
            INamedType type,
            string? underlyingType,
            IReadOnlyList<EnumValueModel> values)
        {
            Name = name;
            Description = description;
            Type = type;
            UnderlyingType = underlyingType;
            Values = values;
        }

        public string Name { get; }

        public string? Description { get; }

        public INamedType Type { get; }

        public string? UnderlyingType { get; }

        public IReadOnlyList<EnumValueModel> Values { get; }
    }
}
