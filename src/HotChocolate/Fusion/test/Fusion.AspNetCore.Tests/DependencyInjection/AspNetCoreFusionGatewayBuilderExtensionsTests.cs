using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.DependencyInjection;

public sealed class AspNetCoreFusionGatewayBuilderExtensionsTests
{
    [Fact]
    public void AddHttpResponseFormatter_Should_Throw_When_TransportVersionIsUnrecognized()
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQLGatewayServer();
        var options = new HttpResponseFormatterOptions
        {
            HttpTransportVersion = (HttpTransportVersion)99
        };

        // act
        void Act() => builder.AddHttpResponseFormatter(options);

        // assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(Act);
        Assert.Equal("options", exception.ParamName);
        Assert.Equal((HttpTransportVersion)99, exception.ActualValue);
    }
}
