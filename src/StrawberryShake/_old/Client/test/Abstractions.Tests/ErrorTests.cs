using System;
using System.Collections.Generic;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake
{
    public class ErrorTests
    {
        [Fact]
        public void Create_Error_All_Properties_Set()
        {
            // arrange
            // act
            var error = new Error
            (
                "Abc",
                new List<object> { "abc", 1 },
                new List<Location> { new Location(1, 2) },
                new Dictionary<string, object> { { "abc", "def" } },
                new Exception("Def")
            );

            // assert
            error.MatchSnapshot();
        }

        [Fact]
        public void Message_Is_Null()
        {
            // arrange
            // act
            Action action = () => new Error(null, null, null, null, null);

            // assert
            Assert.Equal("message",
                Assert.Throws<ArgumentNullException>(action).ParamName);
        }
    }
}
