using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace HotChocolate.Types.Paging
{
    public class PageableDataTests
    {
        [Fact]
        public void Create_WithEnumerable_ArgumentIsCorrectlyPassed()
        {
            // arrange
            // act
            var data = new PageableData<string>(new[] { "a", "b" });

            // assert
            Assert.Collection(data.Source,
                t => Assert.Equal("a", t),
                t => Assert.Equal("b", t));
            Assert.Null(data.Properties);
        }

        [Fact]
        public void Create_WithEnumerableAndProps_ArgumentIsCorrectlyPassed()
        {
            // arrange
            // act
            var data = new PageableData<string>(
                new[] { "a", "b" },
                new Dictionary<string, object> { { "a", "b" } });

            // assert
            Assert.Collection(data.Source,
                t => Assert.Equal("a", t),
                t => Assert.Equal("b", t));
            Assert.Collection(data.Properties,
                t =>
                {
                    Assert.Equal("a", t.Key);
                    Assert.Equal("b", t.Value);
                });
        }

        [Fact]
        public void Create_WithQueryable_ArgumentIsCorrectlyPassed()
        {
            // arrange
            // act
            var data = new PageableData<string>(
                new[] { "a", "b" }.AsQueryable());

            // assert
            Assert.Collection(data.Source,
                t => Assert.Equal("a", t),
                t => Assert.Equal("b", t));
            Assert.Null(data.Properties);
        }

        [Fact]
        public void Create_WithQueryableAndProps_ArgumentIsCorrectlyPassed()
        {
            // arrange
            // act
            var data = new PageableData<string>(
                new[] { "a", "b" }.AsQueryable(),
                new Dictionary<string, object> { { "a", "b" } });

            // assert
            Assert.Collection(data.Source,
                t => Assert.Equal("a", t),
                t => Assert.Equal("b", t));
            Assert.Collection(data.Properties,
                t =>
                {
                    Assert.Equal("a", t.Key);
                    Assert.Equal("b", t.Value);
                });
        }
    }
}
