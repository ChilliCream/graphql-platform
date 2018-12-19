using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class InputFieldDescriptorTests
    {
        [Fact]
        public void DotNetTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            var descriptor = new InputFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)descriptor)
                .Type<ListType<StringType>>()
                .Type<NativeType<IReadOnlyDictionary<string, string>>>();

            // assert
            InputFieldDescription description = descriptor.CreateDescription();
            TypeReference typeRef = description.TypeReference;
            Assert.Equal(typeof(ListType<StringType>), typeRef.ClrType);
        }

        [Fact]
        public void SchemaTypesOverwriteDotNetTypes()
        {
            // arrange
            var descriptor = new InputFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)descriptor)
                .Type<NativeType<IReadOnlyDictionary<string, string>>>()
                .Type<ListType<StringType>>();

            // assert
            InputFieldDescription description = descriptor.CreateDescription();
            TypeReference typeRef = description.TypeReference;
            Assert.Equal(typeof(ListType<StringType>), typeRef.ClrType);
        }

        [Fact]
        public void OverwriteName()
        {
            // arrange
            var descriptor = new InputFieldDescriptor("field1234");

            // act
            ((IInputFieldDescriptor)descriptor)
                .Name("args");

            // assert
            InputFieldDescription description = descriptor.CreateDescription();
            Assert.Equal("args", description.Name);
        }

        [Fact]
        public void OverwriteName2()
        {
            // arrange
            var descriptor = new InputFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)descriptor)
                .Name("args");

            // assert
            InputFieldDescription description = descriptor.CreateDescription();
            Assert.Equal("args", description.Name);
        }

        [Fact]
        public void SetDescription()
        {
            // arrange
            string expectedDescription = Guid.NewGuid().ToString();
            var descriptor = new InputFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)descriptor)
                .Description(expectedDescription);

            // assert
            InputFieldDescription description = descriptor.CreateDescription();
            Assert.Equal(expectedDescription, description.Description);
        }

        [Fact]
        public void SetDefaultValueAndInferType()
        {
            // arrange
            var descriptor = new InputFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)descriptor)
                .DefaultValue("string");

            // assert
            InputFieldDescription description = descriptor.CreateDescription();
            Assert.Equal(typeof(string), description.TypeReference.ClrType);
            Assert.Equal("string", description.NativeDefaultValue);
        }

        [Fact]
        public void OverwriteDefaultValueLiteralWithNativeDefaultValue()
        {
            // arrange
            var descriptor = new InputFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)descriptor)
                .DefaultValue(new StringValueNode("123"))
                .DefaultValue("string");

            // asser
            InputFieldDescription description = descriptor.CreateDescription();
            Assert.Null(description.DefaultValue);
            Assert.Equal("string", description.NativeDefaultValue);
        }

        [Fact]
        public void SettingTheNativeDefaultValueToNullCreatesNullLiteral()
        {
            // arrange
            var descriptor = new InputFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)descriptor)
                .DefaultValue(new StringValueNode("123"))
                .DefaultValue("string")
                .DefaultValue(null);

            // assert
            InputFieldDescription description = descriptor.CreateDescription();
            Assert.IsType<NullValueNode>(description.DefaultValue);
            Assert.Null(description.NativeDefaultValue);
        }

        [Fact]
        public void OverwriteNativeDefaultValueWithDefaultValueLiteral()
        {
            // arrange
            var descriptor = new InputFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)descriptor)
                .DefaultValue("string")
                .DefaultValue(new StringValueNode("123"));

            // assert
            InputFieldDescription description = descriptor.CreateDescription();
            Assert.IsType<StringValueNode>(description.DefaultValue);
            Assert.Equal("123",
                ((StringValueNode)description.DefaultValue).Value);
            Assert.Null(description.NativeDefaultValue);
        }

        [Fact]
        public void InferTypeFromProperty()
        {
            // act
            var descriptor = new InputFieldDescriptor(
                typeof(ObjectField).GetProperty("Arguments"));

            // assert
            InputFieldDescription description = descriptor.CreateDescription();
            Assert.Equal(typeof(FieldCollection<InputField>),
                description.TypeReference.ClrType);
            Assert.Equal("arguments", description.Name);
        }
    }
}
