using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models2
{
    public class OutputFieldModel : IFieldModel
    {
        public OutputFieldModel(
            string name,
            string? description,
            IOutputField field,
            IOutputType type,
            FieldNode selection,
            Path path)
        {
            Name = name;
            Description = description;
            Field = field;
            Selection = selection;
            Path = path;
            Type = type;
        }

        public string Name { get; }

        public string? Description { get; }

        public IOutputField Field { get; }

        IField IFieldModel.Field => Field;

        public IOutputType Type { get; }

        IType IFieldModel.Type => Type;

        public FieldNode Selection { get; }

        public Path Path { get; }
    }
}
