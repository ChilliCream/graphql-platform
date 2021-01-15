using System;
using HotChocolate;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public sealed class EnumValueModel
    {
        public EnumValueModel(
            NameString name,
            string? description,
            IEnumValue value,
            string? underlyingValue)
        {
            Name = name.EnsureNotEmpty(nameof(name));
            Description = description;
            Value = value ?? throw new ArgumentNullException(nameof(value));
            UnderlyingValue = underlyingValue;
        }

        public NameString Name { get; }

        public string? Description { get; }

        public IEnumValue Value { get; }

        public string? UnderlyingValue { get; }
    }
}
