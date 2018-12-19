using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Types.Introspection;
using Moq;
using Xunit;

namespace HotChocolate.Types
{
    public class ObjectFieldDescriptorTests
    {
        [Fact]
        public void DotNetTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            var descriptor = new ObjectFieldDescriptor("field");

            // act
            ((IObjectFieldDescriptor)descriptor)
                .Type<ListType<StringType>>()
                .Type<NativeType<IReadOnlyDictionary<string, string>>>();

            // assert
            ObjectFieldDescription description = descriptor.CreateDescription();
            TypeReference typeRef = description.TypeReference;
            Assert.Equal(typeof(ListType<StringType>), typeRef.ClrType);
        }

        [Fact]
        public void SchemaTypesOverwriteDotNetTypes()
        {
            // arrange
            var descriptor = new ObjectFieldDescriptor("field");

            // act
            ((IObjectFieldDescriptor)descriptor)
                .Type<NativeType<IReadOnlyDictionary<string, string>>>()
                .Type<ListType<StringType>>();

            // assert
            ObjectFieldDescription description = descriptor.CreateDescription();
            TypeReference typeRef = description.TypeReference;
            Assert.Equal(typeof(ListType<StringType>), typeRef.ClrType);
        }

        [Fact]
        public void ResolverTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            var descriptor = new ObjectFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"),
                typeof(ObjectField));

            // act
            ((IObjectFieldDescriptor)descriptor)
               .Name("args")
               .Type<NonNullType<ListType<NonNullType<__InputValue>>>>()
                .Resolver(c => c.Parent<ObjectField>().Arguments);

            // assert
            ObjectFieldDescription description = descriptor.CreateDescription();
            TypeReference typeRef = description.TypeReference;
            Assert.Equal(typeof(NonNullType<ListType<NonNullType<__InputValue>>>), typeRef.ClrType);
        }

        [Fact]
        public void OverwriteName()
        {
            // arrange
            var descriptor = new ObjectFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"),
                typeof(ObjectField));

            // act
            ((IObjectFieldDescriptor)descriptor)
               .Name("args");

            // assert
            Assert.Equal("args", descriptor.CreateDescription().Name);
        }

        [Fact]
        public void SetDescription()
        {
            // arrange
            string expectedDescription = Guid.NewGuid().ToString();
            var descriptor = new ObjectFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"),
                typeof(ObjectField));

            // act
            ((IObjectFieldDescriptor)descriptor)
               .Description(expectedDescription);

            // assert
            Assert.Equal(expectedDescription,
                descriptor.CreateDescription().Description);
        }

        [Fact]
        public void SetResolverAndInferTypeFromResolver()
        {
            // arrange
            var descriptor = new ObjectFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"),
                typeof(ObjectField));

            // act
            ((IObjectFieldDescriptor)descriptor)
               .Resolver(() => "ThisIsAString");

            // assert
            ObjectFieldDescription description = descriptor.CreateDescription();
            Assert.Equal(typeof(NativeType<string>),
                description.TypeReference.ClrType);
            Assert.NotNull(description.Resolver);

            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            Assert.Equal("ThisIsAString",
                description.Resolver(context.Object).Result);
        }

        [Fact]
        public void SetResolverAndInferTypeIsAlwaysRecognosedAsDotNetType()
        {
            // arrange
            var descriptor = new ObjectFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"),
                typeof(ObjectField));

            // act
            ((IObjectFieldDescriptor)descriptor)
               .Type<__Type>()
                .Resolver(ctx => ctx.Schema
                    .GetType<INamedType>(ctx.Argument<string>("type")));

            // assert
            ObjectFieldDescription description = descriptor.CreateDescription();
            Assert.Equal(typeof(__Type), description.TypeReference.ClrType);
            Assert.NotNull(description.Resolver);
        }

        [Fact]
        public void SourceTypeIsSet()
        {
            // arrange
            var descriptor = new ObjectFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"),
                typeof(ObjectField));

            // act
            ((IObjectFieldDescriptor)descriptor)
               .Name("args");

            // assert
            ObjectFieldDescription description = descriptor.CreateDescription();
            Assert.Equal(typeof(ObjectField), description.SourceType);
        }

        [Fact]
        public void ResolverTypeIsSet()
        {
            // arrange
            var descriptor = new ObjectFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"),
                typeof(ObjectField));

            // act
            descriptor.ResolverType(typeof(string));

            // assert
            ObjectFieldDescription description = descriptor.CreateDescription();
            Assert.Equal(typeof(ObjectField), description.SourceType);
            Assert.Equal(typeof(string), description.ResolverType);
        }
    }
}
