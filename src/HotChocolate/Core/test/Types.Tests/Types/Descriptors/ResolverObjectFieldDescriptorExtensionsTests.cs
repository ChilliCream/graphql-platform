using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using Moq;
using Xunit;

namespace HotChocolate.Types.Descriptors
{
    [Obsolete]
    public class ResolverObjectFieldDescriptorExtensionsTests
    {
        [Fact]
        public void Resolver_IResolverContextObject_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() =>
                ResolverObjectFieldDescriptorExtensions.Resolver(
                    null!, _ => new object());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverContextObject_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                descriptor.Object,
                default(Func<IResolverContext, object>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public async Task Resolver_IResolverContextObject_ResolverIsSet()
        {
            // arrange
            FieldResolverDelegate resolver = null;
            var resolverFunc = new Func<IResolverContext, object>(_ => "foo");
            var descriptor = new Mock<IObjectFieldDescriptor>();
            descriptor.Setup(t => t.Resolver(It.IsAny<FieldResolverDelegate>()))
                .Returns(
                    new Func<FieldResolverDelegate, IObjectFieldDescriptor>(
                    r =>
                    {
                        resolver = r;
                        return descriptor.Object;
                    }));

            // act
            ResolverObjectFieldDescriptorExtensions
                .Resolver(descriptor.Object, resolverFunc);

            // assert
            Assert.Equal("foo", await resolver.Invoke(
                new Mock<IResolverContext>().Object));
        }

        [Fact]
        public void Resolver_IResolverContextTaskOfObject_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                null!,
                _ => Task.FromResult(new object()));

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverContextTaskOfObject_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                descriptor.Object,
                default(Func<IResolverContext, Task<object>>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverContextT_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver<object>(
                null!,
                _ => new object());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverContextT_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver<object>(
                descriptor.Object,
                default(Func<IResolverContext, object>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverContextTaskOfT_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver<object>(
                null!,
                _ => Task.FromResult(new object()));

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverContextTaskOfT_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver<object>(
                descriptor.Object,
                default(Func<IResolverContext, Task<object>>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_Object_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                null!,
                () => new object());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_Object_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                descriptor.Object,
                default(Func<object>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_TaskOfObject_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                null!,
                () => Task.FromResult(new object()));

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_TaskOfObject_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                descriptor.Object,
                default(Func<Task<object>>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_T_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver<object>(
                null!,
                () => new object());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_T_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver<object>(
                descriptor.Object,
                default(Func<object>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_TaskOfT_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver<object>(
                null!,
                () => Task.FromResult(new object()));

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_TaskOfT_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver<object>(
                descriptor.Object,
                default(Func<Task<object>>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverContextCtObject_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                null!,
                (_, _) => new object());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverContextCtObject_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                descriptor.Object,
                default(Func<IResolverContext, CancellationToken, object>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverContextCtTaskOfObject_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                null!,
                (_, _) => Task.FromResult(new object()));

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverCtxCtTaskOfObject_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                descriptor.Object,
                default(Func<IResolverContext, CancellationToken, Task<object>>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverContextCtT_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver<object>(
                null!,
                (_, _) => new object());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverContextCtT_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver<object>(
                descriptor.Object,
                default(Func<IResolverContext, CancellationToken, object>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverContextCtTaskOfT_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                null!,
                (_, _) => Task.FromResult(new object()));

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_IResolverCtxCtTaskOfT_ResolverNull_ArgExc()
        {
            // arrange
            var descriptor = new Mock<IObjectFieldDescriptor>();

            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                descriptor.Object,
                default(Func<IResolverContext, CancellationToken, Task<object>>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_Constant_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver(
                null!,
                new object());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Resolver_ConstantT_DescNull_ArgExc()
        {
            // arrange
            // act
            void Action() => ResolverObjectFieldDescriptorExtensions.Resolver<object>(
                null!,
                new object());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }
    }
}
