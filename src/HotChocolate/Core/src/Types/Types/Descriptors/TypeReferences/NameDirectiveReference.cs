namespace HotChocolate.Types.Descriptors
{
    public sealed class NameDirectiveReference
        : IDirectiveReference
    {
        public NameDirectiveReference(NameString name)
        {
            Name = name.EnsureNotEmpty(nameof(name));
        }

        public NameString Name { get; }
    }
}
