using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// Implement this interface to provide access to a fusion schema document storage.
/// </summary>
public interface IFusionSchemaDocumentProvider
    : IObservable<DocumentNode>
    , IAsyncDisposable
{
    /// <summary>
    /// Gets the latest available schema document.
    /// </summary>
    DocumentNode? SchemaDocument { get; }
}
