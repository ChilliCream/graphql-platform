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
        public void DotNetTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            ObjectFieldDescriptor descriptor =
                ObjectFieldDescriptor.New(Context, "field");

            // act
            descriptor
                .Type<ListType<StringType>>()
                .Type<NativeType<IReadOnlyDictionary<string, string>>>();

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(typeof(ListType<StringType>),
                Assert.IsType<ClrTypeReference>(typeRef).Type);
        }

        [Fact]
        public void SchemaTypesOverwriteDotNetTypes()
        {
            // arrange
            ObjectFieldDescriptor descriptor =
                ObjectFieldDescriptor.New(Context, "field");

            // act
            descriptor
                .Type<NativeType<IReadOnlyDictionary<string, string>>>()
                .Type<ListType<StringType>>();

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(typeof(ListType<StringType>),
                Assert.IsType<ClrTypeReference>(typeRef).Type);
        }

        [Fact]
        public void ResolverTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            ObjectFieldDescriptor descriptor = ObjectFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

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
                Assert.IsType<ClrTypeReference>(typeRef).Type);
        }

        [Fact]
        public void OverwriteName()
        {
            // arrange
            ObjectFieldDescriptor descriptor = ObjectFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

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
            ObjectFieldDescriptor descriptor = ObjectFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

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
            ObjectFieldDescriptor descriptor =
                ObjectFieldDescriptor.New(
                    Context,
                    typeof(ObjectField).GetProperty("Arguments"));

            // act
            descriptor.Resolver(() => "ThisIsAString");

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(
                typeof(NativeType<string>),
                Assert.IsType<ClrTypeReference>(typeRef).Type);

            Assert.NotNull(description.Resolver);

            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            Assert.Equal("ThisIsAString",
                description.Resolver(context.Object).Result);
        }

        [Fact]
        public void SetResolverAndInferTypeIsAlwaysRecognisedAsDotNetType()
        {
            // arrange
            ObjectFieldDescriptor descriptor =
                ObjectFieldDescriptor.New(
                    Context,
                    typeof(ObjectField).GetProperty("Arguments"));

            // act
            descriptor
                .Type<__Type>()
                .Resolver(ctx => ctx.Schema
                    .GetType<INamedType>(ctx.Argument<string>("type")));

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(
                typeof(__Type),
                Assert.IsType<ClrTypeReference>(typeRef).Type);
            Assert.NotNull(description.Resolver);
        }

        [Fact]
        public void ResolverTypeIsSet()
        {
            // arrange
            // act
            ObjectFieldDescriptor descriptor =
                ObjectFieldDescriptor.New(
                    Context,
                    typeof(ObjectField).GetProperty("Arguments"),
                    typeof(string));

            // assert
            ObjectFieldDefinition description = descriptor.CreateDefinition();
            Assert.Equal(typeof(string), description.ResolverType);
        }
    }
}
