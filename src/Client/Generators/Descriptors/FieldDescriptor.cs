using System;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{

    public class FieldDescriptor
        : IFieldDescriptor
    {
        public FieldDescriptor(IOutputField field, FieldNode selection, IType type, Path path)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Path = path;

            NameNode responseName = selection.Alias ?? selection.Name;
            ResponseName = responseName.Value;
        }

        public string ResponseName { get; }

        public Path Path { get; }

        public IOutputField Field { get; }

        public FieldNode Selection { get; }

        public IType Type { get; }
    }
}
