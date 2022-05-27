using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline;

/// <summary>
/// The schema merge context represents the context on which the schema merge pipeline operates.
/// </summary>
public interface ISchemaMergeContext
{
    /// <summary>
    /// Gets the initial configurations that are passed into the schema merge pipeline.
    /// </summary>
    IReadOnlyList<ServiceConfiguration> Configurations { get; }

    /// <summary>
    /// Gets or sets the schema documents that are being transformed by the schema merge pipeline.
    /// </summary>
    IImmutableList<DocumentNode> Documents { get; set; }

    /// <summary>
    /// Gets the errors that occured while merging multiple schemas.
    /// </summary>
    ICollection<IError> Errors { get; }

    /// <summary>
    /// Gets custom context data that can be used to pass temporary context between two or more
    /// middleware.
    /// </summary>
    IDictionary<string, object?> ContextData { get; }
}
