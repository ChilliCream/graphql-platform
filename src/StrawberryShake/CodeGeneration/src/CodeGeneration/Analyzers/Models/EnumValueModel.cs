using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public class EnumValueModel
    {
        public EnumValueModel(
            string name,
            EnumValue value,
            string? description,
            string? underlyingValue)
        {
            Name = name;
            Value = value;
            Description = description;
            UnderlyingValue = underlyingValue;
        }

        public string Name { get; }

        public EnumValue Value { get; }

        public string? Description { get; }

        public string? UnderlyingValue { get; }
    }
}
