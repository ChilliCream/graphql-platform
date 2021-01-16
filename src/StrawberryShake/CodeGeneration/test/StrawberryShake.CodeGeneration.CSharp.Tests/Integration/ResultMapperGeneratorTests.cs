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
                IntegrationDescriptors.DroidTypeDescriptor
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateResultMapper_DroidHero()
        {
            await _generator.WriteAsync(
                _codeWriter,
                IntegrationDescriptors.DroidHeroTypeDescriptor
            );
            _stringBuilder.ToString().MatchSnapshot();
        }


        [Fact]
        public async Task GenerateResultMapper_Human()
        {
            await _generator.WriteAsync(
                _codeWriter,
                IntegrationDescriptors.HumanTypeDescriptor
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateResultMapper_HumanHero()
        {
            await _generator.WriteAsync(
                _codeWriter,
                IntegrationDescriptors.HumanHeroTypeDescriptor
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateResultMapper_FriendsConnection()
        {
            await _generator.WriteAsync(
                _codeWriter,
                IntegrationDescriptors.FriendsConnectionDescriptor
            );
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
