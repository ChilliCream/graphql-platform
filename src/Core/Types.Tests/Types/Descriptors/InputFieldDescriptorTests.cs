using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Xunit;

namespace HotChocolate.Types
{
    public class InputFieldDescriptorTests
        : DescriptorTestBase
    {
        [Fact]
        public void DotNetTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            InputFieldDescriptor descriptor = InputFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            descriptor
                .Type<ListType<StringType>>()
                .Type<NativeType<IReadOnlyDictionary<string, string>>>();

            // assert
            InputFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(typeof(ListType<StringType>),
                Assert.IsType<ClrTypeReference>(typeRef).Type);
        }

        [Fact]
        public void SchemaTypesOverwriteDotNetTypes()
        {
            // arrange
            InputFieldDescriptor descriptor = InputFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            descriptor
                .Type<NativeType<IReadOnlyDictionary<string, string>>>()
                .Type<ListType<StringType>>();

            // assert
            InputFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(typeof(ListType<StringType>),
                Assert.IsType<ClrTypeReference>(typeRef).Type);
        }

        [Fact]
        public void SetSchemaType()
        {
            // arrange
            InputFieldDescriptor descriptor = InputFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            descriptor.Type(new StringType());

            // assert
            InputFieldDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.IsType<StringType>(
                Assert.IsType<SchemaTypeReference>(typeRef).Type);
        }

        [Fact]
        public void OverwriteName()
        {
            // arrange
            InputFieldDescriptor descriptor = InputFieldDescriptor.New(
                Context,
                "field1234");

            // act
            descriptor.Name("args");

            // assert
            InputFieldDefinition description = descriptor.CreateDefinition();
            Assert.Equal("args", description.Name);
        }

        [Fact]
        public void OverwriteName2()
        {
            // arrange
            InputFieldDescriptor descriptor = InputFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            descriptor.Name("args");

            // assert
            InputFieldDefinition description = descriptor.CreateDefinition();
            Assert.Equal("args", description.Name);
        }

        [Fact]
        public void SetDescription()
        {
            // arrange
            string expectedDescription = Guid.NewGuid().ToString();
            InputFieldDescriptor descriptor = InputFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            descriptor.Description(expectedDescription);

            // assert
            InputFieldDefinition description = descriptor.CreateDefinition();
            Assert.Equal(expectedDescription, description.Description);
        }

        [Fact]
        public void SetDefaultValueAndInferType()
        {
            // arrange
            InputFieldDescriptor descriptor = InputFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            descriptor.DefaultValue("string");

            // assert
            InputFieldDefinition description = descriptor.CreateDefinition();
            Assert.Equal(typeof(string),
                Assert.IsType<SchemaTypeReference>(description.Type).Type);
            Assert.Equal("string", description.NativeDefaultValue);
        }

        [Fact]
        public void OverwriteDefaultValueLiteralWithNativeDefaultValue()
        {
            // arrange
            InputFieldDescriptor descriptor = InputFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            descriptor
                .DefaultValue(new StringValueNode("123"))
                .DefaultValue("string");

            // asser
            InputFieldDefinition description = descriptor.CreateDefinition();
            Assert.Null(description.DefaultValue);
            Assert.Equal("string", description.NativeDefaultValue);
        }

        [Fact]
        public void SettingTheNativeDefaultValueToNullCreatesNullLiteral()
        {
            // arrange
            InputFieldDescriptor descriptor = InputFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)descriptor)
                .DefaultValue(new StringValueNode("123"))
                .DefaultValue("string")
                .DefaultValue(null);

            // assert
            InputFieldDefinition description = descriptor.CreateDefinition();
            Assert.IsType<NullValueNode>(description.DefaultValue);
            Assert.Null(description.NativeDefaultValue);
        }

        [Fact]
        public void OverwriteNativeDefaultValueWithDefaultValueLiteral()
        {
            // arrange
            InputFieldDescriptor descriptor = InputFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)descriptor)
                .DefaultValue("string")
                .DefaultValue(new StringValueNode("123"));

            // assert
            InputFieldDefinition description = descriptor.CreateDefinition();
            Assert.IsType<StringValueNode>(description.DefaultValue);
            Assert.Equal("123",
                ((StringValueNode)description.DefaultValue).Value);
            Assert.Null(description.NativeDefaultValue);
        }

        [Fact]
        public void InferTypeFromProperty()
        {
            // act
            InputFieldDescriptor descriptor = InputFieldDescriptor.New(
                Context,
                typeof(ObjectField).GetProperty("Arguments"));

            // assert
            InputFieldDefinition description = descriptor.CreateDefinition();
            Assert.Equal(typeof(FieldCollection<InputField>),
                Assert.IsType<IClrTypeReference>(description.Type).Type);
            Assert.Equal("arguments", description.Name);
        }
    }
}
