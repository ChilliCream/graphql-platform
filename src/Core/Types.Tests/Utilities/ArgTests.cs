using System;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Utilities
{
    public class ArgTests
    {
        [Fact]
        public void WhenCalled_ShouldThrow()
        {
            // arrange
            Action action = () => Arg.Is<string>();

            // act
            // assert
            Assert.Throws<NotSupportedException>(action);
        }
    }
}
