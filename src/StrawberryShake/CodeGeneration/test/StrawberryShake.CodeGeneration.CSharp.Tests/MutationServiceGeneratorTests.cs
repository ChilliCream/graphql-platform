using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Tests
{
    public class MutationServiceGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly OperationServiceGenerator _generator;

        public MutationServiceGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new OperationServiceGenerator();
        }

        [Fact]
        public async Task GenerateMutationServiceWithoutArguments()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new MutationOperationDescriptor(
                    new TypeDescriptor(
                        "Foo",
                        false,
                        ListType.NoList,
                        true
                    ),
                    new Dictionary<string, TypeDescriptor>()
                )
            );

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateMutationServiceWithValueArgument()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new MutationOperationDescriptor(
                    new TypeDescriptor(
                        "Foo",
                        false,
                        ListType.NoList,
                        true
                    ),
                    new Dictionary<string, TypeDescriptor>()
                    {
                        {"name", new TypeDescriptor("string", false, ListType.NoList, false)}
                    }
                )
            );

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateMutationServiceWithReferenceArgument()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new MutationOperationDescriptor(
                    new TypeDescriptor(
                        "Foo",
                        false,
                        ListType.NoList,
                        true
                    ),
                    new Dictionary<string, TypeDescriptor>()
                    {
                        {"bar", new TypeDescriptor("BarInput", true, ListType.NoList, true)}
                    }
                )
            );

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateMutationServiceWithArguments()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new MutationOperationDescriptor(
                    new TypeDescriptor(
                        "Foo",
                        false,
                        ListType.NoList,
                        true
                    ),
                    new Dictionary<string, TypeDescriptor>()
                    {
                        {"name", new TypeDescriptor("string", false, ListType.NoList, false)},
                        {"a", new TypeDescriptor("BarInput", true, ListType.NoList, true)}
                    }
                )
            );

            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
