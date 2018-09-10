using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class DirectiveReference
    {
        public DirectiveReference(Type clrType)
        {
            ClrType = clrType
                ?? throw new ArgumentNullException(nameof(clrType));
        }

        public DirectiveReference(string name)
        {
            name.EnsureDirectiveNameIsValid();
            Name = name;
        }

        public Type ClrType { get; }

        public string Name { get; }

        internal static DirectiveReference FromObject(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (obj is DirectiveNode node)
            {
                return new DirectiveReference(node.Name.Value);
            }

            return new DirectiveReference(obj.GetType());
        }
    }
}
