using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Integration
{
    public class ResultDataFactoryTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly ResultDataFactoryGenerator _generator;

        public ResultDataFactoryTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new ResultDataFactoryGenerator();
        }

        [Fact]
        public async Task GenerateResultDataFactory_GetHeroResult()
        {
            await _generator.WriteAsync(
                _codeWriter,
                IntegrationDescriptors.GetHeroResultDescriptor
            );
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
