using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Tests.Other
{
    public class EntityTypeGenerationTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly EntityTypeGenerator _generator;

        public EntityTypeGenerationTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new EntityTypeGenerator();
        }

        [Fact]
        public async Task GenerateSimpleClassWithOneValueTypeProperty()
        {
            await _generator.WriteAsync(
                _codeWriter,
                new TypeDescriptor(
                    NamingConventions.EntityTypeNameFromTypeName("Foo"),
                    "FooBarNamespace",
                    new string[] { },
                    new[] {TestHelper.GetNamedNonNullStringTypeReference("SomeText")}
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
                        new NamedTypeReferenceDescriptor(
                            new TypeDescriptor(
                                "Bar",
                                "BarNamespace"
                            ),
                            false,
                            ListType.NoList,
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
                    NamingConventions.EntityTypeNameFromTypeName("Foo"),
                    "FooBarNamespace",
                    new string[] { },
                    new[]
                    {
                        TestHelper.GetNamedNonNullStringTypeReference("Id"), new NamedTypeReferenceDescriptor(
                            new TypeDescriptor(
                                "Bar",
                                "BarNamespace"
                            ),
                            false,
                            ListType.List,
                            "Bars"
                        ),
                        new NamedTypeReferenceDescriptor(
                            new TypeDescriptor(
                                "Baz",
                                "BazNamespace"
                            ),
                            true,
                            ListType.NoList,
                            "Baz"
                        ),
                    }
                )
            );
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
