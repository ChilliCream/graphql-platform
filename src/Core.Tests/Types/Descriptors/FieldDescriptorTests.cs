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
    public class FieldDescriptorTests
    {
        [Fact]
        public void DotNetTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            ObjectFieldDescriptor fieldDescriptor = new ObjectFieldDescriptor("Type", "field");

            // act
            ((IObjectFieldDescriptor)fieldDescriptor)
                .Type<ListType<StringType>>()
                .Type<NativeType<IReadOnlyDictionary<string, string>>>();

            // assert
            TypeReference typeRef = fieldDescriptor.TypeReference;
            Assert.Equal(typeof(ListType<StringType>), typeRef.NativeType);
        }

        [Fact]
        public void SchemaTypesOverwriteDotNetTypes()
        {
            // arrange
            ObjectFieldDescriptor fieldDescriptor = new ObjectFieldDescriptor("Type", "field");

            // act
            ((IObjectFieldDescriptor)fieldDescriptor)
                .Type<NativeType<IReadOnlyDictionary<string, string>>>()
                .Type<ListType<StringType>>();

            // assert
            TypeReference typeRef = fieldDescriptor.TypeReference;
            Assert.Equal(typeof(ListType<StringType>), typeRef.NativeType);
        }

        [Fact]
        public void ResolverTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            ObjectFieldDescriptor fieldDescriptor = new ObjectFieldDescriptor(
                "Field", typeof(Field).GetProperty("Arguments"),
                typeof(IReadOnlyDictionary<string, InputField>));

            // act
            ((IObjectFieldDescriptor)fieldDescriptor)
               .Name("args")
               .Type<NonNullType<ListType<NonNullType<__InputValue>>>>()
               .Resolver(c => c.Parent<Field>().Arguments.Values);

            // assert
            TypeReference typeRef = fieldDescriptor.TypeReference;
            Assert.Equal(typeof(NonNullType<ListType<NonNullType<__InputValue>>>), typeRef.NativeType);
        }

        [Fact]
        public void OverwriteName()
        {
            // arrange
            ObjectFieldDescriptor fieldDescriptor = new ObjectFieldDescriptor(
                "Field", typeof(Field).GetProperty("Arguments"),
                typeof(IReadOnlyDictionary<string, InputField>));

            // act
            ((IObjectFieldDescriptor)fieldDescriptor)
               .Name("args");

            // assert
            Assert.Equal("args", fieldDescriptor.Name);
        }

        [Fact]
        public void SetDescription()
        {
            // arrange
            string expectedDescription = Guid.NewGuid().ToString();
            ObjectFieldDescriptor fieldDescriptor = new ObjectFieldDescriptor(
                "Field", typeof(Field).GetProperty("Arguments"),
                typeof(IReadOnlyDictionary<string, InputField>));

            // act
            ((IObjectFieldDescriptor)fieldDescriptor)
               .Description(expectedDescription);

            // assert
            Assert.Equal(expectedDescription, fieldDescriptor.Description);
        }

        [Fact]
        public void SetResolverAndInferTypeFromResolver()
        {
            // arrange
            ObjectFieldDescriptor fieldDescriptor = new ObjectFieldDescriptor(
                "Field", typeof(Field).GetProperty("Arguments"),
                typeof(IReadOnlyDictionary<string, InputField>));

            // act
            ((IObjectFieldDescriptor)fieldDescriptor)
               .Resolver(() => "ThisIsAString");

            // assert
            Assert.Equal(typeof(NativeType<string>), fieldDescriptor.TypeReference.NativeType);
            Assert.NotNull(fieldDescriptor.Resolver);

            Mock<IResolverContext> context = new Mock<IResolverContext>(MockBehavior.Strict);
            Assert.Equal("ThisIsAString", fieldDescriptor.Resolver(context.Object, default));
        }

        [Fact]
        public void SetResolverAndInferTypeIsAlwaysRecognosedAsDotNetType()
        {
            // arrange
            ObjectFieldDescriptor fieldDescriptor = new ObjectFieldDescriptor(
                "Field", typeof(Field).GetProperty("Arguments"),
                typeof(IReadOnlyDictionary<string, InputField>));

            // act
            ((IObjectFieldDescriptor)fieldDescriptor)
               .Type<__Type>()
                .Resolver(ctx => ctx.Schema
                    .GetType<INamedType>(ctx.Argument<string>("type")));

            // assert
            Assert.Equal(typeof(__Type), fieldDescriptor.TypeReference.NativeType);
            Assert.NotNull(fieldDescriptor.Resolver);
        }
    }
}
