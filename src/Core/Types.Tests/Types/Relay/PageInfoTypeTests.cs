using System.Linq;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class PageInfoTypeTests
        : TypeTestBase
    {
        [Fact]
        public void CheckThatNameIsCorrect()
        {
            // arrange
            // act
            var type = CreateType(new PageInfoType());

            // assert
            Assert.Equal("PageInfo", type.Name);
        }

        [Fact]
        public void CheckFieldsAreCorrect()
        {
            // arrange
            // act
            PageInfoType type = CreateType(new PageInfoType());

            // assert
            Assert.Collection(
                type.Fields.Where(t => !t.IsIntrospectionField).OrderBy(t => t.Name),
                t =>
                {
                    Assert.Equal("endCursor", t.Name);
                    Assert.IsType<StringType>(t.Type);
                },
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
                });
        }
    }
}
