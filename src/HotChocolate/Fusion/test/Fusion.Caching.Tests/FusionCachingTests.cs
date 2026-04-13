using System.Text;
using HotChocolate.Caching;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using Microsoft.Extensions.DependencyInjection;
using CacheControlAttribute = HotChocolate.Caching.CacheControlAttribute;

namespace HotChocolate.Fusion.Caching;

public class FusionCachingTests : FusionTestBase
{
    [Fact]
    public async Task Query_With_CacheControl_MaxAge_Should_Return_CacheControl_Header()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ expensiveField }");

        // assert
        Assert.NotNull(response.Headers.CacheControl);
        Assert.Equal(TimeSpan.FromSeconds(3600), response.Headers.CacheControl!.MaxAge);
    }

    [Fact]
    public async Task Query_With_Multiple_Fields_Should_Return_Shortest_MaxAge()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ expensiveField cheaperField }");

        // assert
        Assert.NotNull(response.Headers.CacheControl);
        Assert.Equal(TimeSpan.FromSeconds(1800), response.Headers.CacheControl!.MaxAge);
    }

    [Fact]
    public async Task Query_With_Private_Scope_Should_Return_Private_Header()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<PrivateCachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ userData }");

        // assert
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl!.Private);
        Assert.Equal(TimeSpan.FromSeconds(3600), response.Headers.CacheControl.MaxAge);
    }

    [Fact]
    public async Task Query_With_SharedMaxAge_Should_Return_SMaxAge_Header()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SharedCachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ sharedData }");

        // assert
        Assert.NotNull(response.Headers.CacheControl);
        Assert.Equal(TimeSpan.FromSeconds(1800), response.Headers.CacheControl!.SharedMaxAge);
    }

    [Fact]
    public async Task Mutation_Should_Not_Return_CacheControl_Header()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<MutationSchema.Query>()
                .AddMutationType<MutationSchema.Mutation>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("mutation { doSomething }");

        // assert
        Assert.Null(response.Headers.CacheControl);
    }

    [Fact]
    public async Task Query_With_No_CacheControl_Directive_Should_Not_Cache()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<NoCacheSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ plainField }");

        // assert
        Assert.Null(response.Headers.CacheControl);
    }

    [Fact]
    public async Task Query_With_Error_Should_Not_Cache()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<ErrorSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ errorField }");

        // assert
        Assert.Null(response.Headers.CacheControl);
    }

    [Fact]
    public async Task Interceptor_Should_Compute_Constraints_From_Composite_Schema()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        var verifyingInterceptor = new VerifyingInterceptor();

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .AddOperationPlannerInterceptor(_ => verifyingInterceptor)
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ expensiveField }");

        // assert
        Assert.True(verifyingInterceptor.HasHitOnAfterPlanCompleted);
        Assert.True(verifyingInterceptor.HasCacheConstraints);
        Assert.True(verifyingInterceptor.HasCacheControlHeaderValue);
    }

    private sealed class VerifyingInterceptor : IOperationPlannerInterceptor
    {
        public bool HasHitOnAfterPlanCompleted;
        public bool HasCacheConstraints;
        public bool HasCacheControlHeaderValue;

        public void OnAfterPlanCompleted(
            OperationDocumentInfo operationDocumentInfo,
            OperationPlan operationPlan)
        {
            HasHitOnAfterPlanCompleted = true;
            HasCacheConstraints = operationPlan.Operation.Features
                .TryGet<ImmutableCacheConstraints>(out _);
            HasCacheControlHeaderValue = operationPlan.Operation.Features
                .TryGet<Microsoft.Net.Http.Headers.CacheControlHeaderValue>(out _);
        }
    }
}

public static class CachedSchema
{
    public class Query
    {
        [CacheControl(3600)]
        public string ExpensiveField() => "expensive";

        [CacheControl(1800)]
        public string CheaperField() => "cheaper";
    }
}

public static class PrivateCachedSchema
{
    public class Query
    {
        [CacheControl(3600, Scope = CacheControlScope.Private)]
        public string UserData() => "private-data";
    }
}

public static class SharedCachedSchema
{
    public class Query
    {
        [CacheControl(SharedMaxAge = 1800)]
        public string SharedData() => "shared";
    }
}

public static class NoCacheSchema
{
    public class Query
    {
        public string PlainField() => "plain";
    }
}

public static class ErrorSchema
{
    public class Query
    {
        [CacheControl(3600)]
        public string ErrorField() => throw new Exception("Boom!");
    }
}

public static class MutationSchema
{
    public class Query
    {
        public string Hello() => "world";
    }

    public class Mutation
    {
        public string DoSomething() => "done";
    }
}

internal static class TestHttpClientExtensions
{
    public static async Task<HttpResponseMessage> PostGraphQLAsync(
        this HttpClient client, string query)
    {
        var payload = $$"""{"query":"{{query}}"}""";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        return await client.PostAsync("/graphql", content);
    }
}
