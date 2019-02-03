using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using Moq;
using Xunit;

namespace HotChocolate.Types
{
    public class ResolverObjectFieldDescriptorExtensionsTests
    {
        [Fact]
        public void Resolver_IResolverContextObject_ContextNull_ArgExc()
        {
            // arrange
            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver(
                        null,
                        new Func<IResolverContext, object>(c => new object()));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_IResolverContextObject_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver(
                        descriptor.Object,
                       default(Func<IResolverContext, object>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_IResolverContextTaskOfObject_ContextNull_ArgExc()
        {
            // arrange
            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver(
                        null,
                        new Func<IResolverContext, Task<object>>(c =>
                            Task.FromResult(new object())));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_IResolverContextTaskOdObject_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver(
                        descriptor.Object,
                       default(Func<IResolverContext, Task<object>>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

    }
}
