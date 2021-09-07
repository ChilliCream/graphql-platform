using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Execution.Processing
{
    public class ResultMapTests
    {
        [InlineData(8)]
        [InlineData(4)]
        [InlineData(2)]
        [Theory]
        public void EnsureCapacity(int size)
        {
            // arrange
            var resultMap = new ResultMap();

            // act
            resultMap.EnsureCapacity(size);

            // assert
            Assert.Equal(size, resultMap.Count);
        }

        [Fact]
        public void SetValue()
        {
            // arrange
            var resultMap = new ResultMap();
            resultMap.EnsureCapacity(1);

            // act
            resultMap.SetValue(0, "abc", "def");

            // assert
            Assert.Collection(
                (IEnumerable<ResultValue>)resultMap,
                t =>
                {
                    Assert.Equal("abc", t.Name);
                    Assert.Equal("def", t.Value);
                });
        }

        [InlineData(9)]
        [InlineData(8)]
        [InlineData(7)]
        [InlineData(5)]
        [InlineData(4)]
        [InlineData(3)]
        [Theory]
        public void GetValue_ValueIsFound(int capacity)
        {
            // arrange
            var resultMap = new ResultMap();
            resultMap.EnsureCapacity(capacity);
            resultMap.SetValue(0, "abc", "def");
            resultMap.SetValue(capacity / 2, "def", "def");
            resultMap.SetValue(capacity - 1, "ghi", "def");

            // act
            ResultValue value = resultMap.GetValue("def", out var index);

            // assert
            Assert.Equal("def", value.Name);
            Assert.Equal(capacity / 2, index);
        }

        [InlineData(9)]
        [InlineData(8)]
        [InlineData(7)]
        [InlineData(5)]
        [InlineData(4)]
        [InlineData(3)]
        [Theory]
        public void TryGetValue_ValueIsFound(int capacity)
        {
            // arrange
            var resultMap = new ResultMap();
            resultMap.EnsureCapacity(capacity);
            resultMap.SetValue(0, "abc", "def");
            resultMap.SetValue(capacity / 2, "def", "def");
            resultMap.SetValue(capacity - 1, "ghi", "def");

            IReadOnlyDictionary<string, object> dict = resultMap;

            // act
            var found = dict.TryGetValue("def", out var value);

            // assert
            Assert.True(found);
            Assert.Equal("def", value);
        }

        [InlineData(9)]
        [InlineData(8)]
        [InlineData(7)]
        [InlineData(5)]
        [InlineData(4)]
        [InlineData(3)]
        [Theory]
        public void ContainsKey(int capacity)
        {
            // arrange
            var resultMap = new ResultMap();
            resultMap.EnsureCapacity(capacity);
            resultMap.SetValue(0, "abc", "def");
            resultMap.SetValue(capacity / 2, "def", "def");
            resultMap.SetValue(capacity - 1, "ghi", "def");

            IReadOnlyDictionary<string, object> dict = resultMap;

            // act
            var found = dict.ContainsKey("def");

            // assert
            Assert.True(found);
        }

        [Fact]
        public void EnumerateResultValue()
        {
            // arrange
            var resultMap = new ResultMap();
            resultMap.EnsureCapacity(5);

            // act
            resultMap.SetValue(0, "abc1", "def");
            resultMap.SetValue(2, "abc2", "def");
            resultMap.SetValue(4, "abc3", "def");

            // assert
            Assert.Collection(
                (IEnumerable<ResultValue>)resultMap,
                t =>
                {
                    Assert.Equal("abc1", t.Name);
                    Assert.Equal("def", t.Value);
                },
                t =>
                {
                    Assert.Equal("abc2", t.Name);
                    Assert.Equal("def", t.Value);
                },
                t =>
                {
                    Assert.Equal("abc3", t.Name);
                    Assert.Equal("def", t.Value);
                });
        }

        [Fact]
        public void EnumerateKeys()
        {
            // arrange
            var resultMap = new ResultMap();
            resultMap.EnsureCapacity(5);

            // act
            resultMap.SetValue(0, "abc1", "def");
            resultMap.SetValue(2, "abc2", "def");
            resultMap.SetValue(4, "abc3", "def");

            // assert
            Assert.Collection(
                ((IReadOnlyDictionary<string, object>)resultMap).Keys,
                t => Assert.Equal("abc1", t),
                t => Assert.Equal("abc2", t),
                t => Assert.Equal("abc3", t));
        }

        [Fact]
        public void EnumerateValues()
        {
            // arrange
            var resultMap = new ResultMap();
            resultMap.EnsureCapacity(5);

            // act
            resultMap.SetValue(0, "abc1", "def");
            resultMap.SetValue(2, "abc2", "def");
            resultMap.SetValue(4, "abc3", "def");

            // assert
            Assert.Collection(
                ((IReadOnlyDictionary<string, object>)resultMap).Values,
                t => Assert.Equal("def", t),
                t => Assert.Equal("def", t),
                t => Assert.Equal("def", t));
        }
    }
}
