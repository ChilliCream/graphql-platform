using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using Xunit;

namespace HotChocolate.Types.Paging
{
    public class PageInfoTypeTests
    {
        [Fact]
        public void CheckThatNameIsCorrect()
        {
            // arrange
            // act
            var type = new PageInfoType();

            // assert
            Assert.Equal("PageInfo", type.Name);
        }

        [Fact]
        public void CheckFieldsAreCorrect()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

            // act
            var type = new PageInfoType();

            // assert
            INeedsInitialization init = type;

            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), type, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Collection(type.Fields.Where(t => !t.IsIntrospectionField),
                t =>
                {
                    Assert.Equal("hasNextPage", t.Name);
                    Assert.Equal(
                        "Indicates whether more edges exist following " +
                        "the set defined by the clients arguments.",
                        t.Description);
                    Assert.IsType<NonNullType>(t.Type);
                    Assert.IsType<BooleanType>(((NonNullType)t.Type).Type);
                },
                t =>
                {
                    Assert.Equal("hasPreviousPage", t.Name);
                    Assert.Equal(
                        "Indicates whether more edges exist prior " +
                        "the set defined by the clients arguments.",
                        t.Description);
                    Assert.IsType<NonNullType>(t.Type);
                    Assert.IsType<BooleanType>(((NonNullType)t.Type).Type);
                },
                t =>
                {
                    Assert.Equal("startCursor", t.Name);
                    Assert.IsType<StringType>(t.Type);
                },
                t =>
                {
                    Assert.Equal("endCursor", t.Name);
                    Assert.IsType<StringType>(t.Type);
                });
        }
    }
}
