using System;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public class OutputFieldModel : IFieldModel
    {
        public OutputFieldModel(
            NameString name,
            string? description,
            IOutputField field,
            IOutputType type,
            FieldNode syntaxNode,
            Path path)
        {
            Name = name.EnsureNotEmpty(nameof(name));
            Description = description;
            Field = field ?? throw new ArgumentNullException(nameof(field));
            SyntaxNode = syntaxNode ?? throw new ArgumentNullException(nameof(syntaxNode));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public NameString Name { get; }

        public string? Description { get; }

        public IOutputField Field { get; }

        IField IFieldModel.Field => Field;

        public IOutputType Type { get; }

        IType IFieldModel.Type => Type;

        public FieldNode SyntaxNode { get; }

        public Path Path { get; }
    }
}
