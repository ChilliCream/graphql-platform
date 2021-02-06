using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Integration
{
    public class EntityTypeGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly EntityTypeGenerator _generator;

        public EntityTypeGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new EntityTypeGenerator();
        }

        [Fact]
        public void GenerateEntityType_Droid()
        {
            _generator.Generate(
                _codeWriter,
                IntegrationDescriptors.CreateDroidEntityTypeDescriptor());

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateEntityType_Human()
        {
            _generator.Generate(
                _codeWriter,
                IntegrationDescriptors.CreateHumanEntityTypeDescriptor());

            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
