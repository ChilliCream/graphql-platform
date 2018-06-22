using System;
using System.Collections.Generic;
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
            InputFieldDescriptor inputFieldDescriptor = new InputFieldDescriptor(
                typeof(Field).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)inputFieldDescriptor)
                .Type<ListType<StringType>>()
                .Type<NativeType<IReadOnlyDictionary<string, string>>>();

            // assert
            TypeReference typeRef = inputFieldDescriptor.TypeReference;
            Assert.Equal(typeof(ListType<StringType>), typeRef.NativeType);
        }

        [Fact]
        public void SchemaTypesOverwriteDotNetTypes()
        {
            // arrange
            InputFieldDescriptor inputFieldDescriptor = new InputFieldDescriptor(
                typeof(Field).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)inputFieldDescriptor)
                .Type<NativeType<IReadOnlyDictionary<string, string>>>()
                .Type<ListType<StringType>>();

            // assert
            TypeReference typeRef = inputFieldDescriptor.TypeReference;
            Assert.Equal(typeof(ListType<StringType>), typeRef.NativeType);
        }

        [Fact]
        public void OverwriteName()
        {
            // arrange
            InputFieldDescriptor inputFieldDescriptor = new InputFieldDescriptor("1234");

            // act
            ((IInputFieldDescriptor)inputFieldDescriptor)
                .Name("args");

            // assert
            Assert.Equal("args", inputFieldDescriptor.Name);
        }

        [Fact]
        public void OverwriteName2()
        {
            // arrange
            InputFieldDescriptor inputFieldDescriptor = new InputFieldDescriptor(
                typeof(Field).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)inputFieldDescriptor)
                .Name("args");

            // assert
            Assert.Equal("args", inputFieldDescriptor.Name);
        }

        [Fact]
        public void SetDescription()
        {
            // arrange
            string expectedDescription = Guid.NewGuid().ToString();
            InputFieldDescriptor inputFieldDescriptor = new InputFieldDescriptor(
                typeof(Field).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)inputFieldDescriptor)
                .Description(expectedDescription);

            // assert
            Assert.Equal(expectedDescription, inputFieldDescriptor.Description);
        }

        [Fact]
        public void SetDefaultValueAndInferType()
        {
            // arrange
            InputFieldDescriptor inputFieldDescriptor = new InputFieldDescriptor(
                typeof(Field).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)inputFieldDescriptor)
                .DefaultValue("string");

            // assert
            Assert.Equal(typeof(string), inputFieldDescriptor.TypeReference.NativeType);
            Assert.Equal("string", inputFieldDescriptor.NativeDefaultValue);
        }

        [Fact]
        public void OverwriteDefaultValueLiteralWithNativeDefaultValue()
        {
            // arrange
            InputFieldDescriptor inputFieldDescriptor = new InputFieldDescriptor(
                typeof(Field).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)inputFieldDescriptor)
                .DefaultValue(new StringValueNode("123"))
                .DefaultValue("string");

            // assert
            Assert.Null(inputFieldDescriptor.DefaultValue);
            Assert.Equal("string", inputFieldDescriptor.NativeDefaultValue);
        }

        [Fact]
        public void SettingTheNativeDefaultValueToNullCreatesNullLiteral()
        {
            // arrange
            InputFieldDescriptor inputFieldDescriptor = new InputFieldDescriptor(
                typeof(Field).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)inputFieldDescriptor)
                .DefaultValue(new StringValueNode("123"))
                .DefaultValue("string")
                .DefaultValue(null);

            // assert
            Assert.IsType<NullValueNode>(inputFieldDescriptor.DefaultValue);
            Assert.Null(inputFieldDescriptor.NativeDefaultValue);
        }

        [Fact]
        public void OverwriteNativeDefaultValueWithDefaultValueLiteral()
        {
            // arrange
            InputFieldDescriptor inputFieldDescriptor = new InputFieldDescriptor(
                typeof(Field).GetProperty("Arguments"));

            // act
            ((IInputFieldDescriptor)inputFieldDescriptor)
                .DefaultValue("string")
                .DefaultValue(new StringValueNode("123"));

            // assert
            Assert.IsType<StringValueNode>(inputFieldDescriptor.DefaultValue);
            Assert.Equal("123", ((StringValueNode)inputFieldDescriptor.DefaultValue).Value);
            Assert.Null(inputFieldDescriptor.NativeDefaultValue);
        }

        [Fact]
        public void InferTypeFromProperty()
        {
            // act
            InputFieldDescriptor inputFieldDescriptor = new InputFieldDescriptor(
                typeof(Field).GetProperty("Arguments"));

            // assert
            Assert.Equal(typeof(IReadOnlyDictionary<string, InputField>),
                inputFieldDescriptor.TypeReference.NativeType);
            Assert.Equal("arguments", inputFieldDescriptor.Name);
        }
    }
}
