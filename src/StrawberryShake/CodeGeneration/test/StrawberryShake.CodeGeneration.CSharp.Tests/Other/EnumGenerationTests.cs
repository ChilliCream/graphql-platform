using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Other
{
    public class EnumGenerationTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly EnumGenerator _generator;

        public EnumGenerationTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new EnumGenerator();
        }

        [Fact]
        public async Task GenerateSingleValueEnum()
        {
            await _generator.Write(
                _codeWriter,
                new EnumDescriptor(
                    "Foonum",
                    "FooBarNamespace",
                    new []
                    {
                        new EnumElementDescriptor("Foo", "foo")
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateSingleValueEnumWithExplicitValue()
        {
            await _generator.Write(
                _codeWriter,
                new EnumDescriptor(
                    "Foonum",
                    "FooBarNamespace",
                    new []
                    {
                        new EnumElementDescriptor("Foo", "foo", 42)
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateMultiValueEnum()
        {
            await _generator.Write(
                _codeWriter,
                new EnumDescriptor(
                    "Foonum",
                    "FooBarNamespace",
                    new []
                    {
                        new EnumElementDescriptor("Foo", "foo"),
                        new EnumElementDescriptor("Bar", "bar")
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateMultiValueEnumWithOneExplicitValue()
        {
            await _generator.Write(
                _codeWriter,
                new EnumDescriptor(
                    "Foonum",
                    "FooBarNamespace",
                    new []
                    {
                        new EnumElementDescriptor("Foo", "foo", 42),
                        new EnumElementDescriptor("Bar", "bar")
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateMultiValueEnumWithMultipleExplicitValues()
        {
            await _generator.Write(
                _codeWriter,
                new EnumDescriptor(
                    "Foonum",
                    "FooBarNamespace",
                    new []
                    {
                        new EnumElementDescriptor("Foo", "foo", 21),
                        new EnumElementDescriptor("Bar", "bar", 42)
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
