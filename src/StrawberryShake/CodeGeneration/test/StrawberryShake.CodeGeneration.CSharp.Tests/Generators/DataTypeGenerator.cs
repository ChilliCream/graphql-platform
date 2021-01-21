using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Integration
{
    public class DataTypeGenerator
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly OperationDocumentGenerator _generator;

        public DataTypeGenerator()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new OperationDocumentGenerator();
        }

        // [Fact]
        // public async Task GenerateDataType_FriendsConnection()
        // {
        //     await _generator.Write(
        //         _codeWriter,
        //         IntegrationDescriptors.CreateDroidEntityTypeDescriptor());
        //
        //     _stringBuilder.ToString().MatchSnapshot();
        // }
    }
}
