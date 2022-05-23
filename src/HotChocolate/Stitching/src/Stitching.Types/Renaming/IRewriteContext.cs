using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Contracts;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Renaming;

public interface IRewriteContext : ISyntaxVisitorContext, INavigatorContext
{
    /// <summary>
    /// Gets or sets the schema document that is being transformed by the schema rewrite pipeline.
    /// </summary>
    DocumentNode Document { get; set; }

    /// <summary>
    /// Gets the errors that occurred while merging multiple schemas.
    /// </summary>
    ICollection<IError> Errors { get; }

    /// <summary>
    /// Gets custom context data that can be used to pass temporary context between two or more
    /// middleware.
    /// </summary>
    IDictionary<string, object?> ContextData { get; }
}
