using Xunit;

namespace HotChocolate.Execution
{
    public class QueryResultTests
    {
        [Fact]
        public void ResultPropertiesAreNotNull()
        {
            // arrange
            // act
            var result = new QueryResult();

            // assert
            Assert.NotNull(result.Data);
            Assert.NotNull(result.Extensions);
            Assert.NotNull(result.Errors);
        }

        [Fact]
        public void ResultMapsAreEditable()
        {
            // arrange
            var result = new QueryResult();

            // act
            result.Data["a"] = "a";
            result.Extensions["b"] = "b";
            result.Errors.Add(ErrorBuilder.New().SetMessage("c").Build());

            // assert
            Assert.Collection(result.Data,
                a => Assert.Equal("a", a.Value));
            Assert.Collection(result.Extensions,
                b => Assert.Equal("b", b.Value));
            Assert.Collection(result.Errors,
                c => Assert.Equal("c", c.Message));
        }

        [Fact]
        public void ResultAsReadOnly()
        {
            // arrange
            var result = new QueryResult();

            // act
            IReadOnlyQueryResult readOnlyResult = result.AsReadOnly();

            // assert
            Assert.IsType<ReadOnlyQueryResult>(readOnlyResult);
            Assert.NotNull(result.Data);
            Assert.NotNull(result.Extensions);
            Assert.NotNull(result.Errors);
        }
    }
}
