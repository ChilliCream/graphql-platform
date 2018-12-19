using System;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotChocolate.Runtime
{
    public class ServiceFactoryTests
    {
        [Fact]
        public void TypeArgumentValidation()
        {
            // arrange
            ServiceFactory factory = new ServiceFactory();

            // act
            Action a = () => factory.CreateInstance(null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void CreateInstanceWithoutServiceProvider()
        {
            // arrange
            ServiceFactory factory = new ServiceFactory();

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
            var services = new ServiceCollection();
            services.AddSingleton<ClassWithNoDependencies>();
            IServiceProvider serviceProvider =
                 services.BuildServiceProvider();

            var factory = new ServiceFactory();
            factory.Services = serviceProvider;

            // act
            object instance =
                factory.CreateInstance(typeof(ClassWithDependencies));

            // assert
            Assert.NotNull(instance);
            Assert.IsType<ClassWithDependencies>(instance);

            ClassWithDependencies classWithDependencies =
                (ClassWithDependencies)instance;
            Assert.NotNull(classWithDependencies.Dependency);
        }

        private class ClassWithNoDependencies
        {
        }

        private class ClassWithDependencies
        {
            public ClassWithDependencies(ClassWithNoDependencies dependency)
            {
                Dependency = dependency;
            }

            public ClassWithNoDependencies Dependency { get; }
        }
    }
}
