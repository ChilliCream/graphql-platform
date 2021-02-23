using System.Text;
using System.Threading.Tasks;
using HotChocolate;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;

namespace StrawberryShake.Other
{
    public partial class ResultTypeGenerationTests
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
        public void GenerateEntityWithOneValueTypeProperty()
        {
            _generator.Generate(
                _codeWriter,
                new NamedTypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    false,
                    properties: new[]
                    {
                        TestHelper.GetNamedNonNullStringTypeReference("SomeText"),
                    }),
                out _);
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateEntityWithOneReferenceTypeProperty()
        {
            _generator.Generate(
                _codeWriter,
                new NamedTypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    false,
                    new NameString[] { },
                    new[]
                    {
                        new PropertyDescriptor(
                            "Bar",
                            new NonNullTypeDescriptor(
                                new NamedTypeDescriptor(
                                    "Bar",
                                    "BarNamespace",
                                    false,
                                    kind: TypeKind.EntityType)))
                    }),
                out _);
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateEntityWithOneNullableValueTypeProperty()
        {
            _generator.Generate(
                _codeWriter,
                new NamedTypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    false,
                    properties: new[]
                    {
                        TestHelper.GetNamedNullableStringTypeReference("SomeText"),
                    }),
                out _);
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateEntityWithOneNullableReferenceTypeProperty()
        {
            _generator.Generate(
                _codeWriter,
                new NamedTypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    false,
                    new NameString[] { },
                    new[]
                    {
                        new PropertyDescriptor(
                            "Bar",
                            new NamedTypeDescriptor(
                                "Bar",
                                "BarNamespace",
                                false,
                                kind: TypeKind.EntityType))
                    }),
                out _);
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateEntityWithImplements()
        {
            _generator.Generate(
                _codeWriter,
                new NamedTypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    false,
                    new NameString[] {"IFoo", "IBar"},
                    new[]
                    {
                        new PropertyDescriptor(
                            "Bar",
                            new NonNullTypeDescriptor(
                                new NamedTypeDescriptor(
                                    "Bar",
                                    "BarNamespace",
                                    false,
                                    kind: TypeKind.EntityType)))
                    }),
                out _);
            _stringBuilder.ToString().MatchSnapshot();
        }


        [Fact]
        public void GenerateEntityWithMultipleProperties()
        {
            _generator.Generate(
                _codeWriter,
                new NamedTypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    false,
                    properties: new[]
                    {
                        TestHelper.GetNamedNonNullStringTypeReference("Id"),
                        new PropertyDescriptor(
                            "Bars",
                            new NonNullTypeDescriptor(
                                new ListTypeDescriptor(
                                    new NonNullTypeDescriptor(
                                        new NamedTypeDescriptor(
                                            "Bar",
                                            "BarNamespace",
                                            false,
                                            kind: TypeKind.EntityType))))),
                        new PropertyDescriptor(
                            "Baz",
                            new NamedTypeDescriptor(
                                "Baz",
                                "BazNamespace",
                                false,
                                kind: TypeKind.EntityType))
                    }),
                out _);
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
