using System;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Instrumentation
{
    public class DateTimeOffsetExtensionsTests
    {
        [Fact]
        public void ConvertToRfc3339DateTimeString()
        {
            // arrange
            DateTimeOffset input = new DateTime(
                636823738202230302,
                DateTimeKind.Utc);

            // act
            string result = input.ToRfc3339DateTimeString();

            // assert
            result.MatchSnapshot();
        }
    }
}
