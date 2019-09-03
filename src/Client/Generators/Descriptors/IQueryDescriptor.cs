using HotChocolate.Language;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IQueryDescriptor
        : ICodeDescriptor
        , IHasNamespace
    {
        string HashName { get; }

        string Hash { get; }

        byte[] Document { get; }

        DocumentNode OriginalDocument { get; }
    }
}
