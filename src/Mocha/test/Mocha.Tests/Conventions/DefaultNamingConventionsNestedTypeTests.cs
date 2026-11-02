using Mocha.Middlewares;
using Mocha.Tests.Naming;

namespace Mocha.Tests;

public class DefaultNamingConventionsNestedTypeTests
{
    private static readonly HostInfo s_hostWithService = new()
    {
        MachineName = "test-machine",
        ProcessName = "test-process",
        ProcessId = 1,
        AssemblyName = "TestAssembly",
        AssemblyVersion = "1.0.0",
        PackageVersion = "1.0.0",
        FrameworkVersion = ".NET 11.0",
        OperatingSystemVersion = "Linux",
        EnvironmentName = "Test",
        ServiceName = "TestService",
        ServiceVersion = "1.0.0",
        RuntimeInfo = new TestRuntimeInfo(),
        InstanceId = Guid.NewGuid()
    };

    [Theory]
    [InlineData(
        typeof(CreateAccountResponse.UnexpectedError),
        "urn:message:mocha.tests.naming:create-account-response.unexpected-error")]
    [InlineData(
        typeof(DeleteAccountResponse.UnexpectedError),
        "urn:message:mocha.tests.naming:delete-account-response.unexpected-error")]
    [InlineData(
        typeof(CreateAccountResponse.Error.UnexpectedError),
        "urn:message:mocha.tests.naming:create-account-response.error.unexpected-error")]
    [InlineData(
        typeof(DeleteAccountResponse.Error.UnexpectedError),
        "urn:message:mocha.tests.naming:delete-account-response.error.unexpected-error")]
    public void GetMessageIdentity_Should_IncludeDeclaringTypes_When_TypeIsNested(Type type, string expected)
    {
        // arrange
        var sut = new DefaultNamingConventions(s_hostWithService);

        // act
        var result = sut.GetMessageIdentity(type);

        // assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(
        typeof(Envelope<CreateAccountResponse.UnexpectedError>),
        "urn:message:mocha.tests.naming:envelope[create-account-response.unexpected-error]")]
    [InlineData(
        typeof(Envelope<DeleteAccountResponse.Error.UnexpectedError>),
        "urn:message:mocha.tests.naming:envelope[delete-account-response.error.unexpected-error]")]
    [InlineData(
        typeof(Container<string>.Item),
        "urn:message:mocha.tests.naming:container[string].item")]
    [InlineData(
        typeof(Container<>.Item),
        "urn:message:mocha.tests.naming:container[T].item")]
    [InlineData(
        typeof(Envelope<Container<CreateAccountResponse.AccountCreated>.Item>),
        "urn:message:mocha.tests.naming:envelope[container[create-account-response.account-created].item]")]
    public void GetMessageIdentity_Should_IncludeDeclaringTypes_When_GenericInvolvesNestedTypes(
        Type type,
        string expected)
    {
        // arrange
        var sut = new DefaultNamingConventions(s_hostWithService);

        // act
        var result = sut.GetMessageIdentity(type);

        // assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(typeof(AccountClosedEvent), "urn:message:mocha.tests.naming:account-closed-event")]
    [InlineData(typeof(Envelope<AccountClosedEvent>), "urn:message:mocha.tests.naming:envelope[account-closed-event]")]
    [InlineData(typeof(Envelope<>), "urn:message:mocha.tests.naming:envelope[T]")]
    [InlineData(typeof(Pair<,>), "urn:message:mocha.tests.naming:pair[T1,T2]")]
    public void GetMessageIdentity_Should_KeepExistingIdentity_When_TypeIsNotNested(Type type, string expected)
    {
        // arrange
        var sut = new DefaultNamingConventions(s_hostWithService);

        // act
        var result = sut.GetMessageIdentity(type);

        // assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(typeof(CreateAccount.Command), "create-account.command")]
    [InlineData(typeof(DeleteAccount.Command), "delete-account.command")]
    [InlineData(typeof(CreateAccountResponse.Error.UnexpectedError), "create-account-response.error.unexpected-error")]
    [InlineData(typeof(Container<string>.Item), "container.item")]
    public void GetSendEndpointName_Should_IncludeDeclaringTypes_When_TypeIsNested(Type type, string expected)
    {
        // arrange
        var sut = new DefaultNamingConventions(s_hostWithService);

        // act
        var result = sut.GetSendEndpointName(type);

        // assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(typeof(CreateAccount.Command), "mocha.tests.naming.create-account.command")]
    [InlineData(typeof(DeleteAccount.Command), "mocha.tests.naming.delete-account.command")]
    [InlineData(
        typeof(DeleteAccountResponse.Error.UnexpectedError),
        "mocha.tests.naming.delete-account-response.error.unexpected-error")]
    public void GetPublishEndpointName_Should_IncludeDeclaringTypes_When_TypeIsNested(Type type, string expected)
    {
        // arrange
        var sut = new DefaultNamingConventions(s_hostWithService);

        // act
        var result = sut.GetPublishEndpointName(type);

        // assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(typeof(AccountClosedEvent), "account-closed")]
    public void GetSendEndpointName_Should_KeepExistingName_When_TypeIsNotNested(Type type, string expected)
    {
        // arrange
        var sut = new DefaultNamingConventions(s_hostWithService);

        // act
        var result = sut.GetSendEndpointName(type);

        // assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(typeof(AccountClosedEvent), "mocha.tests.naming.account-closed")]
    public void GetPublishEndpointName_Should_KeepExistingName_When_TypeIsNotNested(Type type, string expected)
    {
        // arrange
        var sut = new DefaultNamingConventions(s_hostWithService);

        // act
        var result = sut.GetPublishEndpointName(type);

        // assert
        Assert.Equal(expected, result);
    }

    private sealed class TestRuntimeInfo : IRuntimeInfo
    {
        public string? RuntimeIdentifier => "linux-x64";
        public bool IsServerGC => false;
        public int ProcessorCount => 4;
        public DateTimeOffset? ProcessStartTime => null;
        public bool? IsAotCompiled => false;
        public bool DebuggerAttached => false;
    }
}
