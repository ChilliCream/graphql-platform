using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public class InputFieldModel
        : IFieldModel
    {
        public InputFieldModel(
            string name,
            string? description,
            IInputField field,
            IInputType type)
        {
            Name = name;
            Description = description;
            Field = field;
            Type = type;
        }

        public string Name { get; }

        public string? Description { get; }

        public IInputField Field { get; }

        IField IFieldModel.Field => Field;

        public IInputType Type { get; }

        IType IFieldModel.Type => Type;
    }
}
