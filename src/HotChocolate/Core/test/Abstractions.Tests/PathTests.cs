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
            Path path = MemoryPathFactory.Instance.New("hero");
            path = MemoryPathFactory.Instance.Append(path, "friends");
            path = MemoryPathFactory.Instance.Append(path, 0);
            path = MemoryPathFactory.Instance.Append(path, "name");

            // act
            string result = path.ToString();

            // assert
            Assert.Equal("/hero/friends[0]/name", result);
        }

        [Fact]
        public void Path_ToList()
        {
            // arrange
            Path path = MemoryPathFactory.Instance.New("hero");
            path = MemoryPathFactory.Instance.Append(path, "friends");
            path = MemoryPathFactory.Instance.Append(path, 0);
            path = MemoryPathFactory.Instance.Append(path, "name");

            // act
            IReadOnlyList<object> result = path.ToList();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Path_Equals_Null()
        {
            // arrange
            Path hero = MemoryPathFactory.Instance.New("hero");
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
            Path hero = MemoryPathFactory.Instance.New("hero");
            Path friends = MemoryPathFactory.Instance.New("hero");
            friends = MemoryPathFactory.Instance.Append(friends, "friends");

            // act
            var areEqual = hero.Equals(friends);

            // assert
            Assert.False(areEqual);
        }

        [Fact]
        public void Path_Equals_True()
        {
            // arrange
            Path friends1 = MemoryPathFactory.Instance.New("hero");
            friends1 = MemoryPathFactory.Instance.Append(friends1, "friends");
            Path friends2 = MemoryPathFactory.Instance.New("hero");
            friends2 = MemoryPathFactory.Instance.Append(friends2, "friends");

            // act
            var areEqual = friends1.Equals(friends2);

            // assert
            Assert.True(areEqual);
        }
    }
}
