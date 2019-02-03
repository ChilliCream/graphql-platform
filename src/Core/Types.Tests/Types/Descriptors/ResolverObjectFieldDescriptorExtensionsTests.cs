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
        public void Resolver_IResolverContextObject_DescNull_ArgExc()
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
        public void Resolver_IResolverContextTaskOfObject_DescNull_ArgExc()
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
        public void Resolver_IResolverContextTaskOfObject_ResolverNull_ArgExc()
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

        [Fact]
        public void Resolver_IResolverContextT_DescNull_ArgExc()
        {
            // arrange
            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver<object>(
                        null,
                        new Func<IResolverContext, object>(c => new object()));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_IResolverContextT_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver<object>(
                        descriptor.Object,
                       default(Func<IResolverContext, object>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_IResolverContextTaskOfT_DescNull_ArgExc()
        {
            // arrange
            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver<object>(
                        null,
                        new Func<IResolverContext, Task<object>>(c =>
                            Task.FromResult(new object())));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_IResolverContextTaskOfT_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver<object>(
                        descriptor.Object,
                       default(Func<IResolverContext, Task<object>>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_Object_DescNull_ArgExc()
        {
            // arrange
            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver(
                        null,
                        new Func<object>(() => new object()));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_Object_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver(
                        descriptor.Object,
                        default(Func<object>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_TaskOfObject_DescNull_ArgExc()
        {
            // arrange
            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver(
                        null,
                        new Func<Task<object>>(() =>
                            Task.FromResult(new object())));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_TaskOfObject_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver(
                        descriptor.Object,
                        default(Func<Task<object>>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_T_DescNull_ArgExc()
        {
            // arrange
            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver<object>(
                        null,
                        new Func<object>(() => new object()));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_T_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver<object>(
                        descriptor.Object,
                        default(Func<object>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_TaskOfT_DescNull_ArgExc()
        {
            // arrange
            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver<object>(
                        null,
                        new Func<Task<object>>(() =>
                            Task.FromResult(new object())));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Resolver_TaskOfT_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            Action action = () =>
                ResolverObjectFieldDescriptorExtensions
                    .Resolver<object>(
                        descriptor.Object,
                        default(Func<Task<object>>));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
