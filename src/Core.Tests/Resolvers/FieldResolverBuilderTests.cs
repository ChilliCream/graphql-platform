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
            Mock<IResolverContext> context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<FooType>()).Returns(new FooType());

            FieldMember fieldMember = new FieldMember(
                "type", "field",
                typeof(FooType).GetMethod("Bar"));

            var descriptor = new SourceResolverDescriptor(fieldMember);

            // act
            FieldResolverBuilder fieldResolverBuilder = new FieldResolverBuilder();
            FieldResolver[] resolvers = fieldResolverBuilder.Build(
                new[] { descriptor }).ToArray();

            // assert
            Assert.Collection(resolvers,
                r =>
                {
                    Assert.Equal("type", r.TypeName);
                    Assert.Equal("field", r.FieldName);
                    Assert.NotNull(r.Resolver);

                    object result = r.Resolver(context.Object, CancellationToken.None);
                    Assert.Equal("Hello World", result);
                });
        }

        [Fact]
        public void CreateSyncCollectionMethodResolver()
        {
            // arrange
            Mock<IResolverContext> context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<FooType>()).Returns(new FooType());
            context.Setup(t => t.Service<FooTypeResolver>()).Returns(new FooTypeResolver());

            ArgumentDescriptor argumentDescriptor =
               new ArgumentDescriptor(
                   "foo", "b", ArgumentKind.Source,
                   typeof(FooType));

            FieldMember fieldMember = new FieldMember(
                "type", "field",
                typeof(FooTypeResolver).GetMethod("BarResolver"));

            ResolverDescriptor descriptor = new ResolverDescriptor(
                typeof(FooTypeResolver),
                typeof(FooType),
                fieldMember,
                new[] { argumentDescriptor });

            // act
            FieldResolverBuilder fieldResolverBuilder = new FieldResolverBuilder();
            FieldResolver[] resolvers = fieldResolverBuilder.Build(
                new[] { descriptor }).ToArray();

            // assert
            Assert.Collection(resolvers,
                r =>
                {
                    Assert.Equal("type", r.TypeName);
                    Assert.Equal("field", r.FieldName);
                    Assert.NotNull(r.Resolver);

                    object result = r.Resolver(context.Object, CancellationToken.None);
                    Assert.Equal("Hello World_123", result);
                });
        }

        [Fact]
        public void CreateAsyncCollectionMethodResolver()
        {
            // arrange
            Mock<IResolverContext> context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<FooType>()).Returns(new FooType());
            context.Setup(t => t.Service<FooTypeResolver>()).Returns(new FooTypeResolver());

            ArgumentDescriptor argumentDescriptor =
                new ArgumentDescriptor(
                    "foo", "b", ArgumentKind.Source,
                    typeof(FooType));

            FieldMember fieldMember = new FieldMember(
                "type", "field",
                typeof(FooTypeResolver).GetMethod("BarResolverAsync"));

            ResolverDescriptor descriptor = new ResolverDescriptor(
                typeof(FooTypeResolver),
                typeof(FooType),
                fieldMember,
                new[] { argumentDescriptor });

            // act
            FieldResolverBuilder fieldResolverBuilder = new FieldResolverBuilder();
            FieldResolver[] resolvers = fieldResolverBuilder.Build(
                new[] { descriptor }).ToArray();

            // assert
            Assert.Collection(resolvers,
                r =>
                {
                    Assert.Equal("type", r.TypeName);
                    Assert.Equal("field", r.FieldName);
                    Assert.NotNull(r.Resolver);

                    object result = ((Task<object>)r.Resolver(context.Object, CancellationToken.None)).Result;
                    Assert.Equal("Hello World_123", result);
                });
        }

        [Fact]
        public void CreateSourcePropertyResolver()
        {
            // arrange
            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<FooType>()).Returns(new FooType());

            FieldMember fieldMember = new FieldMember(
                "type", "field", typeof(FooType).GetProperty("BarProperty"));
            var descriptor = new SourceResolverDescriptor(
                typeof(FooType), fieldMember);

            // act
            var fieldResolverBuilder = new FieldResolverBuilder();
            FieldResolver[] resolvers = fieldResolverBuilder.Build(
                new[] { descriptor }).ToArray();

            // assert
            Assert.Collection(resolvers,
                r =>
                {
                    Assert.Equal("type", r.TypeName);
                    Assert.Equal("field", r.FieldName);
                    Assert.NotNull(r.Resolver);

                    object result = r.Resolver(context.Object, CancellationToken.None);
                    Assert.Equal("Hello World Property", result);
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
