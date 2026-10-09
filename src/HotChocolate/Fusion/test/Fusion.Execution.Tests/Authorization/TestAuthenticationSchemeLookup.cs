using System.Collections.Immutable;

namespace HotChocolate.Fusion.Authorization;

internal sealed class TestAuthenticationSchemeLookup(params string[] schemeNames) : IAuthenticationSchemeLookup
{
    private readonly ImmutableArray<string> _schemeNames = [.. schemeNames];

    public ImmutableDictionary<string, string> Challenges { get; init; } =
#if NET10_0_OR_GREATER
        [];
#else
        ImmutableDictionary<string, string>.Empty;
#endif

    public ValueTask<ImmutableArray<string>> GetSchemeNamesAsync(CancellationToken cancellationToken)
        => new(_schemeNames);

    public ValueTask<string?> GetChallengeAsync(string schemeName, CancellationToken cancellationToken)
        => new(Challenges.GetValueOrDefault(schemeName));
}
