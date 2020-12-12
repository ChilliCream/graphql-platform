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
                    new TypeClassDescriptor(
                        NamingConventions.EntityTypeNameFromTypeName("Foo"),
                        "EntityNamespace",
                        new string[] { },
                        new[]
                        {
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "SomeText"
                            ),
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "OtherProperty"
                            )
                        }
                    ),
                    new TypeClassDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
                        new string[] { },
                        new[]
                        {
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
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
                    new TypeClassDescriptor(
                        NamingConventions.EntityTypeNameFromTypeName("Foo"),
                        "EntityNamespace",
                        new string[] { },
                        new[]
                        {
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "SomeText"
                            ),
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "OtherProperty"
                            )
                        }
                    ),
                    new TypeClassDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
                        new string[] { },
                        new[]
                        {
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "SomeText"
                            ),
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
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
                    new TypeClassDescriptor(
                        NamingConventions.EntityTypeNameFromTypeName("Foo"),
                        "EntityNamespace",
                        new string[] { },
                        new[]
                        {
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "SomeText"
                            ),
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    false,
                                    ListType.NoList,
                                    true
                                ),
                                "BarReference"
                            )
                        }
                    ),
                    new TypeClassDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
                        new string[] { },
                        new[]
                        {
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
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
                    new TypeClassDescriptor(
                        NamingConventions.EntityTypeNameFromTypeName("Foo"),
                        "EntityNamespace",
                        new string[] { },
                        new[]
                        {
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "SomeText"
                            ),
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    false,
                                    ListType.List,
                                    true
                                ),
                                "BarReferenceList"
                            )
                        }
                    ),
                    new TypeClassDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
                        new string[] { },
                        new[]
                        {
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "SomeText"
                            ),
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
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
                    new TypeClassDescriptor(
                        NamingConventions.EntityTypeNameFromTypeName("Foo"),
                        "EntityNamespace",
                        new string[] { },
                        new[]
                        {
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "SomeText"
                            ),
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    false,
                                    ListType.List,
                                    true
                                ),
                                "BarReferenceList"
                            ),
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    false,
                                    ListType.NoList,
                                    true
                                ),
                                "BarReference"
                            ),
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "SomeOtherText"
                            ),
                        }
                    ),
                    new TypeClassDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
                        new string[] { },
                        new[]
                        {
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "string",
                                    false,
                                    ListType.NoList,
                                    false
                                ),
                                "SomeText"
                            ),
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    false,
                                    ListType.List,
                                    true
                                ),
                                "BarReferenceList"
                            ),
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    false,
                                    ListType.NoList,
                                    true
                                ),
                                "BarReference"
                            ),
                            new TypeClassPropertyDescriptor(
                                new TypeDescriptor(
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
