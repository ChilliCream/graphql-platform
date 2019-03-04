using System.Collections.Generic;
using ChilliCream.Testing;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Instrumentation
{
    public class PathExtensionsTests
    {
        [Fact]
        public void Path_ToString()
        {
            // arrange
            Path path = Path
                .New("hero")
                .Append("friends")
                .Append(0)
                .Append("name");

            // act
            string result = path.ToString();

            // assert
            Assert.Equal("/hero/friends[0]/name", result);
        }

        [Fact]
        public void Path_ToCollection()
        {
            // arrange
            Path path = Path
                .New("hero")
                .Append("friends")
                .Append(0)
                .Append("name");

            // act
            IReadOnlyCollection<object> result = path.ToCollection();

            // assert
            result.MatchSnapshot();
        }
    }
}
