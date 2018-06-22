using System;
using System.Collections.Generic;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class ArgumentDescriptorTests
    {
        [Fact]
        public void DotNetTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            ArgumentDescriptor argumentDescriptor = new ArgumentDescriptor("Type");

            // act
            ((IArgumentDescriptor)argumentDescriptor)
                .Type<ListType<StringType>>()
                .Type<NativeType<IReadOnlyDictionary<string, string>>>();

            // assert
            TypeReference typeRef = argumentDescriptor.TypeReference;
            Assert.Equal(typeof(ListType<StringType>), typeRef.NativeType);
        }

        [Fact]
        public void SchemaTypesOverwriteDotNetTypes()
        {
            // arrange
            ArgumentDescriptor argumentDescriptor = new ArgumentDescriptor("Type");

            // act
            ((IArgumentDescriptor)argumentDescriptor)
                .Type<NativeType<IReadOnlyDictionary<string, string>>>()
                .Type<ListType<StringType>>();

            // assert
            TypeReference typeRef = argumentDescriptor.TypeReference;
            Assert.Equal(typeof(ListType<StringType>), typeRef.NativeType);
        }

        [Fact]
        public void GetName()
        {
            // act
            ArgumentDescriptor argumentDescriptor = new ArgumentDescriptor("args");

            // assert
            Assert.Equal("args", argumentDescriptor.Name);
        }

        [Fact]
        public void GetNameAndType()
        {
            // act
            ArgumentDescriptor argumentDescriptor =
                new ArgumentDescriptor("args", typeof(string));

            // assert
            Assert.Equal("args", argumentDescriptor.Name);
            Assert.Equal(typeof(string), argumentDescriptor.TypeReference.NativeType);
        }

        [Fact]
        public void SetDescription()
        {
            // arrange
            string expectedDescription = Guid.NewGuid().ToString();
            ArgumentDescriptor argumentDescriptor = new ArgumentDescriptor("Type");

            // act
            ((IArgumentDescriptor)argumentDescriptor)
                .Description(expectedDescription);

            // assert
            Assert.Equal(expectedDescription, argumentDescriptor.Description);
        }

        [Fact]
        public void SetDefaultValueAndInferType()
        {
            // arrange
            ArgumentDescriptor argumentDescriptor =
                new ArgumentDescriptor("args");

            // act
            ((IArgumentDescriptor)argumentDescriptor)
                .DefaultValue("string");

            // assert
            Assert.Equal(typeof(string), argumentDescriptor.TypeReference.NativeType);
            Assert.Equal("string", argumentDescriptor.NativeDefaultValue);
        }

        [Fact]
        public void OverwriteDefaultValueLiteralWithNativeDefaultValue()
        {
            // arrange
            ArgumentDescriptor argumentDescriptor =
                new ArgumentDescriptor("args");

            // act
            ((IArgumentDescriptor)argumentDescriptor)
                .DefaultValue(new StringValueNode("123"))
                .DefaultValue("string");

            // assert
            Assert.Null(argumentDescriptor.DefaultValue);
            Assert.Equal("string", argumentDescriptor.NativeDefaultValue);
        }

        [Fact]
        public void SettingTheNativeDefaultValueToNullCreatesNullLiteral()
        {
            // arrange
            ArgumentDescriptor argumentDescriptor =
                new ArgumentDescriptor("args");

            // act
            ((IArgumentDescriptor)argumentDescriptor)
                .DefaultValue(new StringValueNode("123"))
                .DefaultValue("string")
                .DefaultValue(null);

            // assert
            Assert.IsType<NullValueNode>(argumentDescriptor.DefaultValue);
            Assert.Null(argumentDescriptor.NativeDefaultValue);
        }

        [Fact]
        public void OverwriteNativeDefaultValueWithDefaultValueLiteral()
        {
            // arrange
            ArgumentDescriptor argumentDescriptor =
                new ArgumentDescriptor("args");

            // act
            ((IArgumentDescriptor)argumentDescriptor)
                .DefaultValue("string")
                .DefaultValue(new StringValueNode("123"));

            // assert
            Assert.IsType<StringValueNode>(argumentDescriptor.DefaultValue);
            Assert.Equal("123", ((StringValueNode)argumentDescriptor.DefaultValue).Value);
            Assert.Null(argumentDescriptor.NativeDefaultValue);
        }
    }
}
