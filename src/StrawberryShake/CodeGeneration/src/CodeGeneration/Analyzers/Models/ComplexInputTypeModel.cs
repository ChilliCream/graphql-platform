using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public class ComplexInputTypeModel : ITypeModel
    {
        public ComplexInputTypeModel(
            string name,
            string? description,
            INamedType type,
            IReadOnlyList<InputFieldModel> fields)
        {
            Name = name;
            Description = description;
            Type = type;
            Fields = fields;
        }

        public string Name { get; }

        public string? Description { get; }

        public INamedType Type { get; }

        public IReadOnlyList<InputFieldModel> Fields { get; }
    }
}
