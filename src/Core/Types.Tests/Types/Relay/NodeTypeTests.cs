using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using Moq;
using Xunit;

namespace HotChocolate.Types
{
    public class NodeTypeTests
    {
        [Fact]
        public void IntializeExplicitFieldWithImplicitResolver()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

            // act
            var nodeInterface = new NodeType();

            // assert
            INeedsInitialization init = nodeInterface;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), nodeInterface, false);
            schemaContext.Types.RegisterType(nodeInterface);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);

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
