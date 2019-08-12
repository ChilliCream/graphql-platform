using System;
using System.Reflection;
using Xunit;

namespace HotChocolate.Utilities
{
    public class ActivatorHelperTests
    {
        [Fact]
        public void CreateInstanceFactory_TypeInfoNull_ArgExec()
        {
            // arrange
            // act
            Action action = () => ActivatorHelper
                .CompileFactory(default(TypeInfo));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void CreateInstanceFactoryT_TypeInfoNull_ArgExec()
        {
            // arrange
            // act
            Action action = () => ActivatorHelper
                .CompileFactory<object>(default(TypeInfo));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
