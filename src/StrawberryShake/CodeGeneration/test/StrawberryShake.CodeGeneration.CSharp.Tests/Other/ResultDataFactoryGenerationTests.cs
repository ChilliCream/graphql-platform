using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Tests.Other
{
    public class ResultDataFactoryGenerationTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly ResultDataFactoryGenerator _generator;

        public ResultDataFactoryGenerationTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new ResultDataFactoryGenerator();
        }

        [Fact]
        public async Task GenerateSimpleResultDataFactory_WithOneValueTypeProperty()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new TypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    new string[] { },
                    new[]
                    {
                        TestHelper.GetNamedNonNullStringTypeReference("SomeText")
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
