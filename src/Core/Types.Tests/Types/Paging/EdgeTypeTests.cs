using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using Xunit;

namespace HotChocolate.Types.Paging
{
    public class EdgeTypeTests
    {
        [Fact]
        public void CheckThatNameIsCorrect()
        {
            // arrange
            // act
            var type = new EdgeType<StringType>();

            // assert
            Assert.Equal("StringEdge", type.Name);
        }

        [Fact]
        public void CheckFieldsAreCorrect()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

            // act
            var type = new EdgeType<StringType>();

            // assert
            INeedsInitialization init = type;

            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), type, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Collection(type.Fields.Where(t => !t.IsIntrospectionField),
                t =>
                {
                    Assert.Equal("cursor", t.Name);
                    Assert.IsType<NonNullType>(t.Type);
                    Assert.IsType<StringType>(((NonNullType)t.Type).Type);
                },
                t =>
                {
                    Assert.Equal("node", t.Name);
                    Assert.IsType<StringType>(t.Type);
                });
        }
    }
}
