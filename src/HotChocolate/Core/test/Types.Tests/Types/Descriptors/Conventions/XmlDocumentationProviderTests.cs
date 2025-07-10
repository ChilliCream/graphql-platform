using System.Drawing;
using System.Text.RegularExpressions;

namespace HotChocolate.Types.Descriptors;

public class XmlDocumentationProviderTests
{
    [Fact]
    public void When_xml_doc_is_missing_then_description_is_empty()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(typeof(Point));

        // assert
        Assert.Null(description);
    }

    [Fact]
    public void When_xml_doc_with_multiple_breaks_is_read_then_they_are_not_stripped_away()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(WithMultilineXmlDoc)
                .GetProperty(nameof(WithMultilineXmlDoc.Foo))!);

        // assert
        Assert.Matches(new Regex(@"\n[ \t]*\n"), description);
        Assert.Contains("    * Users", description);
        Assert.Equal(description.Trim(), description);
    }

    [Fact]
    public void When_description_has_see_tag_then_it_is_converted()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(WithSeeTagInXmlDoc)
                .GetProperty(nameof(WithSeeTagInXmlDoc.Foo))!);

        // asssert
        Assert.Equal(
            "null for the default Record.\nSee this and\nthis" +
            " at\nhttps://foo.com/bar/baz.",
            description);
    }

    [Fact]
    public void When_description_has_paramref_tag_then_it_is_converted()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(WithParamrefTagInXmlDoc)
                .GetMethod(nameof(WithParamrefTagInXmlDoc.Foo))!);

        // assert
        Assert.Equal(
            "This is a parameter reference to id.",
            description);
    }

    [Fact]
    public void When_description_has_generic_tags_then_it_is_converted()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(WithGenericTagsInXmlDoc)
                .GetProperty(nameof(WithGenericTagsInXmlDoc.Foo))!);

        // assert
        Assert.Equal("These are some tags.", description);
    }

    [Fact]
    public void When_type_has_description_then_it_it_resolved()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(BaseBaseClass));

        // assert
        Assert.Equal("I am the most base class.", description);
    }

    [Fact]
    public void When_we_use_custom_documentation_files_they_are_correctly_loaded()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(_ => "Dummy.xml"),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(BaseBaseClass));

        // assert
        Assert.Equal("I am the most base class from dummy.", description);
    }

    [Fact]
    public void When_parameter_has_inheritdoc_then_it_is_resolved()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var parameterXml = documentationProvider.GetDescription(
            typeof(ClassWithInheritdoc)
                .GetMethod(nameof(ClassWithInheritdoc.Bar))!
                .GetParameters()
                .Single(p => p.Name == "baz"));

        // assert
        Assert.Equal("Parameter details.", parameterXml);
    }

    [Fact]
    public void When_method_has_inheritdoc_then_it_is_resolved()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var methoddescription = documentationProvider.GetDescription(
            typeof(ClassWithInheritdoc)
                .GetMethod(nameof(ClassWithInheritdoc.Bar))!);

        // assert
        Assert.Equal("Method doc.", methoddescription);
    }

    [Fact]
    public void When_property_has_inheritdoc_then_it_is_resolved()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(ClassWithInheritdoc)
                .GetProperty(nameof(ClassWithInheritdoc.Foo))!);

        // assert
        Assert.Equal("Summary of foo.", description);
    }

    [Fact]
    public void When_type_is_an_interface_then_description_is_resolved()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(IBaseBaseInterface));

        // assert
        Assert.Equal("I am an interface.", description);
    }

    [Fact]
    public void When_parameter_has_inheritdoc_on_interface_then_it_is_resolved()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(ClassWithInheritdocOnInterface)
                .GetMethod(nameof(ClassWithInheritdocOnInterface.Bar))!
                .GetParameters()
                .Single(p => p.Name == "baz"));

        // assert
        Assert.Equal("Parameter summary.", description);
    }

    [Fact]
    public void When_property_has_inheritdoc_on_interface_then_it_is_resolved()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(ClassWithInheritdocOnInterface)
                .GetProperty(nameof(ClassWithInheritdocOnInterface.Foo))!);

        // assert
        Assert.Equal("Property summary.", description);
    }

    [Fact]
    public void When_method_has_inheritdoc_then_on_interface_it_is_resolved()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var methodDescription = documentationProvider.GetDescription(
            typeof(ClassWithInheritdocOnInterface)
                .GetMethod(nameof(ClassWithInheritdocOnInterface.Bar))!);

        // assert
        Assert.Equal("Method summary.", methodDescription);
    }

    [Fact]
    public void When_class_implements_interface_and_property_has_description_then_property_description_is_used()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(ClassWithInterfaceAndCustomSummaries)
                .GetProperty(nameof(ClassWithInterfaceAndCustomSummaries.Foo))!);

        // assert
        Assert.Equal("I am my own property.", description);
    }

    [Fact]
    public void When_class_implements_interface_and_method_has_description_then_method_description_is_used()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(ClassWithInterfaceAndCustomSummaries)
                .GetMethod(nameof(ClassWithInterfaceAndCustomSummaries.Bar))!);

        // assert
        Assert.Equal("I am my own method.", description);
    }

    [Fact]
    public void When_class_implements_interface_and_method_has_description_then_method_parameter_description_is_used()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(ClassWithInterfaceAndCustomSummaries)
                .GetMethod(nameof(ClassWithInterfaceAndCustomSummaries.Bar))!
                .GetParameters()
                .Single(p => p.Name == "baz"));

        // assert
        Assert.Equal("I am my own parameter.", description);
    }

    [Fact]
    public void When_class_has_description_then_it_is_converted()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var description = documentationProvider.GetDescription(
            typeof(ClassWithSummary));

        // assert
        Assert.Equal("I am a test class. This should not be escaped: >", description);
    }

    [Fact]
    public void When_method_has_exceptions_then_it_is_converted()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var methodDescription = documentationProvider.GetDescription(
            typeof(WithExceptionsXmlDoc).GetMethod(nameof(WithExceptionsXmlDoc.Foo))!);

        // assert
        methodDescription.MatchSnapshot();
    }

    [Fact]
    public void When_method_has_exceptions_then_exceptions_with_no_code_will_be_ignored()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var methodDescription = documentationProvider.GetDescription(
            typeof(WithExceptionsXmlDoc).GetMethod(nameof(WithExceptionsXmlDoc.Bar))!);

        // assert
        methodDescription.MatchSnapshot();
    }

    [Fact]
    public void When_method_has_only_exceptions_with_no_code_then_error_section_will_not_be_written()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var methodDescription = documentationProvider.GetDescription(
            typeof(WithExceptionsXmlDoc).GetMethod(nameof(WithExceptionsXmlDoc.Baz))!);

        // assert
        methodDescription.MatchSnapshot();
    }

    [Fact]
    public void When_method_has_no_exceptions_then_it_is_ignored()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var methodDescription = documentationProvider.GetDescription(
            typeof(WithoutExceptionsXmlDoc).GetMethod(nameof(WithoutExceptionsXmlDoc.Foo))!);

        // assert
        methodDescription.MatchSnapshot();
    }

    [Fact]
    public void When_method_has_returns_then_it_is_converted()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var methodDescription = documentationProvider.GetDescription(
            typeof(WithReturnsXmlDoc).GetMethod(nameof(WithReturnsXmlDoc.Foo))!);

        // assert
        methodDescription.MatchSnapshot();
    }

    [Fact]
    public void When_method_has_no_returns_then_it_is_ignored()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var methodDescription = documentationProvider.GetDescription(
            typeof(WithoutReturnsXmlDoc).GetMethod(nameof(WithoutReturnsXmlDoc.Foo))!);

        // assert
        methodDescription.MatchSnapshot();
    }

    [Fact]
    public void When_method_has_dictionary_args_then_it_is_found()
    {
        // arrange
        var documentationProvider = new XmlDocumentationProvider(
            new XmlDocumentationFileResolver(),
            new NoOpStringBuilderPool());

        // act
        var methodDescription = documentationProvider.GetDescription(
            typeof(WithDictionaryArgs).GetMethod(nameof(WithDictionaryArgs.Method))!);

        // assert
        Assert.Equal("This is a method description", methodDescription);
    }
}
