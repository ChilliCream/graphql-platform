using System;
using System.Collections.Generic;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class ArgumentDescriptorTests
    {
        [Fact]
        public void Create_TypeIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => new ArgumentDescriptor("Type", null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void DotNetTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            var descriptor = new ArgumentDescriptor("Type");

            // act
            ((IArgumentDescriptor)descriptor)
                .Type<ListType<StringType>>()
                .Type<NativeType<IReadOnlyDictionary<string, string>>>();

            // assert
            ArgumentDescription description = descriptor.CreateDescription();
            TypeReference typeRef = description.Type;
            Assert.Equal(typeof(ListType<StringType>), typeRef.ClrType);
        }

        [Fact]
        public void SetTypeInstance()
        {
            // arrange
            var descriptor = new ArgumentDescriptor("Type");

            // act
            ((IArgumentDescriptor)descriptor).Type(new StringType());

            // assert
            ArgumentDescription description = descriptor.CreateDescription();
            TypeReference typeRef = description.Type;
            Assert.IsType<StringType>(typeRef.SchemaType);
        }

        [Fact]
        public void SchemaTypesOverwriteDotNetTypes()
        {
            // arrange
            var descriptor = new ArgumentDescriptor("Type");

            // act
            ((IArgumentDescriptor)descriptor)
                .Type<NativeType<IReadOnlyDictionary<string, string>>>()
                .Type<ListType<StringType>>();

            // assert
            ArgumentDescription description = descriptor.CreateDescription();
            TypeReference typeRef = description.Type;
            Assert.Equal(typeof(ListType<StringType>), typeRef.ClrType);
        }

        [Fact]
        public void GetName()
        {
            // act
            var descriptor = new ArgumentDescriptor("args");

            // assert
            Assert.Equal("args", descriptor.CreateDescription().Name);
        }

        [Fact]
        public void GetNameAndType()
        {
            // act
            var descriptor = new ArgumentDescriptor("args", typeof(string));

            // assert
            ArgumentDescription description = descriptor.CreateDescription();
            Assert.Equal("args", description.Name);
            Assert.Equal(typeof(string), description.Type.ClrType);
        }

        [Fact]
        public void SetDescription()
        {
            // arrange
            string expectedDescription = Guid.NewGuid().ToString();
            var descriptor = new ArgumentDescriptor("Type");

            // act
            ((IArgumentDescriptor)descriptor)
                .Description(expectedDescription);

            // assert
            Assert.Equal(expectedDescription,
                descriptor.CreateDescription().Description);
        }

        [Fact]
        public void SetDefaultValueAndInferType()
        {
            // arrange
            var descriptor = new ArgumentDescriptor("args");

            // act
            ((IArgumentDescriptor)descriptor).DefaultValue("string");

            // assert
            ArgumentDescription description = descriptor.CreateDescription();
            Assert.Equal(typeof(string),
                description.Type.ClrType);
            Assert.Equal("string",
                description.NativeDefaultValue);
        }

        [Fact]
        public void SetDefaultValueNull()
        {
            // arrange
            var descriptor = new ArgumentDescriptor("args");

            // act
            ((IArgumentDescriptor)descriptor).DefaultValue(null);

            // assert
            ArgumentDescription description = descriptor.CreateDescription();
            Assert.Equal(NullValueNode.Default, description.DefaultValue);
            Assert.Null(description.NativeDefaultValue);
        }

        [Fact]
        public void OverwriteDefaultValueLiteralWithNativeDefaultValue()
        {
            // arrange
            var descriptor = new ArgumentDescriptor("args");

            // act
            ((IArgumentDescriptor)descriptor)
                .DefaultValue(new StringValueNode("123"))
                .DefaultValue("string");

            // assert
            ArgumentDescription description = descriptor.CreateDescription();
            Assert.Null(description.DefaultValue);
            Assert.Equal("string", description.NativeDefaultValue);
        }

        [Fact]
        public void SettingTheNativeDefaultValueToNullCreatesNullLiteral()
        {
            // arrange
            var descriptor = new ArgumentDescriptor("args");

            // act
            ((IArgumentDescriptor)descriptor)
                .DefaultValue(new StringValueNode("123"))
                .DefaultValue("string")
                .DefaultValue(null);

            // assert
            ArgumentDescription description = descriptor.CreateDescription();
            Assert.IsType<NullValueNode>(description.DefaultValue);
            Assert.Null(description.NativeDefaultValue);
        }

        [Fact]
        public void OverwriteNativeDefaultValueWithDefaultValueLiteral()
        {
            // arrange
            var descriptor = new ArgumentDescriptor("args");

            // act
            ((IArgumentDescriptor)descriptor)
                .DefaultValue("string")
                .DefaultValue(new StringValueNode("123"));

            // assert
            ArgumentDescription description = descriptor.CreateDescription();
            Assert.IsType<StringValueNode>(description.DefaultValue);
            Assert.Equal("123", ((StringValueNode)description.DefaultValue).Value);
            Assert.Null(description.NativeDefaultValue);
        }
    }
}
