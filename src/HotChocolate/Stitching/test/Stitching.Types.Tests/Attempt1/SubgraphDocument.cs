using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1;

public class SubgraphDocument
{
    public NameNode Name { get; }
    public DocumentNode Definition { get; }

    public SubgraphDocument(NameNode name, DocumentNode document)
    {
        Name = name;
        Definition = document;
    }
}
