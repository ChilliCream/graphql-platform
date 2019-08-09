using HotChocolate.Types.Relay;
using Xunit;

namespace HotChocolate.Types
{
    public class NodeTypeTests
        : TypeTestBase
    {
        [Fact]
        public void IntializeExplicitFieldWithImplicitResolver()
        {
            // arrange
            // act
            NodeType nodeInterface = CreateType(
                new NodeType(),
                b => b.ModifyOptions(o => o.StrictValidation = false));

            // assert
            Assert.Equal(
                "Node",
                nodeInterface.Name);

            Assert.Equal(
                "The node interface is implemented by entities that have " +
                "a gloabl unique identifier.",
                nodeInterface.Description);

            Assert.Collection(nodeInterface.Fields,
                t =>
                {
                    Assert.Equal("id", t.Name);
                    Assert.IsType<IdType>(
                        Assert.IsType<NonNullType>(t.Type).Type);
                });
        }
    }
}
