using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public class FieldSelection
    {
        public FieldSelection(FieldNode node, Field field, string name)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Node = node;
            Field = field;
            Name = name;
        }

        public string Name { get; }
        public Field Field { get; }
        public FieldNode Node { get; }
    }
}
