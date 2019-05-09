using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Utilities
{
    public class XmlDocumentationExtensionTests
    {
        [Fact]
        public async Task When_xml_doc_is_missing_then_summary_is_empty()
        {
            await XmlDocumentationExtensions.ClearCacheAsync();

            var summary = await typeof(Point).GetXmlSummaryAsync();

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
            /// You need one of the following role: Owner, Editor, use XYZ to manage permissions.
            /// </summary>
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_xml_doc_with_multiple_breaks_is_read_then_they_are_not_stripped_away()
        {
            await XmlDocumentationExtensions.ClearCacheAsync();

            var summary = await typeof(WithMultilineXmlDoc)
                .GetProperty(nameof(WithMultilineXmlDoc.Foo))
                .GetXmlSummaryAsync();

            Assert.Contains("\n\n", summary);
            Assert.Contains("    * Users", summary);
            Assert.Equal(summary.Trim(), summary);
        }
        
        public class WithSeeTagInXmlDoc
        {
            /// <summary><see langword="null"/> for the default <see cref="Record"/>. See <see cref="Record">this</see> and <see href="https://github.com/rsuter/njsonschema">this</see> at <see href="https://github.com/rsuter/njsonschema"/>.</summary>
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_summary_has_see_tag_then_it_is_converted()
        {
            await XmlDocumentationExtensions.ClearCacheAsync();

            var summary = await typeof(WithSeeTagInXmlDoc)
                .GetProperty(nameof(WithSeeTagInXmlDoc.Foo))
                .GetXmlSummaryAsync();

            Assert.Equal("null for the default Record. See this and this at https://github.com/rsuter/njsonschema.", summary);
        }
        
        public class WithGenericTagsInXmlDoc
        {
            /// <summary>These <c>are</c> <strong>some</strong> tags.</summary>
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_summary_has_generic_tags_then_it_is_converted()
        {
            await XmlDocumentationExtensions.ClearCacheAsync();

            var summary = await typeof(WithGenericTagsInXmlDoc)
                .GetProperty(nameof(WithGenericTagsInXmlDoc.Foo))
                .GetXmlSummaryAsync();

            Assert.Equal("These are some tags.", summary);
        }
        
        public abstract class BaseBaseClass
        {
            /// <summary>Foo.</summary>
            public abstract string Foo { get; }

            /// <summary>Bar.</summary>
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
        public async Task When_parameter_has_inheritdoc_then_it_is_resolved()
        {
            await XmlDocumentationExtensions.ClearCacheAsync();

            var parameterXml = await typeof(ClassWithInheritdoc)
                .GetMethod(nameof(ClassWithInheritdoc.Bar))
                .GetParameters()
                .Single(p => p.Name == "baz")
                .GetXmlDocumentationAsync();

            Assert.Equal("Parameter details.", parameterXml);
        }
    }
}
