using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public class ArgumentDescriptorTests
    : DescriptorTestBase
{
    [Fact]
    public void Create_TypeIsNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action() => new ArgumentDescriptor(Context, "Type", null);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
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
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
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
        var description = descriptor.CreateDefinition();
        var typeRef = description.Type;
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
        var descriptor = new ArgumentDescriptor(Context, "Type");

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
    public void SchemaTypesOverwriteDotNetTypes()
    {
        // arrange
        var descriptor = new ArgumentDescriptor(Context, "Type");

        // act
        ((IArgumentDescriptor)descriptor)
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
        var description = descriptor.CreateDefinition();
        Assert.Equal("args", description.Name);
        Assert.Equal(typeof(string),
            Assert.IsType<ExtendedTypeReference>(description.Type).Type.Source);
    }

    [Fact]
    public void SetDescription()
    {
        // arrange
        var expectedDescription = Guid.NewGuid().ToString();
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
        var description = descriptor.CreateDefinition();
        Assert.Equal(typeof(string),
            Assert.IsType<ExtendedTypeReference>(description.Type).Type.Source);
        Assert.Equal("string",
            description.RuntimeDefaultValue);
    }

    [Fact]
    public void SetDefaultValueViaSyntax()
    {
        // arrange
        var descriptor = new ArgumentDescriptor(Context, "args");

        // act
        descriptor.DefaultValueSyntax("[]");

        // assert
        var description = descriptor.CreateDefinition();
        Assert.IsType<ListValueNode>(description.DefaultValue);
    }

    [Fact]
    public void SetDefaultValueNull()
    {
        // arrange
        var descriptor = new ArgumentDescriptor(Context, "args");

        // act
        descriptor.DefaultValue(null);

        // assert
        var description = descriptor.CreateDefinition();
        Assert.Equal(NullValueNode.Default, description.DefaultValue);
        Assert.Null(description.RuntimeDefaultValue);
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
        var description = descriptor.CreateDefinition();
        Assert.Null(description.DefaultValue);
        Assert.Equal("string", description.RuntimeDefaultValue);
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
        var description = descriptor.CreateDefinition();
        Assert.IsType<NullValueNode>(description.DefaultValue);
        Assert.Null(description.RuntimeDefaultValue);
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
        var description = descriptor.CreateDefinition();
        Assert.IsType<StringValueNode>(description.DefaultValue);
        Assert.Equal("123", ((StringValueNode)description.DefaultValue).Value);
        Assert.Null(description.RuntimeDefaultValue);
    }

    [Fact]
    public void Type_Syntax_Type_Null()
    {
        void Error() => ArgumentDescriptor.New(Context, "foo").Type((string)null);
        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Type_Syntax_Descriptor_Null()
    {
        void Error() => default(ArgumentDescriptor).Type("foo");
        Assert.Throws<ArgumentNullException>(Error);
    }
}
