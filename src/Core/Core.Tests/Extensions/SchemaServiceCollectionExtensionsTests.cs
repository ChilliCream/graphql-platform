using System;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate
{
    public class SchemaServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddErrorFilter_1_Services_Is_Null()
        {
            // arrange
            // act
            Action action = () =>
                SchemaServiceCollectionExtensions.AddErrorFilter<MyErrorFilter>(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddErrorFilter_1()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            SchemaServiceCollectionExtensions.AddErrorFilter<MyErrorFilter>(services);

            // assert
            Assert.Collection(services,
                t =>
                {
                    Assert.Equal(typeof(IErrorFilter), t.ServiceType);
                    Assert.Equal(typeof(MyErrorFilter), t.ImplementationType);
                });
        }

        [Fact]
        public void AddErrorFilter_2_Services_Is_Null()
        {
            // arrange
            // act
            Action action = () =>
                SchemaServiceCollectionExtensions.AddErrorFilter(
                    null, error => error);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddErrorFilter_2_Filter_Is_Null()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            Action action = () =>
                SchemaServiceCollectionExtensions.AddErrorFilter(
                    services, default(Func<IError, IError>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddErrorFilter_2()
        {
            // arrange
            var services = new ServiceCollection();
            Func<IError, IError> filter = error => error;

            // act
            SchemaServiceCollectionExtensions.AddErrorFilter(services, filter);

            // assert
            Assert.Collection(services,
                t =>
                {
                    Assert.Equal(typeof(IErrorFilter), t.ServiceType);
                    Assert.IsType<FuncErrorFilterWrapper>(t.ImplementationInstance);
                });
        }

        [Fact]
        public void AddErrorFilter_3_Services_Is_Null()
        {
            // arrange
            // act
            Action action = () =>
                SchemaServiceCollectionExtensions.AddErrorFilter(
                    null, sp => new MyErrorFilter());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddErrorFilter_3_Filter_Is_Null()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            Action action = () =>
                SchemaServiceCollectionExtensions.AddErrorFilter(
                    services, default(Func<IServiceProvider, IErrorFilter>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddErrorFilter_3()
        {
            // arrange
            var services = new ServiceCollection();
            Func<IServiceProvider, IErrorFilter> factory =
                sp => sp.GetRequiredService<IErrorFilter>();

            // act
            SchemaServiceCollectionExtensions.AddErrorFilter(services, factory);

            // assert
            Assert.Collection(services,
                t =>
                {
                    Assert.Equal(typeof(IErrorFilter), t.ServiceType);
                    Assert.Equal(factory, t.ImplementationFactory);
                });
        }

        public class MyErrorFilter
            : IErrorFilter
        {
            public IError OnError(IError error)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
