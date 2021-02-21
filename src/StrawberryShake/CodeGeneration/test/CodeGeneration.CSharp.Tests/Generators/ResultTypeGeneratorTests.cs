using System.Text;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using StrawberryShake.CodeGeneration.Extensions;
using Xunit;

namespace StrawberryShake.Integration
{
    public class ResultTypeGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly ResultTypeGenerator _generator;

        public ResultTypeGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new ResultTypeGenerator();
        }

        [Fact]
        public void GenerateResult_GetHeroResult()
        {
            _generator.Generate(
                _codeWriter,
                IntegrationDescriptors.CreateGetHeroResultDescriptor(),
                out _);

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateResult_IHero()
        {
            _generator.Generate(
                _codeWriter,
                IntegrationDescriptors.CreateIHeroDescriptor().InnerType(),
                out _);

            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
