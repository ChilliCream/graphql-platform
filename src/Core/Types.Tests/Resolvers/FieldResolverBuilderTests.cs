using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers.CodeGeneration;
using Moq;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class FieldResolverBuilderTests
    {
        [Fact]
        public void CreateSyncSourceMethodResolver()
        {
            // arrange
            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<FooType>())
                .Returns(new FooType());
            context.Setup(t => t.RequestAborted)
                .Returns(CancellationToken.None);

            var fieldMember = new FieldMember(
                "type", "field",
                typeof(FooType).GetMethod("Bar"));

            var descriptor = new SourceResolverDescriptor(fieldMember);

            // act
            var resolverBuilder = new ResolverBuilder();
            resolverBuilder.AddDescriptor(descriptor);
            ResolverBuilderResult result = resolverBuilder.Build();

            // assert
            Assert.Collection(result.Resolvers,
                r =>
                {
                    Assert.Equal("type", r.TypeName);
                    Assert.Equal("field", r.FieldName);
                    Assert.NotNull(r.Resolver);

                    object resolvedValue = r.Resolver(
                        context.Object).Result;
                    Assert.Equal("Hello World", resolvedValue);
                });
        }

        [Fact]
        public void CreateSyncCollectionMethodResolver()
        {
            // arrange
            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<FooType>())
                .Returns(new FooType());
            context.Setup(t => t.Resolver<FooTypeResolver>())
                .Returns(new FooTypeResolver());
            context.Setup(t => t.RequestAborted)
                .Returns(CancellationToken.None);

            var argumentDescriptor =
               new ArgumentDescriptor(
                   "foo", "b", ArgumentKind.Source,
                   typeof(FooType),
                   null);

            var fieldMember = new FieldMember(
                "type", "field",
                typeof(FooTypeResolver).GetMethod("BarResolver"));

            var descriptor = new ResolverDescriptor(
                typeof(FooTypeResolver),
                typeof(FooType),
                fieldMember,
                new[] { argumentDescriptor });

            // act
            var resolverBuilder = new ResolverBuilder();
            resolverBuilder.AddDescriptor(descriptor);
            ResolverBuilderResult result = resolverBuilder.Build();

            // assert
            Assert.Collection(result.Resolvers,
                r =>
                {
                    Assert.Equal("type", r.TypeName);
                    Assert.Equal("field", r.FieldName);
                    Assert.NotNull(r.Resolver);

                    object resolvedValue = r.Resolver(
                        context.Object).Result;
                    Assert.Equal("Hello World_123", resolvedValue);
                });
        }

        [Fact]
        public void CreateAsyncCollectionMethodResolver()
        {
            // arrange
            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<FooType>())
                .Returns(new FooType());
            context.Setup(t => t.Resolver<FooTypeResolver>())
                .Returns(new FooTypeResolver());
            context.Setup(t => t.RequestAborted)
                .Returns(CancellationToken.None);

            var argumentDescriptor =
                new ArgumentDescriptor(
                    "foo", "b", ArgumentKind.Source,
                    typeof(FooType),
                    null);

            var fieldMember = new FieldMember(
                "type", "field",
                typeof(FooTypeResolver).GetMethod("BarResolverAsync"));

            var descriptor = new ResolverDescriptor(
                typeof(FooTypeResolver),
                typeof(FooType),
                fieldMember,
                new[] { argumentDescriptor });

            // act
            var resolverBuilder = new ResolverBuilder();
            resolverBuilder.AddDescriptor(descriptor);
            ResolverBuilderResult result = resolverBuilder.Build();

            // assert
            Assert.Collection(result.Resolvers,
                r =>
                {
                    Assert.Equal("type", r.TypeName);
                    Assert.Equal("field", r.FieldName);
                    Assert.NotNull(r.Resolver);

                    object resolvedValue = r.Resolver(
                            context.Object)
                            .Result;
                    Assert.Equal("Hello World_123", resolvedValue);
                });
        }

        [Fact]
        public void CreateSourcePropertyResolver()
        {
            // arrange
            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<FooType>())
                .Returns(new FooType());
            context.Setup(t => t.RequestAborted)
                .Returns(CancellationToken.None);

            var fieldMember = new FieldMember(
                "type", "field", typeof(FooType).GetProperty("BarProperty"));
            var descriptor = new SourceResolverDescriptor(
                typeof(FooType), fieldMember);

            // act
            var resolverBuilder = new ResolverBuilder();
            resolverBuilder.AddDescriptor(descriptor);
            ResolverBuilderResult result = resolverBuilder.Build();

            // assert
            Assert.Collection(result.Resolvers,
                r =>
                {
                    Assert.Equal("type", r.TypeName);
                    Assert.Equal("field", r.FieldName);
                    Assert.NotNull(r.Resolver);

                    object resolvedResult = r.Resolver(
                            context.Object).Result;
                    Assert.Equal("Hello World Property", resolvedResult);
                });
        }
    }

    public class FooType
    {
        public string Bar()
        {
            return "Hello World";
        }

        public string BarProperty { get; } = "Hello World Property";
    }

    public class FooTypeResolver
    {
        public string BarResolver(FooType foo)
        {
            return foo.Bar() + "_123";
        }

        public Task<string> BarResolverAsync(FooType foo)
        {
            return Task.FromResult(foo.Bar() + "_123");
        }

        public string BarResolverProperty { get; } = "Hello World Property_123";
    }
}
