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
        public async Task GenerateResultMapper_Droid()
        {
            await _generator.WriteAsync(
                _codeWriter,
                IntegrationDescriptors.CreateDroidNamedTypeDescriptor());

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateResultMapper_DroidHero()
        {
            await _generator.WriteAsync(
                _codeWriter,
                IntegrationDescriptors.CreateDroidHeroNamedTypeDescriptor());

            _stringBuilder.ToString().MatchSnapshot();
        }


        [Fact]
        public async Task GenerateResultMapper_Human()
        {
            await _generator.WriteAsync(
                _codeWriter,
                IntegrationDescriptors.CreateHumanNamedTypeDescriptor());

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateResultMapper_HumanHero()
        {
            await _generator.WriteAsync(
                _codeWriter,
                IntegrationDescriptors.CreateHumanHeroNamedTypeDescriptor());

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateResultMapper_FriendsConnection()
        {
            await _generator.WriteAsync(
                _codeWriter,
                IntegrationDescriptors.CreateFriendsConnectionDescriptor());

            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
