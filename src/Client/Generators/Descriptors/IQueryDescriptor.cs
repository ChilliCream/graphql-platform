using HotChocolate.Language;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IQueryDescriptor
        : ICodeDescriptor
    {
        string HashName { get; }

        string Hash { get; }

        byte[] Document { get; }

        DocumentNode OriginalDocument { get; }
    }
}
