using System.Collections.Immutable;

#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

/// <summary>
/// Configuration options for the <see cref="RequestDeduplicationHandler"/>.
/// </summary>
public sealed class RequestDeduplicationOptions
{
    /// <summary>
    /// Gets or sets the header names whose values participate in the deduplication hash.
    /// Two requests with the same body and URI but different values for any of these
    /// headers are treated as distinct requests and will not be deduplicated.
    /// Default: <c>["Authorization", "Cookie"]</c>.
    /// </summary>
    public ImmutableArray<string> HashHeaders { get; set; } =
    [
        "Authorization",
        "Cookie"
    ];
}
