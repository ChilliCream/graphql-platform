using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class FieldInfo
    {
        public FieldInfo(IOutputField field, FieldNode selection)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
        }

        public IOutputField Field { get; }
        public FieldNode Selection { get; }
    }
}
