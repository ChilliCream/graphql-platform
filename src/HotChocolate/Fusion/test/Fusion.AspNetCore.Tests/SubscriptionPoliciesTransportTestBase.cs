using System.Globalization;
using System.Security.Claims;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Policies;
using HotChocolate.Fusion.Policies.Audit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HotChocolate.Fusion;

/// <summary>
/// Builds a gateway in front of the <see cref="SubscriptionPoliciesSource"/> whose principal
/// comes from the test headers.
/// </summary>
public abstract class SubscriptionPoliciesTransportTestBase : FusionTestBase
{
    public const string UserHeader = "X-Test-User";
    public const string ExpiryHeader = "X-Test-Expiry";
    protected const string Member = "member";
    protected const string Reader = "reader";

    protected const string ChangedSubscription = "subscription { changed { id name tag } }";

    protected static readonly SubscriptionPoliciesSource.Item Event =
        new("1", "name", "code", "note", "tag");

    protected static void DenyLivePolicy(IFusionGatewayBuilder builder)
        => builder.AddInMemoryPolicies(policies => policies.Deny(SubscriptionPoliciesSource.LivePolicy));

    protected Task<SubscriptionGateway> CreateSubscriptionGatewayAsync(DenyHandling denyHandling)
        => CreateSubscriptionGatewayAsync(denyHandling, _ => { });

    protected async Task<SubscriptionGateway> CreateSubscriptionGatewayAsync(
        DenyHandling denyHandling,
        Action<IFusionGatewayBuilder> configure)
    {
        var feed = new SubscriptionPoliciesFeed();
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch.AddHours(1));
        var audit = new RecordingAuditProvider();

        var source = CreateSourceSchema(
            "A",
            SubscriptionPoliciesSource.Configure,
            configureServices: services => services.AddSingleton(feed));

        var gateway = await CreateCompositeSchemaAsync(
            [("A", source)],
            configureServices: services => services.AddAuthentication().AddJwtBearer("Bearer", _ => { }),
            configureApplication: UseTestUser,
            configureGatewayBuilder: builder =>
            {
                builder.ConfigureSchemaServices(
                    (_, services) =>
                    {
                        services.AddSingleton<TimeProvider>(time);
                        services.AddSingleton<IAuditProvider>(audit);
                    });
                builder.ModifyAuthorizationOptions(o => o.DenyHandling = denyHandling);

                // The policies the test configures take precedence over the allowing defaults below.
                configure(builder);
                builder.AddInMemoryPolicies(
                    policies => policies
                        .Allow(SubscriptionPoliciesSource.LivePolicy)
                        .Allow(SubscriptionPoliciesSource.FlipPolicy));
            },
            includeOperationPlan: false);

        return new SubscriptionGateway(gateway, feed, time, audit);
    }

    /// <summary>
    /// Starts a subscription over server-sent events and returns once the gateway opened the
    /// source schema subscription.
    /// </summary>
    protected static async Task<SubscriptionSseClient> StartSseAsync(
        SubscriptionGateway gateway,
        HttpClient client,
        string query,
        string? user,
        string? expiry)
    {
        var expectedOpened = gateway.Feed.Opened + 1;
        var pending = SubscriptionSseClient.StartAsync(client, query, user, expiry);
        await gateway.Feed.WaitForOpenedAsync(expectedOpened);

        return await pending;
    }

    protected static string FormatExpiry(DateTimeOffset expiry)
        => expiry.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

    private static void UseTestUser(IApplicationBuilder app)
    {
        app.UseWebSockets();
        app.UseRouting();
        app.Use(
            async (context, next) =>
            {
                if (context.Request.Headers.TryGetValue(UserHeader, out var user))
                {
                    var claims = new List<Claim>();

                    if (user == Reader)
                    {
                        claims.Add(new Claim("scope", "read"));
                    }

                    if (context.Request.Headers.TryGetValue(ExpiryHeader, out var expiry))
                    {
                        claims.Add(new Claim("exp", expiry.ToString()));
                    }

                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
                }

                await next(context);
            });
        app.UseEndpoints(endpoint => endpoint.MapGraphQL());
    }

    protected sealed class SubscriptionGateway(
        Gateway gateway,
        SubscriptionPoliciesFeed feed,
        FakeTimeProvider time,
        RecordingAuditProvider audit)
        : IDisposable
    {
        public Gateway Gateway { get; } = gateway;

        public SubscriptionPoliciesFeed Feed { get; } = feed;

        public FakeTimeProvider Time { get; } = time;

        public RecordingAuditProvider Audit { get; } = audit;

        public void Dispose() => Gateway.Dispose();
    }
}
