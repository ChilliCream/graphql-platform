using System.Collections.Generic;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate
{
    public class PathTests
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
            var result = path.ToString();

            // assert
            Assert.Equal("/hero/friends[0]/name", result);
        }

        [Fact]
        public void Path_ToString_With_Index_MaxValue()
        {
            // arrange
            Path path = Path
                .New("hero")
                .Append("friends")
                .Append(int.MaxValue)
                .Append("name");

            // act
            var result = path.ToString();

            // assert
            Assert.Equal($"/hero/friends[{int.MaxValue}]/name", result);
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
            Path? friends = null;

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

        [Fact]
        public void Path_Is_Cached()
        {
            // arrange
            Path friends1 = Path.New("hero").Append("friends");
            Path friends2 = Path.New("hero").Append("friends");

            // act
            var areEqual = ReferenceEquals(friends1, friends2);

            // assert
            Assert.True(areEqual);
        }
    }
}
