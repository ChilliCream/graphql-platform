using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using Moq;
using Xunit;

namespace HotChocolate.Types
{
    public class ObjectFieldDescriptorTests
        : DescriptorTestBase
    {
        [Fact]
        public void SetGenericType()
        {
            // arrange
            var descriptor =
              ObjectFieldDescriptor.New(Context, "field");

            // act
            descriptor.Type<StringType>();

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(
                typeof(StringType),
                Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
        }

        [Fact]
        public void SetNonGenericType()
        {
            // arrange
            var descriptor =
              ObjectFieldDescriptor.New(Context, "field");

            // act
            descriptor.Type(typeof(StringType));

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(
                typeof(StringType),
                Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
        }

        [Fact]
        public void DotNetTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            var descriptor =
                ObjectFieldDescriptor.New(Context, "field");

            // act
            descriptor
                .Type<ListType<StringType>>()
                .Type<NativeType<IReadOnlyDictionary<string, string>>>();

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(typeof(ListType<StringType>),
                Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
        }

        [Fact]
        public void SchemaTypesOverwriteDotNetTypes()
        {
            // arrange
            var descriptor =
                ObjectFieldDescriptor.New(Context, "field");

            // act
            descriptor
                .Type<NativeType<IReadOnlyDictionary<string, string>>>()
                .Type<ListType<StringType>>();

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(typeof(ListType<StringType>),
                Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
        }

        [Fact]
        public void ResolverTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            var descriptor = ObjectFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"),
                typeof(ObjectField));

            // act
            descriptor
               .Name("args")
               .Type<NonNullType<ListType<NonNullType<__InputValue>>>>()
                .Resolver(c => c.Parent<ObjectField>().Arguments);

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(
                typeof(NonNullType<ListType<NonNullType<__InputValue>>>),
                Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
        }

        [Fact]
        public void OverwriteName()
        {
            // arrange
            var descriptor = ObjectFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"),
                typeof(ObjectField));

            // act
            descriptor.Name("args");

            // assert
            Assert.Equal("args", descriptor.CreateDefinition().Name);
        }

        [Fact]
        public void SetDescription()
        {
            // arrange
            string expectedDescription = Guid.NewGuid().ToString();
            var descriptor = ObjectFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"),
                typeof(ObjectField));

            // act
            descriptor.Description(expectedDescription);

            // assert
            Assert.Equal(expectedDescription,
                descriptor.CreateDefinition().Description);
        }

        [Fact]
        public void SetResolverAndInferTypeFromResolver()
        {
            // arrange
            var descriptor =
                ObjectFieldDescriptor.New(
                    Context,
                    typeof(ObjectField).GetProperty("Arguments"),
                    typeof(ObjectField));

            // act
            descriptor.Resolver(() => "ThisIsAString");

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(
                typeof(string),
                Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);

            Assert.NotNull(description.Resolver);

            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            Assert.Equal("ThisIsAString",
                description.Resolver(context.Object).Result);
        }

        [Fact]
        public void SetResolverAndInferTypeIsAlwaysRecognisedAsDotNetType()
        {
            // arrange
            var descriptor =
                ObjectFieldDescriptor.New(
                    Context,
                    typeof(ObjectField).GetProperty("Arguments"),
                    typeof(ObjectField));

            // act
            descriptor
                .Type<__Type>()
                .Resolver(ctx => ctx.Schema
                    .GetType<INamedType>(ctx.ArgumentValue<string>("type")));

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(
                typeof(__Type),
                Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
            Assert.NotNull(description.Resolver);
        }

        [Fact]
        public void ResolverTypeIsSet()
        {
            // arrange
            // act
            var descriptor =
                ObjectFieldDescriptor.New(
                    Context,
                    typeof(ObjectField).GetProperty("Arguments"),
                    typeof(ObjectField),
                    typeof(string));

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            Assert.Equal(typeof(string), description.ResolverType);
        }
    }
}
