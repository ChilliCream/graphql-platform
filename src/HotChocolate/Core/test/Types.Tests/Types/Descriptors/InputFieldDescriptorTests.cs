using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public class InputFieldDescriptorTests
    : DescriptorTestBase
{
    [Fact]
    public void DotNetTypesDoNotOverwriteSchemaTypes()
    {
        // arrange
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // act
        descriptor
            .Type<ListType<StringType>>()
            .Type<NativeType<IReadOnlyDictionary<string, string>>>();

        // assert
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
        Assert.Equal(
            typeof(ListType<StringType>),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
    }

    [Fact]
    public void SchemaTypesOverwriteDotNetTypes()
    {
        // arrange
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // act
        descriptor
            .Type<NativeType<IReadOnlyDictionary<string, string>>>()
            .Type<ListType<StringType>>();

        // assert
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
        Assert.Equal(
            typeof(ListType<StringType>),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
    }

    [Fact]
    public void SetSchemaType()
    {
        // arrange
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // act
        descriptor.Type(new StringType());

        // assert
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
        Assert.IsType<StringType>(
            Assert.IsType<SchemaTypeReference>(typeRef).Type);
    }

    [Fact]
    public void SetGenericType()
    {
        // arrange
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // act
        descriptor.Type<StringType>();

        // assert
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
        Assert.Equal(
            typeof(StringType),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
    }

    [Fact]
    public void SetNonGenericType()
    {
        // arrange
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // act
        descriptor.Type(typeof(StringType));

        // assert
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
        Assert.Equal(
            typeof(StringType),
            Assert.IsType<ExtendedTypeReference>(typeRef).Type.Source);
    }

    [Fact]
    public void OverwriteName()
    {
        // arrange
        var descriptor = InputFieldDescriptor.New(
            Context,
            "field1234");

        // act
        descriptor.Name("args");

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Equal("args", description.Name);
    }

    [Fact]
    public void OverwriteName2()
    {
        // arrange
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // act
        descriptor.Name("args");

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Equal("args", description.Name);
    }

    [Fact]
    public void SetDescription()
    {
        // arrange
        var expectedDescription = Guid.NewGuid().ToString();
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // act
        descriptor.Description(expectedDescription);

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Equal(expectedDescription, description.Description);
    }

    [Fact]
    public void SetDefaultValueAndInferType()
    {
        // arrange
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // act
        descriptor.DefaultValue("string");

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Equal(
            typeof(string),
            Assert.IsType<ExtendedTypeReference>(description.Type).Type.Source);
        Assert.Equal("string", description.RuntimeDefaultValue);
    }

    [Fact]
    public void SetDefaultValueViaSyntax()
    {
        // arrange
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // act
        descriptor.DefaultValueSyntax("[]");

        // assert
        var description = descriptor.CreateDefinition();
        Assert.IsType<ListValueNode>(description.DefaultValue);
    }

    [Fact]
    public void OverwriteDefaultValueLiteralWithNativeDefaultValue()
    {
        // arrange
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // act
        descriptor
            .DefaultValue(new StringValueNode("123"))
            .DefaultValue("string");

        // asser
        var description = descriptor.CreateDefinition();
        Assert.Null(description.DefaultValue);
        Assert.Equal("string", description.RuntimeDefaultValue);
    }

    [Fact]
    public void SettingTheNativeDefaultValueToNullCreatesNullLiteral()
    {
        // arrange
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // act
        ((IInputFieldDescriptor)descriptor)
            .DefaultValue(new StringValueNode("123"))
            .DefaultValue("string")
            .DefaultValue(null);

        // assert
        var description = descriptor.CreateDefinition();
        Assert.IsType<NullValueNode>(description.DefaultValue);
        Assert.Null(description.RuntimeDefaultValue);
    }

    [Fact]
    public void OverwriteNativeDefaultValueWithDefaultValueLiteral()
    {
        // arrange
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // act
        ((IInputFieldDescriptor)descriptor)
            .DefaultValue("string")
            .DefaultValue(new StringValueNode("123"));

        // assert
        var description = descriptor.CreateDefinition();
        Assert.IsType<StringValueNode>(description.DefaultValue);
        Assert.Equal("123",
            ((StringValueNode)description.DefaultValue).Value);
        Assert.Null(description.RuntimeDefaultValue);
    }

    [Fact]
    public void InferTypeFromProperty()
    {
        // act
        var descriptor = InputFieldDescriptor.New(
            Context,
            typeof(ObjectField).GetProperty("Arguments"));

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Equal(typeof(FieldCollection<Argument>),
            Assert.IsType<ExtendedTypeReference>(description.Type).Type.Source);
        Assert.Equal("arguments", description.Name);
    }

    [Fact]
    public void Type_Syntax_Type_Null()
    {
        void Error() => InputFieldDescriptor.New(Context, "foo").Type((string)null);
        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Type_Syntax_Descriptor_Null()
    {
        void Error() => default(InputFieldDescriptor).Type("foo");
        Assert.Throws<ArgumentNullException>(Error);
    }
}
