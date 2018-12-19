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

        public NameString Name { get; private set; }

        public string Description { get; private set; }

        public IDirectiveCollection Directives { get; private set; }

        protected void Initialize(
            NameString name,
            string description,
            IDirectiveCollection directives)
        {
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
