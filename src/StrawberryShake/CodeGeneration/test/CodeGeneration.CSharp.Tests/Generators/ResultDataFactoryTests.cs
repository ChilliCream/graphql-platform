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
        public void GenerateResultDataFactory_GetHeroResult()
        {
            _generator.Generate(
                _codeWriter,
                IntegrationDescriptors.CreateGetHeroResultDescriptor());

            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
