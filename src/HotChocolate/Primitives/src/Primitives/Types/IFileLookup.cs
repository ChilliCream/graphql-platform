using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

/// <summary>
/// Provides a lookup mechanism for uploaded files in a GraphQL multipart request.
/// This interface is used to retrieve files that were uploaded using the
/// GraphQL multipart request specification.
/// </summary>
public interface IFileLookup
{
    /// <summary>
    /// Attempts to retrieve an uploaded file by its multipart request map key.
    /// </summary>
    /// <param name="name">
    /// The map key of the file from the multipart request.
    /// </param>
    /// <param name="file">
    /// When this method returns <c>true</c>, contains the uploaded file;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the file was found; otherwise, <c>false</c>.
    /// </returns>
    bool TryGetFile(string name, [NotNullWhen(true)] out IFile? file);
}
