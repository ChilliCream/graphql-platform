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
        public void GenerateOperationDocument_GetHero()
        {
            _generator.Generate(
                _codeWriter,
                IntegrationDescriptors.CreateGetHeroQueryDescriptor());

            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
