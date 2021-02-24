using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Integration
{
    public class ResultBuilderGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly JsonResultBuilderGenerator _generator;

        public ResultBuilderGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new JsonResultBuilderGenerator();
        }

        [Fact]
        public void GenerateResultBuilder()
        {
            _generator.Generate(
                _codeWriter,
                IntegrationDescriptors.CreateGetHeroResultBuilderDescriptor(),
                out _);

            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
