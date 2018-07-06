using System;
using System.Diagnostics;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    [DebuggerDisplay("{Field.Name}: {Field.Type}")]
    public class FieldSelection
    {
        public FieldSelection(FieldNode node, ObjectField field, string responseName)
        {
            if (string.IsNullOrEmpty(responseName))
            {
                throw new ArgumentNullException(nameof(responseName));
            }

            Node = node ?? throw new ArgumentNullException(nameof(node));
            Field = field ?? throw new ArgumentNullException(nameof(field));
            ResponseName = responseName;
        }

        public string ResponseName { get; }

        public ObjectField Field { get; }

        public FieldNode Node { get; }
    }
}
