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

        public DirectiveReference(NameString name)
        {
            if (name.IsEmpty)
            {
                throw new ArgumentException(
                    TypeResources.Name_CannotBe_Empty(),
                    nameof(name));
            }

            Name = name;
        }

        public Type ClrType { get; }

        public string Name { get; }

        internal static DirectiveReference FromDescription(
            DirectiveDescription description)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (description.ParsedDirective != null)
            {
                return new DirectiveReference(
                    description.ParsedDirective.Name.Value);
            }

            return new DirectiveReference(
                description.CustomDirective.GetType());
        }
    }
}
