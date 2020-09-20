using System;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Utilities
{
    public class ServiceFactoryTests
    {
        [Fact]
        public void TypeArgumentValidation()
        {
            // arrange
            var factory = new ServiceFactory();

            // act
            Action a = () => factory.CreateInstance(null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void CreateInstanceWithoutServiceProvider()
        {
            // arrange
            var factory = new ServiceFactory();

            // act
            object instance =
                factory.CreateInstance(typeof(ClassWithNoDependencies));

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

            var factory = new ServiceFactory();
            factory.Services = serviceProvider;

            // act
            object instance =
                factory.CreateInstance(typeof(ClassWithDependencies));

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
            var factory = new ServiceFactory();
            var type = typeof(ClassWithException);

            // act
            Action action = () => factory.CreateInstance(type);

            // assert
            Assert.Throws<ServiceException>(action)
                .Message.MatchSnapshot();
        }

        [Fact]
        public void Cannot_Resolve_Dependencies()
        {
            // arrange
            var factory = new ServiceFactory();
            var type = typeof(ClassWithDependencies);

            // act
            Action action = () => factory.CreateInstance(type);

            // assert
            Assert.Throws<ServiceException>(action)
                .Message.MatchSnapshot();
        }

        [Fact]
        public void No_Services_Available()
        {
            // arrange
            var factory = new ServiceFactory();
            var type = typeof(ClassWithDependencies);

            // act
            Action action = () => factory.CreateInstance(type);

            // assert
            Assert.Throws<ServiceException>(action)
                .Message.MatchSnapshot();
        }

        private class ClassWithNoDependencies
        {
        }

        private class ClassWithDependencies
        {
            public ClassWithDependencies(ClassWithNoDependencies dependency)
            {
                Dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
            }

            public ClassWithNoDependencies Dependency { get; }
        }

        private class ClassWithException
        {
            public ClassWithException()
            {
                throw new NullReferenceException();
            }
        }
    }
}
