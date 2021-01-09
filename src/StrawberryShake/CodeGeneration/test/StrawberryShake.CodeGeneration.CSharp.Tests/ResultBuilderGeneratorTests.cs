using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Tests
{
    public class ResultBuilderGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly JsonResultBuilderGenerator _generator;

        public ResultBuilderGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new JsonResultBuilderGenerator();
        }

        [Fact]
        public async Task GenerateResultBuilder_WithIntegrationTestStencil()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new ResultBuilderDescriptor()
                {
                    ResultType = new TypeDescriptor(
                        "GetHeroResult",
                        "FooBarNamespace",
                        new string[] { },
                        new[]
                        {
                            new NamedTypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "IHero",
                                    "IHeroNamespace",
                                    isImplementedBy: new[] {"Human", "Droid"},
                                    isEntityType: true
                                ),
                                false,
                                ListType.NoList,
                                "Hero"
                            ),
                            new NamedTypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    "System"
                                ),
                                false,
                                ListType.NoList,
                                "Version"
                            )
                        }
                    ),
                    ValueParsers = new[] {("string", "string", "String")}
                }
            );
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
