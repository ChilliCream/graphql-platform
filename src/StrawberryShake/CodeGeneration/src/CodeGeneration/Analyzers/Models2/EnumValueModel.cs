using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models2
{
    public sealed class EnumValueModel
    {
        public EnumValueModel(
            string name,
            string? description,
            IEnumValue value,
            string? underlyingValue)
        {
            Name = name;
            Description = description;
            Value = value;
            UnderlyingValue = underlyingValue;
        }

        public string Name { get; }

        public string? Description { get; }

        public IEnumValue Value { get; }

        public string? UnderlyingValue { get; }
    }
}
