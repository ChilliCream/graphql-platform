using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Other
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
                            TestHelper.GetNamedNonNullStringTypeReference("SomeText"),
                            TestHelper.GetNamedNonNullStringTypeReference("OtherProperty"),
                        }
                    ),
                    new TypeDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
                        new string[] { },
                        new[] {TestHelper.GetNamedNonNullStringTypeReference("SomeText"),}
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
                            TestHelper.GetNamedNonNullStringTypeReference("SomeText"),
                            TestHelper.GetNamedNonNullStringTypeReference("OtherProperty"),
                        }
                    ),
                    new TypeDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
                        new string[] { },
                        new[]
                        {
                            TestHelper.GetNamedNonNullStringTypeReference("SomeText"),
                            TestHelper.GetNamedNonNullStringTypeReference("OtherProperty"),
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
                            TestHelper.GetNamedNonNullStringTypeReference("SomeText"), new NamedTypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    "BarNamespace"
                                ),
                                false,
                                ListType.NoList,
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
                            new NamedTypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    "BarNamespace"
                                ),
                                false,
                                ListType.NoList,
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
                            TestHelper.GetNamedNonNullStringTypeReference("SomeText"), new NamedTypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    "BarNamespace"
                                ),
                                false,
                                ListType.List,
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
                            TestHelper.GetNamedNonNullStringTypeReference("SomeText"), new NamedTypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    "BarNamespace",
                                    isEntityType: true
                                ),
                                false,
                                ListType.List,
                                "BarEntityList"
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
                            TestHelper.GetNamedNonNullStringTypeReference("SomeText"), new NamedTypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    "BarNamespace"
                                ),
                                true,
                                ListType.List,
                                "BarReferenceList"
                            ),
                            new NamedTypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    "BarNamespace"
                                ),
                                true,
                                ListType.NoList,
                                "BarReference"
                            ),
                            TestHelper.GetNamedNonNullStringTypeReference("SomeOtherText")
                        }
                    ),
                    new TypeDescriptor(
                        "Foo",
                        "ResultTypeNamespace",
                        new string[] { },
                        new[]
                        {
                            TestHelper.GetNamedNonNullStringTypeReference("SomeText"), new NamedTypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    "BarNamespace"
                                ),
                                false,
                                ListType.List,
                                "BarReferenceList"
                            ),
                            new NamedTypeReferenceDescriptor(
                                new TypeDescriptor(
                                    "Bar",
                                    "BarNamespace"
                                ),
                                false,
                                ListType.NoList,
                                "BarReference"
                            ),
                            TestHelper.GetNamedNonNullStringTypeReference("SomeOtherText")
                        }
                    )
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
