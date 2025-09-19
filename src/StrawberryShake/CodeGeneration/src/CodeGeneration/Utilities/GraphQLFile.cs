using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Utilities;

public class GraphQLFile
{
    public GraphQLFile(DocumentNode document)
        : this(Guid.NewGuid().ToString("N"), document)
    {
    }

    public GraphQLFile(string fileName, DocumentNode document)
    {
        FileName = fileName;
        Document = document;
    }

    public string FileName { get; }

    public DocumentNode Document { get; }
}
