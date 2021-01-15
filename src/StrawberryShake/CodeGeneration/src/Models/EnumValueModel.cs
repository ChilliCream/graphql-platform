using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public sealed class EnumValueModel
    {
        public EnumValueModel(
            string name,
            IEnumValue value,
            string? description,
            string? underlyingValue)
        {
            Name = name;
            Value = value;
            Description = description;
            UnderlyingValue = underlyingValue;
        }

        public string Name { get; }

        public IEnumValue Value { get; }

        public string? Description { get; }

        public string? UnderlyingValue { get; }
    }
}
