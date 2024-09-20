using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.Execution.DependencyInjection;

public class RequestExecutorBuilderExtensionsTypeDiscoveryTests
{
    [Fact]
    public void AddTypeDiscoveryHandler_1_Builder_Is_Null()
    {
        void Fail() => RequestExecutorBuilderExtensions
            .AddTypeDiscoveryHandler(null!, _ => new MockHandler());

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddTypeDiscoveryHandler_1_Factory_Is_Null()
    {
        var mock = new Mock<IRequestExecutorBuilder>();

        void Fail() => mock.Object.AddTypeDiscoveryHandler<MockHandler>(null!);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    public sealed class MockHandler : TypeDiscoveryHandler
    {
        public override bool TryInferType(
            TypeReference typeReference,
            TypeDiscoveryInfo typeReferenceInfo,
            [NotNullWhen(true)] out TypeReference[]? schemaTypeRefs)
        {
            schemaTypeRefs = null;
            return false;
        }
    }
}
