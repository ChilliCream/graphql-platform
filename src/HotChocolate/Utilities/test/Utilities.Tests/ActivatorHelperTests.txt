using System;
using System.Reflection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Utilities;

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

    [Fact]
    public void CreateInstanceFactory_From_Interface()
    {
        // arrange
        // act
        Action action = () => ActivatorHelper
            .CompileFactory(typeof(IFoo).GetTypeInfo());

        // assert
        Assert.Throws<InvalidOperationException>(action)
            .Message.MatchSnapshot();
    }

    [Fact]
    public void CreateInstanceFactory_From_AbstactClass()
    {
        // arrange
        // act
        Action action = () => ActivatorHelper
            .CompileFactory(typeof(FooBase).GetTypeInfo());

        // assert
        Assert.Throws<InvalidOperationException>(action)
            .Message.MatchSnapshot();
    }

    public interface IFoo
    {

    }

    public abstract class FooBase
    {

    }

    public class Foo
    {
        public Foo(int a)
        {

        }

        public Foo(string b)
        {

        }
    }
}
