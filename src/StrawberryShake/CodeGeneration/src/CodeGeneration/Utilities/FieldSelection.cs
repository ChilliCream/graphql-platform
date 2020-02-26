using System;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public class FieldSelection
        : IFieldSelection
    {
        public FieldSelection(IOutputField field, FieldNode selection, Path path)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            NameNode responseName = selection.Alias ?? selection.Name;
            ResponseName = responseName.Value;
        }

        public string ResponseName { get; }

        public IOutputField Field { get; }

        public FieldNode Selection { get; }

        public Path Path { get; }
    }
}
