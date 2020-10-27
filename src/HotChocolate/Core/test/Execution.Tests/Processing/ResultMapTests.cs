using System;
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

        [Fact]
        public void Complete()
        {
            // arrange
            var resultMap = new ResultMap();
            resultMap.EnsureCapacity(5);

            // act
            resultMap.SetValue(0, "abc1", "def");
            resultMap.SetValue(2, "abc2", "def");
            resultMap.SetValue(4, "abc3", "def");
            resultMap.Complete();

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
    }
}
