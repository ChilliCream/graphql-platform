using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Tests
{
    public class ResultTypeGenerationTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly ResultTypeGenerator _generator;

        public ResultTypeGenerationTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new ResultTypeGenerator();
        }

        [Fact]
        public async Task GenerateSimpleClassWithOneValueTypeProperty()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new TypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    new string[] { },
                    new[]
                    {
                        new TypePropertyDescriptor(
                            new TypeReferenceDescriptor(
                                "string",
                                false,
                                ListType.NoList,
                                false
                            ),
                            "SomeText"
                        )
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateSimpleClassWithOneReferenceTypeProperty()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new TypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    new string[] { },
                    new[]
                    {
                        new TypePropertyDescriptor(
                            new TypeReferenceDescriptor(
                                "Bar",
                                false,
                                ListType.NoList,
                                true
                            ),
                            "Bar"
                        )
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateSimpleClassWithOneNullableValueTypeProperty()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new TypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    new string[] { },
                    new[]
                    {
                        new TypePropertyDescriptor(
                            new TypeReferenceDescriptor(
                                "string",
                                true,
                                ListType.NoList,
                                true
                            ),
                            "SomeText"
                        )
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateSimpleClassWithOneNullableReferenceTypeProperty()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new TypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    new string[] { },
                    new[]
                    {
                        new TypePropertyDescriptor(
                            new TypeReferenceDescriptor(
                                "Bar",
                                true,
                                ListType.NoList,
                                true
                            ),
                            "Bar"
                        )
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateSimpleClassWithImplements()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new TypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    new string[] {"IFoo", "IBar"},
                    new[]
                    {
                        new TypePropertyDescriptor(
                            new TypeReferenceDescriptor(
                                "Bar",
                                false,
                                ListType.NoList,
                                true
                            ),
                            "Bar"
                        )
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }


        [Fact]
        public async Task GenerateSimpleClassWithMultipleProperties()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new TypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    new string[] { },
                    new[]
                    {
                        new TypePropertyDescriptor(
                            new TypeReferenceDescriptor(
                                "string",
                                false,
                                ListType.NoList,
                                false
                            ),
                            "Id"
                        ),
                        new TypePropertyDescriptor(
                            new TypeReferenceDescriptor(
                                "Bar",
                                false,
                                ListType.List,
                                true
                            ),
                            "Bars"
                        ),
                        new TypePropertyDescriptor(
                            new TypeReferenceDescriptor(
                                "Baz",
                                true,
                                ListType.NoList,
                                true
                            ),
                            "Baz"
                        ),
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
