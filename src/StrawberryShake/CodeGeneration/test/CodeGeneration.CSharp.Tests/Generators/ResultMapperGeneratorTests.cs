using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Integration
{
    public class ResultMapperGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly ResultFromEntityTypeMapperGenerator _generator;

        public ResultMapperGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new ResultFromEntityTypeMapperGenerator();
        }

        [Fact]
        public void GenerateResultMapper_Droid()
        {
            _generator.Generate(
                _codeWriter,
                IntegrationDescriptors.CreateDroidNamedTypeDescriptor(),
                out _);

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateResultMapper_DroidHero()
        {
            _generator.Generate(
                _codeWriter,
                IntegrationDescriptors.CreateDroidHeroNamedTypeDescriptor(),
                out _);

            _stringBuilder.ToString().MatchSnapshot();
        }


        [Fact]
        public void GenerateResultMapper_Human()
        {
            _generator.Generate(
                _codeWriter,
                IntegrationDescriptors.CreateHumanNamedTypeDescriptor(),
                out _);

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateResultMapper_HumanHero()
        {
            _generator.Generate(
                _codeWriter,
                IntegrationDescriptors.CreateHumanHeroNamedTypeDescriptor(),
                out _);

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateResultMapper_FriendsConnection()
        {
            _generator.Generate(
                _codeWriter,
                IntegrationDescriptors.CreateFriendsConnectionDescriptor(),
                out _);

            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
