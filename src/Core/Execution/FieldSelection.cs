using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public class FieldSelection
    {
        public FieldSelection(FieldNode node, Field field, string responseName)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (string.IsNullOrEmpty(responseName))
            {
                throw new ArgumentNullException(nameof(responseName));
            }

            Node = node;
            Field = field;
            ResponseName = responseName;
        }

        public string ResponseName { get; }
        public Field Field { get; }
        public FieldNode Node { get; }
    }
}
