using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Testing;

/// <summary>
/// Testing extensions for <see cref="OperationRequestBuilder"/>.
/// </summary>
public static class OperationRequestBuilderExtensions
{
    /// <summary>
    /// Registers a set of uploaded files with the operation request so that
    /// <c>Upload</c> variables can be resolved through an <see cref="IFileLookup"/> feature.
    /// </summary>
    /// <param name="builder">
    /// The operation request builder.
    /// </param>
    /// <param name="fileMap">
    /// A map of multipart request keys to <see cref="IFile"/> instances.
    /// </param>
    /// <returns>
    /// The same <paramref name="builder"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="fileMap"/> is <c>null</c>.
    /// </exception>
    public static OperationRequestBuilder SetFiles(
        this OperationRequestBuilder builder,
        Dictionary<string, IFile> fileMap)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(fileMap);

        builder.Features.Set<IFileLookup>(new FormFileLookup(fileMap));
        return builder;
    }
}
