using System;
using System.Collections.Generic;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class QueryResultBuilderTests
    {
        [Fact]
        public void Create_Result_Without_Data_And_Errors()
        {
            // arrange
            // act
            Action result = () => QueryResultBuilder.New().Create();

            // assert
            Assert.Throws<ArgumentException>(result);
        }

        [Fact]
        public void Create_Result_Set_Data()
        {
            // arrange
            var builder = new QueryResultBuilder();

            // act
            builder.SetData(new Dictionary<string, object> { { "a", "b" } });

            // assert
            builder.Create().MatchSnapshot();
        }
    }
}
