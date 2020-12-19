using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Tests
{
    public class ClientGenerationTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly ClientGenerator _generator;

        public ClientGenerationTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new ClientGenerator();
        }

        [Fact]
        public async Task GenerateClient_OneOperation_Success()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new ClientDescriptor(
                    "FooClient",
                    new List<OperationDescriptor>()
                    {
                        new QueryOperationDescriptor(
                            new TypeDescriptor(
                                "GetFoo",
                                false,
                                ListType.NoList,
                                true
                            ),
                            new Dictionary<string, TypeDescriptor>()
                        )
                    }
                )
            );

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateClient_TwoOperations_Success()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new ClientDescriptor(
                    "FooClient",
                    new List<OperationDescriptor>()
                    {
                        new QueryOperationDescriptor(
                            new TypeDescriptor(
                                "GetFoo",
                                false,
                                ListType.NoList,
                                true
                            ),
                            new Dictionary<string, TypeDescriptor>()
                        ),
                        new MutationOperationDescriptor(
                            new TypeDescriptor(
                                "UpdateFoo",
                                false,
                                ListType.NoList,
                                true
                            ),
                            new Dictionary<string, TypeDescriptor>()
                        )
                    }
                )
            );

            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
