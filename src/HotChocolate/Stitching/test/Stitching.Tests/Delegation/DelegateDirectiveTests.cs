using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Delegation
{
    public class DelegateDirectiveTests
    {
        [Fact]
        public void Directive_Definition_PrintIsMtch()
        {
            // arrange
            DocumentNode schemaDocument = SchemaBuilder.New()
                .ModifyOptions(x => x.StrictValidation = false)
                .AddDirectiveType<DelegateDirectiveType>()
                .Create()
                .ToDocument();

            // act
            var printed = schemaDocument.Definitions
                .OfType<DirectiveDefinitionNode>()
                .First(x => x.Name.Value == "delegate")
                .Print();

            // assert
            printed.MatchSnapshot();
        }
    }
}
