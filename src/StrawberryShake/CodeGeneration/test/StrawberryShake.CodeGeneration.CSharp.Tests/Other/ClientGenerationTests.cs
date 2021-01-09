using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Other
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
                            new TypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "Foo",
                                    "FooNamespace"
                                ),
                                false,
                                ListType.NoList
                            ),
                            new Dictionary<string, TypeReferenceDescriptor>()
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
                            new TypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "GetFoo",
                                    "FooNamespace"
                                ),
                                false,
                                ListType.NoList
                            ),
                            new Dictionary<string, TypeReferenceDescriptor>()
                        ),
                        new MutationOperationDescriptor(
                            new TypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "UpdateFoo",
                                    "FooNamespace"
                                ),
                                false,
                                ListType.NoList
                            ),
                            new Dictionary<string, TypeReferenceDescriptor>()
                        )
                    }
                )
            );

            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
