using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Types.Descriptors
{
    public class XmlDocumentationProviderTests
    {
        [Fact]
        public void When_xml_doc_is_missing_then_summary_is_empty()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(typeof(Point));

            // assert
            Assert.Null(summary);
        }

        [Fact]
        public void When_xml_doc_with_multiple_breaks_is_read_then_they_are_not_stripped_away()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(
                typeof(WithMultilineXmlDoc)
                    .GetProperty(nameof(WithMultilineXmlDoc.Foo)));

            // assert
            Assert.Matches(new Regex(@"\n[ \t]*\n"), summary);
            Assert.Contains("    * Users", summary);
            Assert.Equal(summary.Trim(), summary);
        }

        [Fact]
        public void When_summary_has_see_tag_then_it_is_converted()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(
                typeof(WithSeeTagInXmlDoc)
                    .GetProperty(nameof(WithSeeTagInXmlDoc.Foo)));

            // asssert
            Assert.Equal(
                "null for the default Record.\nSee this and\nthis" +
                " at\nhttps://foo.com/bar/baz.",
                summary);
        }

        [Fact]
        public void When_summary_has_generic_tags_then_it_is_converted()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(
                typeof(WithGenericTagsInXmlDoc)
                    .GetProperty(nameof(WithGenericTagsInXmlDoc.Foo)));

            // assert
            Assert.Equal("These are some tags.", summary);
        }

        [Fact]
        public void When_type_has_summary_then_it_it_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(
                typeof(BaseBaseClass));

            // assert
            Assert.Equal("I am the most base class.", summary);
        }

        [Fact]
        public void When_parameter_has_inheritdoc_then_it_is_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var parameterXml = documentationProvider.GetSummary(
                    typeof(ClassWithInheritdoc)
                .GetMethod(nameof(ClassWithInheritdoc.Bar))
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
                new XmlDocumentationFileResolver());

            // act
            var methodSummary = documentationProvider.GetSummary(
                typeof(ClassWithInheritdoc)
                    .GetMethod(nameof(ClassWithInheritdoc.Bar)));

            // assert
            Assert.Equal("Method doc.", methodSummary);
        }

        [Fact]
        public void When_property_has_inheritdoc_then_it_is_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(
                typeof(ClassWithInheritdoc)
                    .GetProperty(nameof(ClassWithInheritdoc.Foo)));

            // assert
            Assert.Equal("Summary of foo.", summary);
        }

        [Fact]
        public void When_type_is_an_interface_then_summary_is_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(
                typeof(IBaseBaseInterface));

            // assert
            Assert.Equal("I am an interface.", summary);
        }

        [Fact]
        public void When_parameter_has_inheritdoc_on_interface_then_it_is_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(
                typeof(ClassWithInheritdocOnInterface)
                    .GetMethod(nameof(ClassWithInheritdocOnInterface.Bar))
                    .GetParameters()
                    .Single(p => p.Name == "baz"));

            // assert
            Assert.Equal("Parameter summary.", summary);
        }

        [Fact]
        public void When_property_has_inheritdoc_on_interface_then_it_is_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(
                typeof(ClassWithInheritdocOnInterface)
                .GetProperty(nameof(ClassWithInheritdocOnInterface.Foo)));

            // assert
            Assert.Equal("Property summary.", summary);
        }

        [Fact]
        public void When_method_has_inheritdoc_then_on_interface_it_is_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var methodSummary = documentationProvider.GetSummary(
                typeof(ClassWithInheritdocOnInterface)
                .GetMethod(nameof(ClassWithInheritdocOnInterface.Bar)));

            // assert
            Assert.Equal("Method summary.", methodSummary);
        }

        [Fact]
        public void When_class_implements_interface_and_property_has_summary_then_property_summary_is_used()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(
                typeof(ClassWithInterfaceAndCustomSummaries)
                .GetProperty(nameof(ClassWithInterfaceAndCustomSummaries.Foo)));

            // assert
            Assert.Equal("I am my own property.", summary);
        }

        [Fact]
        public void When_class_implements_interface_and_method_has_summary_then_method_summary_is_used()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(
                typeof(ClassWithInterfaceAndCustomSummaries)
                .GetMethod(nameof(ClassWithInterfaceAndCustomSummaries.Bar)));

            // assert
            Assert.Equal("I am my own method.", summary);
        }

        [Fact]
        public void When_class_implements_interface_and_method_has_summary_then_method_parameter_summary_is_used()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(
                typeof(ClassWithInterfaceAndCustomSummaries)
                .GetMethod(nameof(ClassWithInterfaceAndCustomSummaries.Bar))
                .GetParameters()
                .Single(p => p.Name == "baz"));

            // assert
            Assert.Equal("I am my own parameter.", summary);
        }

        [Fact]
        public void When_class_has_summary_then_it_is_converted()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = documentationProvider.GetSummary(
                typeof(ClassWithSummary));

            // assert
            Assert.Equal("I am a test class.", summary);
        }
    }
}
