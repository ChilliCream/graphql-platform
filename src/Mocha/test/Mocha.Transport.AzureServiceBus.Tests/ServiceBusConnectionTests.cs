using Azure.Core;
using Azure.Messaging.ServiceBus;
using Mocha.Transport.AzureServiceBus;

namespace Mocha.Transport.AzureServiceBus.Tests;

public sealed class ServiceBusConnectionTests
{
    [Fact]
    public void ResolveAdministrationConnectionString_Should_FallBackToConnectionString_When_NotConfigured()
    {
        // arrange
        const string connectionString =
            "Endpoint=sb://127.0.0.1:5672/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true";
        var configuration = new AzureServiceBusTransportConfiguration
        {
            ConnectionString = connectionString
        };

        // act
        var result = ServiceBusConnection.ResolveAdministrationConnectionString(configuration);

        // assert
        Assert.Same(connectionString, result);
    }

    [Fact]
    public void ResolveAdministrationConnectionString_Should_ReturnAdministrationConnectionString_When_Configured()
    {
        // arrange
        const string connectionString =
            "Endpoint=sb://127.0.0.1:5672/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true";
        const string administrationConnectionString =
            "Endpoint=sb://127.0.0.1:5300/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true";
        var configuration = new AzureServiceBusTransportConfiguration
        {
            ConnectionString = connectionString,
            AdministrationConnectionString = administrationConnectionString
        };

        // act
        var result = ServiceBusConnection.ResolveAdministrationConnectionString(configuration);

        // assert
        Assert.Same(administrationConnectionString, result);
    }

    [Fact]
    public void Create_Should_Throw_When_AdministrationConnectionStringConfiguredWithCredential()
    {
        // arrange
        var configuration = new AzureServiceBusTransportConfiguration
        {
            FullyQualifiedNamespace = "localhost",
            Credential = new TestTokenCredential(),
            AdministrationConnectionString =
                "Endpoint=sb://127.0.0.1:5300/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true"
        };

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => ServiceBusConnection.Create(configuration, new ServiceBusClientOptions()));

        // assert
        Assert.Contains("AdministrationConnectionString", exception.Message);
        Assert.Contains("ConnectionString", exception.Message);
    }

    private sealed class TestTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return new AccessToken("token", DateTimeOffset.MaxValue);
        }

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(GetToken(requestContext, cancellationToken));
        }
    }
}
