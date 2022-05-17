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
            Path path = PathFactory.Instance.New("hero");
            path = PathFactory.Instance.Append(path, "friends");
            path = PathFactory.Instance.Append(path, 0);
            path = PathFactory.Instance.Append(path, "name");

            // act
            string result = path.ToString();

            // assert
            Assert.Equal("/hero/friends[0]/name", result);
        }

        [Fact]
        public void Path_ToList()
        {
            // arrange
            Path path = PathFactory.Instance.New("hero");
            path = PathFactory.Instance.Append(path, "friends");
            path = PathFactory.Instance.Append(path, 0);
            path = PathFactory.Instance.Append(path, "name");

            // act
            IReadOnlyList<object> result = path.ToList();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Path_Equals_Null()
        {
            // arrange
            Path hero = PathFactory.Instance.New("hero");
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
            Path hero = PathFactory.Instance.New("hero");
            Path friends = PathFactory.Instance.New("hero");
            friends = PathFactory.Instance.Append(friends, "friends");

            // act
            var areEqual = hero.Equals(friends);

            // assert
            Assert.False(areEqual);
        }

        [Fact]
        public void Path_Equals_True()
        {
            // arrange
            Path friends1 = PathFactory.Instance.New("hero");
            friends1 = PathFactory.Instance.Append(friends1, "friends");
            Path friends2 = PathFactory.Instance.New("hero");
            friends2 = PathFactory.Instance.Append(friends2, "friends");

            // act
            var areEqual = friends1.Equals(friends2);

            // assert
            Assert.True(areEqual);
        }
    }
}
