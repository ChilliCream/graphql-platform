using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Integration
{
    public class ResultInfoGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly ResultInfoGenerator _generator;

        public ResultInfoGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new ResultInfoGenerator();
        }

        [Fact]
        public async Task GenerateResultInfo_GetHeroResultInfo()
        {
            await _generator.WriteAsync(
                _codeWriter,
                IntegrationDescriptors.GetHeroResultDescriptor
            );
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
