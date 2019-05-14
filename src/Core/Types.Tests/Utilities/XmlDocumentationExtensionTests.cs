using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Utilities
{
    public class XmlDocumentationExtensionTests
    {
        [Fact]
        public void When_xml_doc_is_missing_then_summary_is_empty()
        {
            // arrange
            XmlDocumentationExtensions.ClearCache();

            // act
            var summary = typeof(Point).GetXmlSummary();

            // assert
            Assert.Empty(summary);
        }

        public class WithMultilineXmlDoc
        {
            /// <summary>
            /// Query and manages users.
            ///
            /// Please note:
            /// * Users ...
            /// * Users ...
            ///     * Users ...
            ///     * Users ...
            ///
            /// You need one of the following role: Owner,
            /// Editor, use XYZ to manage permissions.
            /// </summary>
            public string Foo { get; set; }
        }

        [Fact]
        public void When_xml_doc_with_multiple_breaks_is_read_then_they_are_not_stripped_away()
        {
            // arrange
            XmlDocumentationExtensions.ClearCache();

            // act
            var summary = typeof(WithMultilineXmlDoc)
                .GetProperty(nameof(WithMultilineXmlDoc.Foo))
                .GetXmlSummary();

            // assert
            Assert.Matches(new Regex(@"\n[ \t]*\n"), summary);
            Assert.Contains("    * Users", summary);
            Assert.Equal(summary.Trim(), summary);
        }

        public class WithSeeTagInXmlDoc
        {
            /// <summary>
            /// <see langword="null"/> for the default <see cref="Record"/>.
            /// See <see cref="Record">this</see> and
            /// <see href="https://github.com/rsuter/njsonschema">this</see> at
            /// <see href="https://github.com/rsuter/njsonschema"/>.
            /// </summary>
            public string Foo { get; set; }
        }

        [Fact]
        public void When_summary_has_see_tag_then_it_is_converted()
        {
            // arrange
            XmlDocumentationExtensions.ClearCache();

            // act
            var summary = typeof(WithSeeTagInXmlDoc)
                .GetProperty(nameof(WithSeeTagInXmlDoc.Foo))
                .GetXmlSummary();

            // asssert
            Assert.Equal(
                "null for the default Record. See this and this" +
                " at https://github.com/rsuter/njsonschema.",
                summary);
        }

        public class WithGenericTagsInXmlDoc
        {
            /// <summary>These <c>are</c> <strong>some</strong> tags.</summary>
            public string Foo { get; set; }
        }

        [Fact]
        public void When_summary_has_generic_tags_then_it_is_converted()
        {
            XmlDocumentationExtensions.ClearCache();

            var summary = typeof(WithGenericTagsInXmlDoc)
                .GetProperty(nameof(WithGenericTagsInXmlDoc.Foo))
                .GetXmlSummary();

            Assert.Equal("These are some tags.", summary);
        }

        /// <summary>
        /// I am the most base class.
        /// </summary>
        public abstract class BaseBaseClass
        {
            /// <summary>Summary of foo.</summary>
            public abstract string Foo { get; }

            /// <summary>Method doc.</summary>
            /// <param name="baz">Parameter details.</param>
            public abstract void Bar(string baz);
        }

        public abstract class BaseClass : BaseBaseClass
        {
            /// <inheritdoc />
            public override string Foo { get; }

            /// <inheritdoc />
            public override void Bar(string baz) { }
        }

        public class ClassWithInheritdoc : BaseClass
        {
            /// <inheritdoc />
            public override string Foo { get; }

            /// <inheritdoc />
            public override void Bar(string baz) { }
        }

        [Fact]
        public async Task When_type_has_summary_then_it_it_resolved()
        {
            XmlDocumentationExtensions.ClearCache();

            var summary = typeof(BaseBaseClass).GetXmlSummary();

            Assert.Equal("I am the most base class.", summary);
        }

        [Fact]
        public async Task When_parameter_has_inheritdoc_then_it_is_resolved()
        {
            XmlDocumentationExtensions.ClearCache();

            var parameterXml = await typeof(ClassWithInheritdoc)
                .GetMethod(nameof(ClassWithInheritdoc.Bar))
                .GetParameters()
                .Single(p => p.Name == "baz")
                .GetXmlDocumentation();

            Assert.Equal("Parameter details.", parameterXml);
        }

        [Fact]
        public async Task When_method_has_inheritdoc_then_it_is_resolved()
        {
            await XmlDocumentationExtensions.ClearCache();

            var methodSummary = await typeof(ClassWithInheritdoc)
                .GetMethod(nameof(ClassWithInheritdoc.Bar))
                .GetXmlSummaryAsync();

            Assert.Equal("Method doc.", methodSummary);
        }

        [Fact]
        public async Task When_property_has_inheritdoc_then_it_is_resolved()
        {
            await XmlDocumentationExtensions.ClearCache();

            var summary = await typeof(ClassWithInheritdoc)
                .GetProperty(nameof(ClassWithInheritdoc.Foo))
                .GetXmlSummaryAsync();

            Assert.Equal("Summary of foo.", summary);
        }

        /// <summary>
        /// I am an interface.
        /// </summary>
        public interface IBaseBaseInterface
        {
            /// <summary>Property summary.</summary>
            string Foo { get; }

            /// <summary>Method summary.</summary>
            /// <param name="baz">Parameter summary.</param>
            void Bar(string baz);
        }

        public interface IBaseInterface : IBaseBaseInterface
        {
        }

        public class ClassWithInheritdocOnInterface : IBaseInterface
        {
            /// <inheritdoc />
            public string Foo { get; }

            /// <inheritdoc />
            public void Bar(string baz) { }
        }

        public class ClassWithInterfaceAndCustomSummaries : IBaseInterface
        {
            /// <summary>
            /// I am my own property.
            /// </summary>
            public string Foo { get; }

            /// <summary>
            /// I am my own method.
            /// </summary>
            /// <param name="baz">I am my own parameter.</param>
            public void Bar(string baz) { }
        }

        [Fact]
        public async Task When_type_is_an_interface_then_summary_is_resolved()
        {
            await XmlDocumentationExtensions.ClearCache();

            var summary = await typeof(IBaseBaseInterface)
                .GetXmlSummary();

            Assert.Equal("I am an interface.", summary);
        }

        [Fact]
        public async Task When_parameter_has_inheritdoc_on_interface_then_it_is_resolved()
        {
            await XmlDocumentationExtensions.ClearCache();

            var summary = await typeof(ClassWithInheritdocOnInterface)
                .GetMethod(nameof(ClassWithInheritdocOnInterface.Bar))
                .GetParameters()
                .Single(p => p.Name == "baz")
                .GetXmlDocumentation();

            Assert.Equal("Parameter summary.", summary);
        }

        [Fact]
        public async Task When_property_has_inheritdoc_on_interface_then_it_is_resolved()
        {
            await XmlDocumentationExtensions.ClearCache();

            var summary = await typeof(ClassWithInheritdocOnInterface)
                .GetProperty(nameof(ClassWithInheritdocOnInterface.Foo))
                .GetXmlSummaryAsync();

            Assert.Equal("Property summary.", summary);
        }

        [Fact]
        public async Task When_method_has_inheritdoc_then_on_interface_it_is_resolved()
        {
            await XmlDocumentationExtensions.ClearCache();

            var methodSummary = await typeof(ClassWithInheritdocOnInterface)
                .GetMethod(nameof(ClassWithInheritdocOnInterface.Bar))
                .GetXmlSummaryAsync();

            Assert.Equal("Method summary.", methodSummary);
        }

        [Fact]
        public async Task When_class_implements_interface_and_property_has_summary_then_property_summary_is_used()
        {
            await XmlDocumentationExtensions.ClearCache();

            var summary = await typeof(ClassWithInterfaceAndCustomSummaries)
                .GetProperty(nameof(ClassWithInterfaceAndCustomSummaries.Foo))
                .GetXmlSummaryAsync();

            Assert.Equal("I am my own property.", summary);
        }

        [Fact]
        public async Task When_class_implements_interface_and_method_has_summary_then_method_summary_is_used()
        {
            await XmlDocumentationExtensions.ClearCache();

            var summary = await typeof(ClassWithInterfaceAndCustomSummaries)
                .GetMethod(nameof(ClassWithInterfaceAndCustomSummaries.Bar))
                .GetXmlSummaryAsync();

            Assert.Equal("I am my own method.", summary);
        }

        [Fact]
        public async Task When_class_implements_interface_and_method_has_summary_then_method_parameter_summary_is_used()
        {
            await XmlDocumentationExtensions.ClearCache();

            var summary = await typeof(ClassWithInterfaceAndCustomSummaries)
                .GetMethod(nameof(ClassWithInterfaceAndCustomSummaries.Bar))
                .GetParameters()
                .Single(p => p.Name == "baz")
                .GetXmlDocumentation();

            Assert.Equal("I am my own parameter.", summary);
        }

        /// <summary>
        /// I am a test class.
        /// </summary>
        public class ClassWithSummary
        {
        }

        [Fact]
        public async Task When_class_has_summary_then_it_is_converted()
        {
            await XmlDocumentationExtensions.ClearCache();

            var summary = await typeof(ClassWithSummary)
                .GetXmlSummary();

            Assert.Equal("I am a test class.", summary);
        }
    }
}
