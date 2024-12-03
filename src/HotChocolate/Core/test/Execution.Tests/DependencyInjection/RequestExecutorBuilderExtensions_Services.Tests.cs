using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.Execution.DependencyInjection;

public class RequestExecutorBuilderExtensionsServicesTests
{
    [Fact]
    public void AddScopedServiceInitializer_1_Builder_Is_Null()
    {
        void Fail() => RequestExecutorBuilderExtensions
            .AddScopedServiceInitializer<string>(null!, (_, _) => { });

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddScopedServiceInitializer_1_Initializer_Is_Null()
    {
        var mock = new Mock<IRequestExecutorBuilder>();

        void Fail() => RequestExecutorBuilderExtensions
            .AddScopedServiceInitializer<string>(mock.Object, null!);

        Assert.Throws<ArgumentNullException>(Fail);
    }
}
