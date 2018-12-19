using System;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Execution
{
    public class QueryResultTests
    {
        [Fact]
        public void Create_OnlyData_DataIsReadOnly()
        {
            // arrange
            var data = new OrderedDictionary();
            Assert.False(data.IsReadOnly);

            // act
            var result = new QueryResult(data);

            // assert
            Assert.True(result.Data.IsReadOnly);
            Assert.True(data.IsReadOnly);
        }

        [Fact]
        public void ToJson_OnlyData_JsonStringNoIndentations()
        {
            // arrange
            var data = new OrderedDictionary();
            var result = new QueryResult(data);

            // act
            string json = result.ToJson(false);


            // assert

        }

        [Fact]
        public void Create_OnlyData_ErrorIsNull()
        {
            // arrange
            var data = new OrderedDictionary();

            // act
            var result = new QueryResult(data);

            // assert
            Assert.Null(result.Errors);
        }

        [Fact]
        public void Create_DataIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action a = () => new QueryResult((OrderedDictionary)null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void Create_OnlyError_ErrorIsReadOnly()
        {
            // arrange
            var data = new OrderedDictionary();
            Assert.False(data.IsReadOnly);

            // act
            var result = new QueryResult(data);

            // assert
            Assert.True(result.Data.IsReadOnly);
            Assert.True(data.IsReadOnly);
        }

        [Fact]
        public void Create_WithOneError_ContainsOneError()
        {
            // arrange
            QueryError error = new QueryError("foo");

            // act
            var result = new QueryResult(error);

            // assert
            Assert.Collection(result.Errors,
                t => Assert.Equal(error, t));
        }

        [Fact]
        public void Create_OnlyError_DataIsNull()
        {
            // arrange
            QueryError error = new QueryError("foo");

            // act
            var result = new QueryResult(error);

            // assert
            Assert.Null(result.Data);
        }
    }
}
