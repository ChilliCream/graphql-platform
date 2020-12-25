using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultFromEntityTypeMapperGeneratorTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly ResultFromEntityTypeMapperGenerator _generator;

        public ResultFromEntityTypeMapperGeneratorTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new ResultFromEntityTypeMapperGenerator();
        }

        [Fact]
        public async Task MapperWithOneValueProperty()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new ResultFromEntityTypeMapperDescriptor(
                    new TypeDescriptor(
                        NamingConventions.EntityTypeNameFromTypeName("Foo"),
                        "EntityNamespace",
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
                            ),
                            new TypePropertyDescriptor(
                                new TypeReferenceDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "OtherProperty"
                            )
                        }
                    ),
                    new TypeDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
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
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }


        [Fact]
        public async Task MapperWithTwoValueProperties()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new ResultFromEntityTypeMapperDescriptor(
                    new TypeDescriptor(
                        NamingConventions.EntityTypeNameFromTypeName("Foo"),
                        "EntityNamespace",
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
                            ),
                            new TypePropertyDescriptor(
                                new TypeReferenceDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "OtherProperty"
                            )
                        }
                    ),
                    new TypeDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
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
                            ),
                            new TypePropertyDescriptor(
                                new TypeReferenceDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "OtherProperty"
                            )
                        }
                    )
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }


        [Fact]
        public async Task MapperWithOneReferenceProperty()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new ResultFromEntityTypeMapperDescriptor(
                    new TypeDescriptor(
                        NamingConventions.EntityTypeNameFromTypeName("Foo"),
                        "EntityNamespace",
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
                            ),
                            new TypePropertyDescriptor(
                                new TypeReferenceDescriptor(
                                    "Bar",
                                    false,
                                    ListType.NoList,
                                    true
                                ),
                                "BarReference"
                            )
                        }
                    ),
                    new TypeDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
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
                                "BarReference"
                            )
                        }
                    )
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task MapperWithReferencePropertyList()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new ResultFromEntityTypeMapperDescriptor(
                    new TypeDescriptor(
                        NamingConventions.EntityTypeNameFromTypeName("Foo"),
                        "EntityNamespace",
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
                            ),
                            new TypePropertyDescriptor(
                                new TypeReferenceDescriptor(
                                    "Bar",
                                    false,
                                    ListType.List,
                                    true
                                ),
                                "BarReferenceList"
                            )
                        }
                    ),
                    new TypeDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
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
                            ),
                            new TypePropertyDescriptor(
                                new TypeReferenceDescriptor(
                                    "Bar",
                                    false,
                                    ListType.List,
                                    true
                                ),
                                "BarReferenceList"
                            )
                        }
                    )
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task MapperWithManyProperties()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new ResultFromEntityTypeMapperDescriptor(
                    new TypeDescriptor(
                        NamingConventions.EntityTypeNameFromTypeName("Foo"),
                        "EntityNamespace",
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
                            ),
                            new TypePropertyDescriptor(
                                new TypeReferenceDescriptor(
                                    "Bar",
                                    false,
                                    ListType.List,
                                    true
                                ),
                                "BarReferenceList"
                            ),
                            new TypePropertyDescriptor(
                                new TypeReferenceDescriptor(
                                    "Bar",
                                    false,
                                    ListType.NoList,
                                    true
                                ),
                                "BarReference"
                            ),
                            new TypePropertyDescriptor(
                                new TypeReferenceDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "SomeOtherText"
                            ),
                        }
                    ),
                    new TypeDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
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
                            ),
                            new TypePropertyDescriptor(
                                new TypeReferenceDescriptor(
                                    "Bar",
                                    false,
                                    ListType.List,
                                    true
                                ),
                                "BarReferenceList"
                            ),
                            new TypePropertyDescriptor(
                                new TypeReferenceDescriptor(
                                    "Bar",
                                    false,
                                    ListType.NoList,
                                    true
                                ),
                                "BarReference"
                            ),
                            new TypePropertyDescriptor(
                                new TypeReferenceDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "SomeOtherText"
                            ),
                        }
                    )
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
