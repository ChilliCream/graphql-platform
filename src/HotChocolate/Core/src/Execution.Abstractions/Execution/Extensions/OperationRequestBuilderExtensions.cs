using System.Security.Claims;
using HotChocolate.Features;
using HotChocolate.Types;

namespace HotChocolate.Execution;

/// <summary>
/// Extensions methods for <see cref="OperationRequestBuilder"/>.
/// </summary>
public static class OperationRequestBuilderExtensions
{
    /// <summary>
    /// Sets the user for this request.
    /// </summary>
    public static OperationRequestBuilder SetUser(
        this OperationRequestBuilder builder,
        ClaimsPrincipal claimsPrincipal)
        => builder.SetGlobalState(nameof(ClaimsPrincipal), claimsPrincipal);

    /// <summary>
    /// Marks this request as a warmup request that will bypass security measures and skip execution.
    /// </summary>
    public static OperationRequestBuilder MarkAsWarmupRequest(
        this OperationRequestBuilder builder)
        => builder.SetGlobalState(ExecutionContextData.IsWarmupRequest, true);

    /// <summary>
    /// Registers an uploaded file with the operation request so that an <c>Upload</c> variable
    /// referencing <paramref name="name"/> can be resolved through the
    /// <see cref="IFileLookup"/> feature.
    /// </summary>
    /// <param name="builder">
    /// The operation request builder.
    /// </param>
    /// <param name="name">
    /// The multipart request map key that identifies the file.
    /// </param>
    /// <param name="file">
    /// The file to associate with <paramref name="name"/>.
    /// </param>
    /// <returns>
    /// The same <paramref name="builder"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="file"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or empty.
    /// </exception>
    public static OperationRequestBuilder AddFile(
        this OperationRequestBuilder builder,
        string name,
        IFile file)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(file);

        var lookup = builder.Features.GetOrSet<IFileLookup>(
            static () => new OperationRequestFileLookup());

        if (lookup is not OperationRequestFileLookup mutable)
        {
            throw new InvalidOperationException(
                "An IFileLookup feature has already been set on this request "
                + "and cannot be extended.");
        }

        mutable.Add(name, file);
        return builder;
    }
}
