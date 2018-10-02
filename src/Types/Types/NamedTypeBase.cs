using System;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public class NamedTypeBase
        : TypeBase
        , INamedType
        , IHasDirectives
    {
        protected NamedTypeBase(TypeKind kind)
            : base(kind)
        {
        }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public IDirectiveCollection Directives { get; private set; }

        protected void Initialize(
            string name,
            string description,
            IDirectiveCollection directives)
        {
            if (string.IsNullOrEmpty(name)
                || !ValidationHelper.IsTypeNameValid(Name))
            {
                throw new ArgumentException(
                    "Named types have to have a valid type name.",
                    nameof(name));
            }

            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            Name = name;
            Description = description;
            Directives = directives;

            if (directives is INeedsInitialization init)
            {
                RegisterForInitialization(init);
            }
        }
    }
}
