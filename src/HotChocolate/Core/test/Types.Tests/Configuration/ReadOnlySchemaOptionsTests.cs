using System;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Configuration
{
    public class ReadOnlySchemaOptionsTests
    {
        [Fact]
        public void Copy_Options()
        {
            // arrange
            var options = new SchemaOptions
            {
                QueryTypeName = "A",
                MutationTypeName = "B",
                SubscriptionTypeName = "C",
                StrictValidation = false,
                SortFieldsByName = true,
                UseXmlDocumentation = false,
                DefaultBindingBehavior = BindingBehavior.Explicit,
                FieldMiddleware = FieldMiddlewareApplication.AllFields,
                PreserveSyntaxNodes = true
            };

            // act
            var copied = new ReadOnlySchemaOptions(options);

            // assert
            copied.MatchSnapshot();
        }

        [Fact]
        public void Copy_Options_ResolveXmlDocumentationFileName()
        {
            // arrange
            var options = new SchemaOptions
            {
                QueryTypeName = "A",
                MutationTypeName = "B",
                SubscriptionTypeName = "C",
                StrictValidation = false,
                SortFieldsByName = true,
                UseXmlDocumentation = false,
                ResolveXmlDocumentationFileName = assembly => "docs.xml",
                DefaultBindingBehavior = BindingBehavior.Explicit,
                FieldMiddleware = FieldMiddlewareApplication.AllFields,
                PreserveSyntaxNodes = true
            };

            // act
            var copied = new ReadOnlySchemaOptions(options);

            // assert
            Assert.Same(
                options.ResolveXmlDocumentationFileName, 
                copied.ResolveXmlDocumentationFileName);
        }

        [Fact]
        public void Copy_Options_Defaults()
        {
            // arrange
            var options = new SchemaOptions();

            // act
            var copied = new ReadOnlySchemaOptions(options);

            // assert
            copied.MatchSnapshot();
        }

        [Fact]
        public void Create_Options_Null()
        {
            // arrange
            // act
            Action action = () => new ReadOnlySchemaOptions(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
