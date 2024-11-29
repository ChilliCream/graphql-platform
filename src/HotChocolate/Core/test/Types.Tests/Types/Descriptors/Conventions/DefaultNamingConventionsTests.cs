// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors;

public class DefaultNamingConventionsTests
{
    [InlineData("f", "f")]
    [InlineData("IOFile", "ioFile")]
    [InlineData("FooBar", "fooBar")]
    [InlineData("FOO1Bar", "foo1Bar")]
    [InlineData("FOO_Bar", "foo_Bar")]
    [InlineData("FOO", "foo")]
    [InlineData("FOo", "fOo")]
    [InlineData("FOOBarBaz", "fooBarBaz")]
    [InlineData("FoO", "foO")]
    [InlineData("F","f")]
    [Theory]
    public void GetFormattedFieldName_ReturnsFormattedFieldName(
            string fieldName,
            string expected) {
        // arrange
        var namingConventions = new DefaultNamingConventions(
            new XmlDocumentationProvider(
                new XmlDocumentationFileResolver(),
                new NoOpStringBuilderPool()));

        // act
        var formattedFieldName = namingConventions.FormatFieldName(fieldName);

        // assert
        Assert.Equal(expected, formattedFieldName);
    }

    [InlineData("Foo", "FOO")]
    [InlineData("FooBar", "FOO_BAR")]
    [InlineData("FooBarBaz", "FOO_BAR_BAZ")]
    [InlineData("StringGUID", "STRING_GUID")]
    [InlineData("IPAddress", "IP_ADDRESS")]
    [InlineData("FOO_BAR_BAZ", "FOO_BAR_BAZ")]
    [InlineData("FOOBAR", "FOOBAR")]
    [InlineData("F", "F")]
    [InlineData("f", "F")]
    [Theory]
    public void GetEnumName(string runtimeName, string expectedSchemaName)
    {
        // arrange
        var namingConventions = new DefaultNamingConventions(
            new XmlDocumentationProvider(
                new XmlDocumentationFileResolver(),
                new NoOpStringBuilderPool()));

        // act
        var schemaName = namingConventions.GetEnumValueName(runtimeName);

        // assert
        Assert.Equal(expectedSchemaName, schemaName);
    }

    [InlineData(true)]
    [InlineData(1)]
    [InlineData("abc")]
    [InlineData(Foo.Bar)]
    [Theory]
    public void GetEnumValueDescription_NoDescription(object value)
    {
        // arrange
        var namingConventions = new DefaultNamingConventions(
            new XmlDocumentationProvider(
                new XmlDocumentationFileResolver(),
                new NoOpStringBuilderPool()));

        // act
        var result = namingConventions.GetEnumValueDescription(value);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void GetEnumValueDescription_XmlDescription()
    {
        // arrange
        var namingConventions = new DefaultNamingConventions(
            new XmlDocumentationProvider(
                new XmlDocumentationFileResolver(),
                new NoOpStringBuilderPool()));

        // act
        var result = namingConventions.GetEnumValueDescription(EnumWithDocEnum.Value1);

        // assert
        Assert.Equal("Value1 Documentation", result);
    }

    [Fact]
    public void GetEnumValueDescription_AttributeDescription()
    {
        // arrange
        var namingConventions = new DefaultNamingConventions(
            new XmlDocumentationProvider(
                new XmlDocumentationFileResolver(),
                new NoOpStringBuilderPool()));
        // act
        var result = namingConventions.GetEnumValueDescription(Foo.Baz);

        // assert
        Assert.Equal("Baz Desc", result);
    }

    [InlineData(typeof(MyInputType), "MyInput")]
    [InlineData(typeof(MyType), "MyTypeInput")]
    [InlineData(typeof(MyInput), "MyInput")]
    [InlineData(typeof(YourInputType), "YourInputTypeInput")]
    [InlineData(typeof(YourInput), "YourInput")]
    [InlineData(typeof(Your), "YourInput")]
    [Theory]
    public void Input_Naming_Convention(Type type, string expectedName)
    {
        // arrange
        var namingConventions = new DefaultNamingConventions(
            new XmlDocumentationProvider(
                new XmlDocumentationFileResolver(),
                new NoOpStringBuilderPool()));

        // act
        var typeName = namingConventions.GetTypeName(type, TypeKind.InputObject);

        // assert
        Assert.Equal(expectedName, typeName);
    }

    private enum Foo
    {
        Bar,

        [GraphQLDescription("Baz Desc")] Baz,
    }

    private sealed class MyInputType : InputObjectType
    {
    }

    private sealed class MyType : InputObjectType
    {
    }

    private sealed class MyInput : InputObjectType
    {
    }

    public class YourInputType
    {
    }

    public class YourInput
    {
    }

    public class Your
    {
    }
}
