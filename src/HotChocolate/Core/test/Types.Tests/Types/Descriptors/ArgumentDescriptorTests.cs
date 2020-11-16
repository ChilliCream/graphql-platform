using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Xunit;

namespace HotChocolate.Types
{
    public class ArgumentDescriptorTests
        : DescriptorTestBase
    {
        [Fact]
        public void Create_TypeIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => new ArgumentDescriptor(
                Context, "Type", null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void DotNetTypesDoNotOverwriteSchemaTypes()
        {
            // arrange
            var descriptor = new ArgumentDescriptor(Context, "Type");

            // act
            descriptor
                .Type<ListType<StringType>>()
                .Type<NativeType<IReadOnlyDictionary<string, string>>>();

            // assert
            ArgumentDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(typeof(ListType<StringType>),
                Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
        }

        [Fact]
        public void SetTypeInstance()
        {
            // arrange
            var descriptor = new ArgumentDescriptor(Context, "Type");

            // act
            descriptor.Type(new StringType());

            // assert
            ArgumentDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.IsType<StringType>(
                Assert.IsType<SchemaTypeReference>(typeRef).Type);
        }

        [Fact]
        public void SetGenericType()
        {
            // arrange
            var descriptor = new ArgumentDescriptor(Context, "Type");

            // act
            descriptor.Type<StringType>();

            // assert
            ArgumentDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(
                typeof(StringType),
                Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
        }

        [Fact]
        public void SetNonGenericType()
        {
            // arrange
            var descriptor = new ArgumentDescriptor(Context, "Type");

            // act
            descriptor.Type(typeof(StringType));

            // assert
            ArgumentDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(
                typeof(StringType),
                Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
        }

        [Fact]
        public void SchemaTypesOverwriteDotNetTypes()
        {
            // arrange
            var descriptor = new ArgumentDescriptor(Context, "Type");

            // act
            ((IArgumentDescriptor)descriptor)
                .Type<NativeType<IReadOnlyDictionary<string, string>>>()
                .Type<ListType<StringType>>();

            // assert
            ArgumentDefinition description = descriptor.CreateDefinition();
            ITypeReference typeRef = description.Type;
            Assert.Equal(
                typeof(ListType<StringType>),
                Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
        }

        [Fact]
        public void GetName()
        {
            // act
            var descriptor = new ArgumentDescriptor(Context, "args");

            // assert
            Assert.Equal("args", descriptor.CreateDefinition().Name);
        }

        [Fact]
        public void GetNameAndType()
        {
            // act
            var descriptor = new ArgumentDescriptor(
                Context, "args", typeof(string));

            // assert
            ArgumentDefinition description = descriptor.CreateDefinition();
            Assert.Equal("args", description.Name);
            Assert.Equal(typeof(string),
                Assert.IsType<ExtendedTypeReference>(description.Type).Type.Source);
        }

        [Fact]
        public void SetDescription()
        {
            // arrange
            string expectedDescription = Guid.NewGuid().ToString();
            var descriptor = new ArgumentDescriptor(Context, "Type");

            // act
            descriptor.Description(expectedDescription);

            // assert
            Assert.Equal(expectedDescription,
                descriptor.CreateDefinition().Description);
        }

        [Fact]
        public void SetDefaultValueAndInferType()
        {
            // arrange
            var descriptor = new ArgumentDescriptor(Context, "args");

            // act
            descriptor.DefaultValue("string");

            // assert
            ArgumentDefinition description = descriptor.CreateDefinition();
            Assert.Equal(typeof(string),
                Assert.IsType<ExtendedTypeReference>(description.Type).Type.Source);
            Assert.Equal("string",
                description.NativeDefaultValue);
        }

        [Fact]
        public void SetDefaultValueNull()
        {
            // arrange
            var descriptor = new ArgumentDescriptor(Context, "args");

            // act
            descriptor.DefaultValue(null);

            // assert
            ArgumentDefinition description = descriptor.CreateDefinition();
            Assert.Equal(NullValueNode.Default, description.DefaultValue);
            Assert.Null(description.NativeDefaultValue);
        }

        [Fact]
        public void OverwriteDefaultValueLiteralWithNativeDefaultValue()
        {
            // arrange
            var descriptor = new ArgumentDescriptor(Context, "args");

            // act
            descriptor
                .DefaultValue(new StringValueNode("123"))
                .DefaultValue("string");

            // assert
            ArgumentDefinition description = descriptor.CreateDefinition();
            Assert.Null(description.DefaultValue);
            Assert.Equal("string", description.NativeDefaultValue);
        }

        [Fact]
        public void SettingTheNativeDefaultValueToNullCreatesNullLiteral()
        {
            // arrange
            var descriptor = new ArgumentDescriptor(Context, "args");

            // act
            descriptor
                .DefaultValue(new StringValueNode("123"))
                .DefaultValue("string")
                .DefaultValue(null);

            // assert
            ArgumentDefinition description = descriptor.CreateDefinition();
            Assert.IsType<NullValueNode>(description.DefaultValue);
            Assert.Null(description.NativeDefaultValue);
        }

        [Fact]
        public void OverwriteNativeDefaultValueWithDefaultValueLiteral()
        {
            // arrange
            var descriptor = new ArgumentDescriptor(Context, "args");

            // act
            descriptor
                .DefaultValue("string")
                .DefaultValue(new StringValueNode("123"));

            // assert
            ArgumentDefinition description = descriptor.CreateDefinition();
            Assert.IsType<StringValueNode>(description.DefaultValue);
            Assert.Equal("123", ((StringValueNode)description.DefaultValue).Value);
            Assert.Null(description.NativeDefaultValue);
        }
    }
}
