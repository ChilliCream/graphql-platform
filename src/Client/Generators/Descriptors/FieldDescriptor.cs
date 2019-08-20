using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{

    public class FieldDescriptor
        : IFieldDescriptor
    {
        public FieldDescriptor(IOutputField field, FieldNode selection, IType type)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            Type = type ?? throw new ArgumentNullException(nameof(type));

            NameNode responseName = selection.Alias ?? selection.Name;
            ResponseName = responseName.Value;
        }

        public string ResponseName { get; }
        public IOutputField Field { get; }
        public FieldNode Selection { get; }
        public IType Type { get; }
    }
}
