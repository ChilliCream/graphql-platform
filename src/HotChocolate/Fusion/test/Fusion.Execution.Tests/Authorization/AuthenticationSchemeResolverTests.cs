using System.Collections.Immutable;

namespace HotChocolate.Fusion.Authorization;

public class AuthenticationSchemeResolverTests
{
    [Fact]
    public async Task GetChallengeAsync_Should_AdvertiseProtocolChallenge_When_HandlerFixesOne()
    {
        // arrange
        var schemeResolver = new AuthenticationSchemeResolver(
            new FusionAuthorizationOptions(),
            new TestAuthenticationSchemeLookup("MyJwt")
            {
                Challenges = ImmutableDictionary<string, string>.Empty.Add("MyJwt", "Bearer")
            });

        // act
        var challenge = await schemeResolver.GetChallengeAsync(CancellationToken.None);

        // assert
        Assert.Equal("Bearer", challenge);
    }

    [Fact]
    public async Task GetChallengeAsync_Should_AdvertiseChallengeOnce_When_RegistrationsShareIt()
    {
        // arrange
        var schemeResolver = new AuthenticationSchemeResolver(
            new FusionAuthorizationOptions(),
            new TestAuthenticationSchemeLookup("JwtA", "JwtB")
            {
                Challenges = ImmutableDictionary<string, string>.Empty.Add("JwtA", "Bearer").Add("JwtB", "Bearer")
            });

        // act
        var challenge = await schemeResolver.GetChallengeAsync(CancellationToken.None);

        // assert
        Assert.Equal("Bearer", challenge);
    }

    [Fact]
    public async Task GetChallengeAsync_Should_AdvertiseChallengesInOrdinalOrder_When_RegistrationsDiffer()
    {
        // arrange
        var schemeResolver = new AuthenticationSchemeResolver(
            new FusionAuthorizationOptions(),
            new TestAuthenticationSchemeLookup("Windows", "MyJwt")
            {
                Challenges = ImmutableDictionary<string, string>.Empty
                    .Add("Windows", "Negotiate")
                    .Add("MyJwt", "Bearer")
            });

        // act
        var challenge = await schemeResolver.GetChallengeAsync(CancellationToken.None);

        // assert
        Assert.Equal("Bearer, Negotiate", challenge);
    }

    [Fact]
    public async Task GetChallengeAsync_Should_AdvertiseMappedChallenge_When_HandlerFixesNone()
    {
        // arrange
        var options = new FusionAuthorizationOptions
        {
            SchemeChallenges = ImmutableDictionary<string, string>.Empty.Add("Cookies", "Cookie")
        };
        var schemeResolver = new AuthenticationSchemeResolver(options, new TestAuthenticationSchemeLookup("Cookies"));

        // act
        var challenge = await schemeResolver.GetChallengeAsync(CancellationToken.None);

        // assert
        Assert.Equal("Cookie", challenge);
    }

    [Fact]
    public async Task GetChallengeAsync_Should_ReturnNull_When_HandlerFixesNoneAndNoEntryIsMapped()
    {
        // arrange
        var schemeResolver = new AuthenticationSchemeResolver(
            new FusionAuthorizationOptions(),
            new TestAuthenticationSchemeLookup("Cookies"));

        // act
        var challenge = await schemeResolver.GetChallengeAsync(CancellationToken.None);

        // assert
        Assert.Null(challenge);
    }

    [Fact]
    public async Task GetChallengeAsync_Should_AdvertiseOnlyListedSchemes_When_SchemesAreSet()
    {
        // arrange
        var options = new FusionAuthorizationOptions
        {
            Schemes = ImmutableArray.Create("Windows"),
            SchemeChallenges = ImmutableDictionary<string, string>.Empty.Add("Windows", "Negotiate")
        };
        var schemeResolver = new AuthenticationSchemeResolver(
            options,
            new TestAuthenticationSchemeLookup("MyJwt", "Windows")
            {
                Challenges = ImmutableDictionary<string, string>.Empty.Add("MyJwt", "Bearer")
            });

        // act
        var challenge = await schemeResolver.GetChallengeAsync(CancellationToken.None);

        // assert
        Assert.Equal("Negotiate", challenge);
    }

    [Fact]
    public async Task GetChallengeAsync_Should_SkipUnregisteredSchemes_When_ListedSchemeIsNotRegistered()
    {
        // arrange
        var options = new FusionAuthorizationOptions
        {
            Schemes = ImmutableArray.Create("Windows", "MyJwt"),
            SchemeChallenges = ImmutableDictionary<string, string>.Empty.Add("Windows", "Negotiate")
        };
        var schemeResolver = new AuthenticationSchemeResolver(
            options,
            new TestAuthenticationSchemeLookup("MyJwt")
            {
                Challenges = ImmutableDictionary<string, string>.Empty.Add("MyJwt", "Bearer")
            });

        // act
        var challenge = await schemeResolver.GetChallengeAsync(CancellationToken.None);

        // assert
        Assert.Equal("Bearer", challenge);
    }

    [Fact]
    public async Task GetChallengeAsync_Should_ReturnNull_When_NoSchemeIsRegistered()
    {
        // arrange
        var schemeResolver = new AuthenticationSchemeResolver(
            new FusionAuthorizationOptions { Schemes = ImmutableArray.Create("Bearer") },
            new TestAuthenticationSchemeLookup());

        // act
        var challenge = await schemeResolver.GetChallengeAsync(CancellationToken.None);

        // assert
        Assert.Null(challenge);
    }

    [Fact]
    public async Task GetChallengeAsync_Should_ReturnNull_When_HostHasNoLookup()
    {
        // arrange
        var schemeResolver = new AuthenticationSchemeResolver(new FusionAuthorizationOptions(), lookup: null);

        // act
        var challenge = await schemeResolver.GetChallengeAsync(CancellationToken.None);

        // assert
        Assert.Null(challenge);
    }
}
