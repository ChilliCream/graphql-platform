using System.Collections.Generic;
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
        public void Path_ToList()
        {
            // arrange
            Path path = Path
                .New("hero")
                .Append("friends")
                .Append(0)
                .Append("name");

            // act
            IReadOnlyList<object> result = path.ToList();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Path_Equals_Null()
        {
            // arrange
            Path hero = Path.New("hero");
            Path friends = null;

            // act
            var areEqual = hero.Equals(friends);

            // assert
            Assert.False(areEqual);
        }

        [Fact]
        public void Path_Equals_False()
        {
            // arrange
            Path hero = Path.New("hero");
            Path friends = Path.New("hero").Append("friends");

            // act
            var areEqual = hero.Equals(friends);

            // assert
            Assert.False(areEqual);
        }

        [Fact]
        public void Path_Equals_True()
        {
            // arrange
            Path friends1 = Path.New("hero").Append("friends");
            Path friends2 = Path.New("hero").Append("friends");

            // act
            var areEqual = friends1.Equals(friends2);

            // assert
            Assert.True(areEqual);
        }
    }
}
