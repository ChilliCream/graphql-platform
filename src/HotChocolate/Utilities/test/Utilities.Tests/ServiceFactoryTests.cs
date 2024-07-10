using System;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Utilities;

public class ServiceFactoryTests
{
    [Fact]
    public void TypeArgumentValidation()
    {
        // arrange
        // act
        void Error() => ServiceFactory.CreateInstance(EmptyServiceProvider.Instance, null!);

        // assert
        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void CreateInstanceWithoutServiceProvider()
    {
        // arrange
        // act
        var instance = ServiceFactory.CreateInstance(EmptyServiceProvider.Instance, typeof(ClassWithNoDependencies));

        // assert
        Assert.NotNull(instance);
        Assert.IsType<ClassWithNoDependencies>(instance);
    }

    [Fact]
    public void CreateInstanceWithServiceProvider()
    {
        // arrange
        var serviceProvider = new DictionaryServiceProvider(
            typeof(ClassWithNoDependencies),
            new ClassWithNoDependencies());
        
        // act
        var instance = ServiceFactory.CreateInstance(serviceProvider, typeof(ClassWithDependencies));

        // assert
        Assert.NotNull(instance);
        Assert.IsType<ClassWithDependencies>(instance);

        var classWithDependencies =
            (ClassWithDependencies)instance;
        Assert.NotNull(classWithDependencies.Dependency);
    }

    [Fact]
    public void Catch_Exception_On_Create()
    {
        // arrange
        var type = typeof(ClassWithException);

        // act
        void Error() => ServiceFactory.CreateInstance(EmptyServiceProvider.Instance, type);

        // assert
        Assert.Throws<ServiceException>(Error)
            .Message.MatchSnapshot();
    }

    [Fact]
    public void Cannot_Resolve_Dependencies()
    {
        // arrange
        var type = typeof(ClassWithDependencies);

        // act
        void Error() => ServiceFactory.CreateInstance(EmptyServiceProvider.Instance, type);

        // assert
        Assert.Throws<ServiceException>(Error)
            .Message.MatchSnapshot();
    }

    [Fact]
    public void No_Services_Available()
    {
        // arrange
        var type = typeof(ClassWithDependencies);

        // act
        void Error() => ServiceFactory.CreateInstance(EmptyServiceProvider.Instance, type);

        // assert
        Assert.Throws<ServiceException>(Error)
            .Message.MatchSnapshot();
    }

    private sealed class ClassWithNoDependencies;

    private sealed class ClassWithDependencies(ClassWithNoDependencies dependency)
    {
        public ClassWithNoDependencies Dependency { get; } = 
            dependency ?? throw new ArgumentNullException(nameof(dependency));
    }

    private sealed class ClassWithException
    {
        public ClassWithException()
        {
            throw new NullReferenceException();
        }
    }
}