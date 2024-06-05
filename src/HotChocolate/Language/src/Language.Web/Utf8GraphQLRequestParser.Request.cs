namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLRequestParser
{
    private ref struct Request
    {
        public string? OperationName { get; set; }

        public string? QueryId { get; set; }

        public string? QueryHash { get; set; }

        public ReadOnlySpan<byte> Query { get; set; }

        public bool HasQuery { get; set; }

        public IReadOnlyList<IReadOnlyDictionary<string, object?>>? Variables { get; set; }

        public IReadOnlyDictionary<string, object?>? Extensions { get; set; }

        public DocumentNode? Document { get; set; }
    }
}
