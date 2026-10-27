using HotChocolate.Execution;
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

    [Fact]
    public async Task Execute_Should_ResolveGuidNode_When_IdIsInNewFormatAndOutputNewIdFormatIsFalse()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDefaultNodeIdSerializer(outputNewIdFormat: false)
            .AddGlobalObjectIdentification()
            .AddQueryType<GuidNodeQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              node(id: "Q2hlY2tvdXQ6W62PD8vZn0ahZXCGdyiVDg==") {
                id
              }
              echoCheckoutId(id: "Q2hlY2tvdXQ6W62PD8vZn0ahZXCGdyiVDg==")
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "id": "Q2hlY2tvdXQKZzBmOGZhZDViZDljYjQ2OWZhMTY1NzA4Njc3Mjg5NTBl"
                },
                "echoCheckoutId": "0f8fad5b-d9cb-469f-a165-70867728950e"
              }
            }
            """);
    }

    public class GuidNodeQuery
    {
        [NodeResolver]
        public Checkout GetCheckoutById(Guid id) => new(id);

        public Guid EchoCheckoutId([ID(nameof(Checkout))] Guid id) => id;
    }

    public sealed class Checkout(Guid id)
    {
        public Guid Id { get; } = id;
    }
}
