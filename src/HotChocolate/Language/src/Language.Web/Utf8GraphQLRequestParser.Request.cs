namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLRequestParser
{
    internal ref struct Request
    {
        public string? OperationName { get; set; }

        public OperationDocumentId? DocumentId { get; set; }

        public OperationDocumentHash? DocumentHash { get; set; }

        public ReadOnlySpan<byte> DocumentBody { get; set; }

        public bool ContainsDocument { get; set; }

        public IReadOnlyList<IReadOnlyDictionary<string, object?>>? Variables { get; set; }

        public IReadOnlyDictionary<string, object?>? Extensions { get; set; }

        public DocumentNode? Document { get; set; }
    }
}
