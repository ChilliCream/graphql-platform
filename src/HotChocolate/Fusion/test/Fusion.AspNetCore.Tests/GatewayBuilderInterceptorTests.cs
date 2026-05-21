using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class GatewayBuilderInterceptorTests : FusionTestBase
{
    private const string SimpleSchema =
        """
        type Query {
          field: String
        }
        """;

    [Fact]
    public async Task AddHttpRequestInterceptor_Generic_Should_Register_Custom_Interceptor()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.AddHttpRequestInterceptor<CustomHttpRequestInterceptor>());

        // act
        var executor = await gateway.Services.GetRequestExecutorAsync();
        var interceptor = executor.Schema.Services.GetRequiredService<IHttpRequestInterceptor>();

        // assert
        Assert.IsType<CustomHttpRequestInterceptor>(interceptor);
    }

    [Fact]
    public async Task AddHttpRequestInterceptor_Factory_Should_Register_Custom_Interceptor()
    {
        // arrange
        var factoryInvoked = false;
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.AddHttpRequestInterceptor(
                _ =>
                {
                    factoryInvoked = true;
                    return new CustomHttpRequestInterceptor();
                }));

        // act
        var executor = await gateway.Services.GetRequestExecutorAsync();
        var interceptor = executor.Schema.Services.GetRequiredService<IHttpRequestInterceptor>();

        // assert
        Assert.IsType<CustomHttpRequestInterceptor>(interceptor);
        Assert.True(factoryInvoked);
    }

    [Fact]
    public async Task AddSocketSessionInterceptor_Generic_Should_Register_Custom_Interceptor()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.AddSocketSessionInterceptor<CustomSocketSessionInterceptor>());

        // act
        var executor = await gateway.Services.GetRequestExecutorAsync();
        var interceptor = executor.Schema.Services.GetRequiredService<ISocketSessionInterceptor>();

        // assert
        Assert.IsType<CustomSocketSessionInterceptor>(interceptor);
    }

    [Fact]
    public async Task AddSocketSessionInterceptor_Factory_Should_Register_Custom_Interceptor()
    {
        // arrange
        var factoryInvoked = false;
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.AddSocketSessionInterceptor(
                _ =>
                {
                    factoryInvoked = true;
                    return new CustomSocketSessionInterceptor();
                }));

        // act
        var executor = await gateway.Services.GetRequestExecutorAsync();
        var interceptor = executor.Schema.Services.GetRequiredService<ISocketSessionInterceptor>();

        // assert
        Assert.IsType<CustomSocketSessionInterceptor>(interceptor);
        Assert.True(factoryInvoked);
    }

    [Fact]
    public async Task AddHttpResponseFormatter_Generic_Should_Register_Custom_Formatter()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.AddHttpResponseFormatter<CustomHttpResponseFormatter>());

        // act
        var executor = await gateway.Services.GetRequestExecutorAsync();
        var formatter = executor.Schema.Services.GetRequiredService<IHttpResponseFormatter>();

        // assert
        Assert.IsType<CustomHttpResponseFormatter>(formatter);
    }

    [Fact]
    public async Task AddHttpResponseFormatter_Factory_Should_Register_Custom_Formatter()
    {
        // arrange
        var factoryInvoked = false;
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.AddHttpResponseFormatter(
                _ =>
                {
                    factoryInvoked = true;
                    return new CustomHttpResponseFormatter();
                }));

        // act
        var executor = await gateway.Services.GetRequestExecutorAsync();
        var formatter = executor.Schema.Services.GetRequiredService<IHttpResponseFormatter>();

        // assert
        Assert.IsType<CustomHttpResponseFormatter>(formatter);
        Assert.True(factoryInvoked);
    }

    [Fact]
    public async Task AddHttpResponseFormatter_Indented_Should_Replace_Default_Formatter()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.AddHttpResponseFormatter(indented: true));

        // act
        var executor = await gateway.Services.GetRequestExecutorAsync();
        var formatter = executor.Schema.Services.GetRequiredService<IHttpResponseFormatter>();

        // assert
        Assert.IsAssignableFrom<DefaultHttpResponseFormatter>(formatter);
    }

    [Fact]
    public async Task AddHttpResponseFormatter_Options_Should_Replace_Default_Formatter()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);
        var options = new HttpResponseFormatterOptions
        {
            HttpTransportVersion = HttpTransportVersion.Latest
        };

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.AddHttpResponseFormatter(options));

        // act
        var executor = await gateway.Services.GetRequestExecutorAsync();
        var formatter = executor.Schema.Services.GetRequiredService<IHttpResponseFormatter>();

        // assert
        Assert.IsAssignableFrom<DefaultHttpResponseFormatter>(formatter);
    }

    private sealed class CustomHttpRequestInterceptor : DefaultHttpRequestInterceptor;

    private sealed class CustomSocketSessionInterceptor : DefaultSocketSessionInterceptor;

    private sealed class CustomHttpResponseFormatter : DefaultHttpResponseFormatter;
}
