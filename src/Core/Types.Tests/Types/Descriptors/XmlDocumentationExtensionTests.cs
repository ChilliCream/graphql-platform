using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Types.Descriptors
{
    public class XmlDocumentationExtensionTests
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
            Assert.Empty(summary);
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
                "null for the default Record. See this and this" +
                " at https://github.com/rsuter/njsonschema.",
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
            var parameterXml = documentationProvider.GetMemberSummary(
                    typeof(ClassWithInheritdoc)
                .GetMethod(nameof(ClassWithInheritdoc.Bar))
                .GetParameters()
                .Single(p => p.Name == "baz")
                .GetXmlDocumentation();

            // assert
            Assert.Equal("Parameter details.", parameterXml);
        }

        [Fact]
        public async Task When_method_has_inheritdoc_then_it_is_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var methodSummary = await typeof(ClassWithInheritdoc)
                .GetMethod(nameof(ClassWithInheritdoc.Bar))
                .GetXmlSummaryAsync();

            // assert
            Assert.Equal("Method doc.", methodSummary);
        }

        [Fact]
        public async Task When_property_has_inheritdoc_then_it_is_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = await typeof(ClassWithInheritdoc)
                .GetProperty(nameof(ClassWithInheritdoc.Foo))
                .GetXmlSummaryAsync();

            // assert
            Assert.Equal("Summary of foo.", summary);
        }





        [Fact]
        public async Task When_type_is_an_interface_then_summary_is_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = await typeof(IBaseBaseInterface)
                .GetXmlSummary();

            // assert
            Assert.Equal("I am an interface.", summary);
        }

        [Fact]
        public async Task When_parameter_has_inheritdoc_on_interface_then_it_is_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = await typeof(ClassWithInheritdocOnInterface)
                .GetMethod(nameof(ClassWithInheritdocOnInterface.Bar))
                .GetParameters()
                .Single(p => p.Name == "baz")
                .GetXmlDocumentation();

            // assert
            Assert.Equal("Parameter summary.", summary);
        }

        [Fact]
        public async Task When_property_has_inheritdoc_on_interface_then_it_is_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = await typeof(ClassWithInheritdocOnInterface)
                .GetProperty(nameof(ClassWithInheritdocOnInterface.Foo))
                .GetXmlSummaryAsync();

            // assert
            Assert.Equal("Property summary.", summary);
        }

        [Fact]
        public async Task When_method_has_inheritdoc_then_on_interface_it_is_resolved()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var methodSummary = await typeof(ClassWithInheritdocOnInterface)
                .GetMethod(nameof(ClassWithInheritdocOnInterface.Bar))
                .GetXmlSummaryAsync();


            // assert
            Assert.Equal("Method summary.", methodSummary);
        }

        [Fact]
        public async Task When_class_implements_interface_and_property_has_summary_then_property_summary_is_used()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = await typeof(ClassWithInterfaceAndCustomSummaries)
                .GetProperty(nameof(ClassWithInterfaceAndCustomSummaries.Foo))
                .GetXmlSummaryAsync();

            // assert
            Assert.Equal("I am my own property.", summary);
        }

        [Fact]
        public async Task When_class_implements_interface_and_method_has_summary_then_method_summary_is_used()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = await typeof(ClassWithInterfaceAndCustomSummaries)
                .GetMethod(nameof(ClassWithInterfaceAndCustomSummaries.Bar))
                .GetXmlSummaryAsync();

            // assert
            Assert.Equal("I am my own method.", summary);
        }

        [Fact]
        public async Task When_class_implements_interface_and_method_has_summary_then_method_parameter_summary_is_used()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = await typeof(ClassWithInterfaceAndCustomSummaries)
                .GetMethod(nameof(ClassWithInterfaceAndCustomSummaries.Bar))
                .GetParameters()
                .Single(p => p.Name == "baz")
                .GetXmlDocumentation();

            // assert
            Assert.Equal("I am my own parameter.", summary);
        }



        [Fact]
        public async Task When_class_has_summary_then_it_is_converted()
        {
            // arrange
            var documentationProvider = new XmlDocumentationProvider(
                new XmlDocumentationFileResolver());

            // act
            var summary = await typeof(ClassWithSummary)
                .GetXmlSummary();

            // assert
            Assert.Equal("I am a test class.", summary);
        }
    }
}
