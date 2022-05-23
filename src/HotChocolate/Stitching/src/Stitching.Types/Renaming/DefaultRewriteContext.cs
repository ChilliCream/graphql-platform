using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Contracts;

namespace HotChocolate.Stitching.Types.Renaming;

public class DefaultRewriteContext : IRewriteContext
{
    public DefaultRewriteContext(DocumentNode document)
    {
        Document = document;
        Navigator = new DefaultSyntaxNavigator();
        Errors = new List<IError>();
        ContextData = new Dictionary<string, object?>();
    }


    public DocumentNode Document { get; set; }
    public ISyntaxNavigator Navigator { get; }
    public ICollection<IError> Errors { get; }
    public IDictionary<string, object?> ContextData { get; }
}
