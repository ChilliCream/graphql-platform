#nullable enable

using HotChocolate.Resolvers;
using Moq;

namespace HotChocolate.Types;

public class ResolveObjectFieldDescriptorExtensionsTests
{
    [Fact]
    public void Resolver_IResolverContextObject_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    null!,
                    new Func<IResolverContext, object>(
                        _ => new object()));

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
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    descriptor.Object,
                    default(Func<IResolverContext, object?>)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task Resolver_IResolverContextObject_ResolverIsSet()
    {
        // arrange
        FieldResolverDelegate resolver = null!;
        var resolverFunc = new Func<IResolverContext, object>(c => "foo");
        var descriptor = new Mock<IObjectFieldDescriptor>();
        descriptor.Setup(t => t.Resolve(It.IsAny<FieldResolverDelegate>()))
            .Returns(
                new Func<FieldResolverDelegate, IObjectFieldDescriptor>(
                    r =>
                    {
                        resolver = r;
                        return descriptor.Object;
                    }));

        // act
        ResolveObjectFieldDescriptorExtensions
            .Resolve(descriptor.Object, resolverFunc);

        // assert
        Assert.Equal("foo", await resolver.Invoke(
            new Mock<IResolverContext>().Object));
    }

    [Fact]
    public void Resolver_IResolverContextTaskOfObject_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    null!,
                    new Func<IResolverContext, Task<object?>?>(
                        async _ => await Task.FromResult(new object())));

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
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    descriptor.Object,
                    default(Func<IResolverContext, Task<object?>?>)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_IResolverContextT_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve<object>(
                    null!,
                    new Func<IResolverContext, object>(
                        _ => new object()));

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
            ResolveObjectFieldDescriptorExtensions
                .Resolve<object>(
                    descriptor.Object,
                    default(Func<IResolverContext, object>)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_IResolverContextTaskOfT_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve<object>(
                    null!,
                    new Func<IResolverContext, Task<object>>(
                        _ => Task.FromResult(new object())));

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
            ResolveObjectFieldDescriptorExtensions
                .Resolve<object>(
                    descriptor.Object,
                    default(Func<IResolverContext, Task<object>>)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_Object_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    null!,
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
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    descriptor.Object,
                    default(Func<object>)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_TaskOfObject_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    null!,
                    new Func<Task<object>>(
                        () => Task.FromResult(new object()))!);

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
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    descriptor.Object,
                    default(Func<Task<object>>)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_T_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve<object>(
                    null!,
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
            ResolveObjectFieldDescriptorExtensions
                .Resolve<object>(
                    descriptor.Object,
                    default(Func<object>)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_TaskOfT_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve<object>(
                    null!,
                    new Func<Task<object>>(
                        () => Task.FromResult(new object())));

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
            ResolveObjectFieldDescriptorExtensions
                .Resolve<object>(
                    descriptor.Object,
                    default(Func<Task<object>>)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_IResolverContextCtObject_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    null!,
                    new Func<IResolverContext, CancellationToken, object>(
                        (_, _) => new object()));

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_IResolverContextCtObject_ResolverNull_ArgExc()
    {
        // arrange
        var descriptor = new Mock<IObjectFieldDescriptor>();

        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    descriptor.Object,
                    default(Func<IResolverContext, CancellationToken, object>)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_IResolverContextCtTaskOfObject_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    null!,
                    new Func<IResolverContext, CancellationToken,
                        Task<object>>((_, _) =>
                        Task.FromResult(new object())));

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_IResolverCtxCtTaskOfObject_ResolverNull_ArgExc()
    {
        // arrange
        var descriptor = new Mock<IObjectFieldDescriptor>();

        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    descriptor.Object,
                    default(Func<IResolverContext, CancellationToken, Task<object>?>)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_IResolverContextCtT_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve<object>(
                    null!,
                    new Func<IResolverContext, CancellationToken, object>(
                        (_, _) => new object()));

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_IResolverContextCtT_ResolverNull_ArgExc()
    {
        // arrange
        var descriptor = new Mock<IObjectFieldDescriptor>();

        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve<object>(
                    descriptor.Object,
                    default(Func<IResolverContext, CancellationToken, object>)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_IResolverContextCtTaskOfT_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    null!,
                    new Func<IResolverContext, CancellationToken, Task<object>>(
                        (_, _) => Task.FromResult(new object())));

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_IResolverCtxCtTaskOfT_ResolverNull_ArgExc()
    {
        // arrange
        var descriptor = new Mock<IObjectFieldDescriptor>();

        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve(
                    descriptor.Object,
                    default(Func<IResolverContext, CancellationToken, Task<object>>)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_Constant_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve(null!, new object());

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Resolver_ConstantT_DescNull_ArgExc()
    {
        // arrange
        // act
        Action action = () =>
            ResolveObjectFieldDescriptorExtensions
                .Resolve<object>(null!, new object());

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }
}
