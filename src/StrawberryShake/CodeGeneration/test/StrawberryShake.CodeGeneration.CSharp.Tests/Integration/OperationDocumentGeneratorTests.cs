using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Integration
{
    public class OperationDocumentGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly OperationDocumentGenerator _generator;

        public OperationDocumentGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new OperationDocumentGenerator();
        }

        [Fact]
        public async Task GenerateOperationDocument_GetHero()
        {
            await _generator.WriteAsync(
                _codeWriter,
                IntegrationDescriptors.CreateGetHeroQueryDescriptor());
            
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
