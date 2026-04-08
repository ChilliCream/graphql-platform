using Mocha.Middlewares;

namespace Mocha.Tests;

public class DefaultNamingConventionsTests
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

    private static readonly HostInfo s_hostWithoutService = new()
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
        ServiceName = null,
        ServiceVersion = null,
        RuntimeInfo = new TestRuntimeInfo(),
        InstanceId = Guid.NewGuid()
    };

    [Fact]
    public void GetReceiveEndpointName_Type_Should_ReturnKebabCaseName_When_HandlerSuffix()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetReceiveEndpointName(typeof(OrderCreatedHandler), ReceiveEndpointKind.Default);

        Assert.Equal("order-created", result);
    }

    [Fact]
    public void GetReceiveEndpointName_Type_Should_StripConsumerSuffix_When_ConsumerSuffix()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetReceiveEndpointName(typeof(PaymentProcessedConsumer), ReceiveEndpointKind.Default);

        Assert.Equal("payment-processed", result);
    }

    [Fact]
    public void GetReceiveEndpointName_Type_Should_AppendErrorSuffix_When_ErrorKind()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetReceiveEndpointName(typeof(OrderCreatedHandler), ReceiveEndpointKind.Error);

        Assert.Equal("order-created_error", result);
    }

    [Fact]
    public void GetReceiveEndpointName_Type_Should_AppendSkippedSuffix_When_SkippedKind()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetReceiveEndpointName(typeof(OrderCreatedHandler), ReceiveEndpointKind.Skipped);

        Assert.Equal("order-created_skipped", result);
    }

    [Fact]
    public void GetReceiveEndpointName_Type_Should_AppendReplySuffix_When_ReplyKind()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetReceiveEndpointName(typeof(OrderCreatedHandler), ReceiveEndpointKind.Reply);

        Assert.Equal("order-created_reply", result);
    }

    [Fact]
    public void GetReceiveEndpointName_Type_Should_StripGenericArity_When_GenericHandler()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetReceiveEndpointName(typeof(MyHandler<>), ReceiveEndpointKind.Default);

        Assert.Equal("my", result);
    }

    [Fact]
    public void GetReceiveEndpointName_Type_Should_Throw_When_NullType()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        Assert.Throws<ArgumentNullException>(() =>
            sut.GetReceiveEndpointName((Type)null!, ReceiveEndpointKind.Default)
        );
    }

    [Fact]
    public void GetReceiveEndpointName_String_Should_ReturnKebabCase_When_PascalCaseName()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetReceiveEndpointName("OrderProcessing", ReceiveEndpointKind.Default);

        Assert.Equal("order-processing", result);
    }

    [Fact]
    public void GetReceiveEndpointName_String_Should_AppendErrorSuffix_When_ErrorKind()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetReceiveEndpointName("OrderProcessing", ReceiveEndpointKind.Error);

        Assert.Equal("order-processing_error", result);
    }

    [Fact]
    public void GetReceiveEndpointName_String_Should_StripConsumerSuffix_When_ConsumerSuffix()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetReceiveEndpointName("MyCustomConsumer", ReceiveEndpointKind.Default);

        Assert.Equal("my-custom", result);
    }

    [Fact]
    public void GetReceiveEndpointName_String_Should_Throw_When_EmptyString()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        Assert.Throws<ArgumentException>(() => sut.GetReceiveEndpointName("", ReceiveEndpointKind.Default));
    }

    [Fact]
    public void GetReceiveEndpointName_String_Should_Throw_When_Whitespace()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        Assert.Throws<ArgumentException>(() => sut.GetReceiveEndpointName("   ", ReceiveEndpointKind.Default));
    }

    [Fact]
    public void GetReceiveEndpointName_String_Should_TrimWhitespace_When_PaddedName()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetReceiveEndpointName("  OrderProcessing  ", ReceiveEndpointKind.Default);

        Assert.Equal("order-processing", result);
    }

    [Fact]
    public void GetSagaName_Should_StripHandlerSuffixAndKebabCase_When_HandlerSuffix()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetSagaName(typeof(OrderCreatedHandler));

        Assert.Equal("order-created", result);
    }

    [Fact]
    public void GetSagaName_Should_StripConsumerSuffixAndKebabCase_When_ConsumerSuffix()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetSagaName(typeof(PaymentProcessedConsumer));

        Assert.Equal("payment-processed", result);
    }

    [Fact]
    public void GetSagaName_Should_ReturnKebabCase_When_NoKnownSuffix()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetSagaName(typeof(OrderWorkflow));

        Assert.Equal("order-workflow", result);
    }

    [Fact]
    public void GetInstanceEndpoint_Should_ReturnFormattedGuid_When_ValidGuid()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");

        var result = sut.GetInstanceEndpoint(guid);

        Assert.Equal($"response-{guid:N}", result);
    }

    [Fact]
    public void GetInstanceEndpoint_Should_Throw_When_EmptyGuid()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        Assert.Throws<ArgumentException>(() => sut.GetInstanceEndpoint(Guid.Empty));
    }

    [Fact]
    public void GetSendEndpointName_Should_StripCommandSuffix_When_CommandType()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetSendEndpointName(typeof(CreateOrderCommand));

        Assert.Equal("create-order", result);
    }

    [Fact]
    public void GetSendEndpointName_Should_StripMessageSuffix_When_MessageType()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetSendEndpointName(typeof(ProcessPaymentMessage));

        Assert.Equal("process-payment", result);
    }

    [Fact]
    public void GetSendEndpointName_Should_StripEventSuffix_When_EventType()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetSendEndpointName(typeof(OrderCreatedEvent));

        Assert.Equal("order-created", result);
    }

    [Fact]
    public void GetSendEndpointName_Should_Throw_When_NullType()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        Assert.Throws<ArgumentNullException>(() => sut.GetSendEndpointName(null!));
    }

    [Fact]
    public void GetPublishEndpointName_Should_ReturnNamespaceDotName_When_ValidType()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetPublishEndpointName(typeof(CreateOrderCommand));

        Assert.Equal("mocha.tests.create-order", result);
    }

    [Fact]
    public void GetPublishEndpointName_Should_StripMessageSuffix_When_MessageType()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetPublishEndpointName(typeof(ProcessPaymentMessage));

        Assert.EndsWith(".process-payment", result);
    }

    [Fact]
    public void GetPublishEndpointName_Should_Throw_When_NullType()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        Assert.Throws<ArgumentNullException>(() => sut.GetPublishEndpointName(null!));
    }

    [Fact]
    public void GetMessageIdentity_Should_ReturnUrnFormat_When_NonGenericType()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetMessageIdentity(typeof(CreateOrderCommand));

        Assert.StartsWith("urn:message:", result);
        Assert.Contains("create-order-command", result);
    }

    [Fact]
    public void GetMessageIdentity_Should_IncludeNamespace_When_NonGenericType()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetMessageIdentity(typeof(CreateOrderCommand));

        Assert.Contains("mocha", result);
    }

    [Fact]
    public void GetMessageIdentity_Should_IncludeGenericNotation_When_OpenGenericWithOneArg()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetMessageIdentity(typeof(GenericMessage<>));

        Assert.Contains("[T]", result);
    }

    [Fact]
    public void GetMessageIdentity_Should_IncludeGenericNotation_When_OpenGenericWithTwoArgs()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetMessageIdentity(typeof(GenericMessage<,>));

        Assert.Contains("[T1,T2]", result);
    }

    [Fact]
    public void GetMessageIdentity_Should_IncludeClosedGenericArgs_When_ClosedGeneric()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        var result = sut.GetMessageIdentity(typeof(GenericMessage<string>));

        Assert.Contains("generic-message[string]", result);
    }

    [Fact]
    public void GetMessageIdentity_Should_Throw_When_NullType()
    {
        var sut = new DefaultNamingConventions(s_hostWithService);

        Assert.Throws<NullReferenceException>(() => sut.GetMessageIdentity(null!));
    }

    private sealed class OrderCreatedHandler;

    private sealed class PaymentProcessedConsumer;

    private sealed class MyHandler<T>;

    private sealed class OrderWorkflow;

    private sealed class CreateOrderCommand;

    private sealed class ProcessPaymentMessage;

    private sealed class OrderCreatedEvent;

    private sealed class GenericMessage<T>;

    private sealed class GenericMessage<T1, T2>;

    // =========================================================================
    // Test Infrastructure
    // =========================================================================

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
