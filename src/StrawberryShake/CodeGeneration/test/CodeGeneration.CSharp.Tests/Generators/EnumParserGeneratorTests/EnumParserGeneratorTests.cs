using System.Text;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Other
{
    public class EnumParserGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly EnumParserGenerator _generator;

        public EnumParserGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new EnumParserGenerator();
        }

        [Fact]
        public void GenerateEnumParser()
        {
            _generator.Generate(
                _codeWriter,
                new EnumTypeDescriptor(
                    "MetasyntacticVariable",
                    "FooBarNamespace",
                    new []
                    {
                        new EnumValueDescriptor("Foo", "FOO"),
                        new EnumValueDescriptor("Bar", "BAR"),
                        new EnumValueDescriptor("Baz", "BAZ")
                    }),
                out string fileName);

            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
