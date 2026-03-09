using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Relay;

public class NodeIdSerializerTests
{
    [Fact]
    public void Registered_NodeIdSerializer_Should_Not_Be_Overwritten_By_AddGlobalObjectIdentification()
    {
        // arrange
        var provider = new ServiceCollection()
            .AddGraphQL()
            .AddLegacyNodeIdSerializer()
            .AddGlobalObjectIdentification()
            .Services.BuildServiceProvider();

        // act
        var serializer = provider.GetRequiredService<INodeIdSerializer>();

        // assert
        Assert.IsType<LegacyNodeIdSerializer>(serializer);
    }

    [Fact]
    public void
        Registered_DefaultNodeIdSerializer_With_OutputNewIdFormat_Should_Not_Be_Overwritten_By_AddGlobalObjectIdentification()
    {
        // arrange
        var provider = new ServiceCollection()
            .AddGraphQL()
            .AddDefaultNodeIdSerializer(outputNewIdFormat: false)
            .AddGlobalObjectIdentification()
            .Services.BuildServiceProvider();

        // act
        var serializer = provider.GetRequiredService<INodeIdSerializer>();
        var serializedId = serializer.Format("Foo", 32);

        // assert
        Assert.Equal("Rm9vCmkzMg==", serializedId);
    }
}
