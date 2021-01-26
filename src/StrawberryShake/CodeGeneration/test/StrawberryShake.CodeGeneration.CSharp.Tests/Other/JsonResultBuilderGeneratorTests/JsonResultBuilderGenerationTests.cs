using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using HotChocolate;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.CSharp;
using Xunit;
using static StrawberryShake.TestHelper;

namespace StrawberryShake.Other
{
    public partial class JsonResultBuilderGenerationTests
    {
        readonly StringBuilder _stringBuilder;
        readonly CodeWriter _codeWriter;
        readonly JsonResultBuilderGenerator _generator;

        public JsonResultBuilderGenerationTests()
        {
            _stringBuilder = new StringBuilder();
            _codeWriter = new CodeWriter(_stringBuilder);
            _generator = new JsonResultBuilderGenerator();
        }


        [Fact]
        public void GenerateResultBuilder_OneNullableEntityProperty()
        {
            _generator.Generate(
                _codeWriter,
                GetResultBuilderDescriptor(
                    new[]
                    {
                        new PropertyDescriptor(
                            "TheBar",
                            new NamedTypeDescriptor(
                                "Bar",
                                "BarNamespace",
                                false,
                                kind: TypeKind.EntityType,
                                graphQLTypeName: "BarType",
                                properties: new[] {GetNamedNullableStringTypeReference("name")}))
                    }));

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateResultBuilder_OneNonNullableEntityProperty()
        {
            _generator.Generate(
                _codeWriter,
                GetResultBuilderDescriptor(
                    new[]
                    {
                        new PropertyDescriptor(
                            "TheBar",
                            new NonNullTypeDescriptor(
                                new NamedTypeDescriptor(
                                    "Bar",
                                    "BarNamespace",
                                    false,
                                    kind: TypeKind.EntityType,
                                    graphQLTypeName: "BarType",
                                    properties: new[]
                                    {
                                        GetNamedNonNullStringTypeReference("name")
                                    })))
                    }));

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateResultBuilder_OneNullableEntityProperty_OneNullableEntityProperty()
        {
            _generator.Generate(
                _codeWriter,
                GetResultBuilderDescriptor(
                    new[]
                    {
                        new PropertyDescriptor(
                            "TheBar",
                            new NamedTypeDescriptor(
                                "Bar",
                                "BarNamespace",
                                false,
                                kind: TypeKind.EntityType,
                                graphQLTypeName: "BarType",
                                properties: new[]
                                {
                                    new PropertyDescriptor(
                                        "Baz",
                                        new NamedTypeDescriptor(
                                            "Baz",
                                            "BazNamespace",
                                            false,
                                            kind: TypeKind.EntityType,
                                            properties: new[]
                                            {
                                                GetNamedNullableStringTypeReference("Name")
                                            }))
                                }))
                    }));

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateResultBuilder_OneNullableDataProperty()
        {
            _generator.Generate(
                _codeWriter,
                GetResultBuilderDescriptor(
                    new[]
                    {
                        new PropertyDescriptor(
                            "TheBar",
                            new NamedTypeDescriptor(
                                "Bar",
                                "BarNamespace",
                                false,
                                kind: TypeKind.DataType,
                                graphQLTypeName: "BarType",
                                properties: new[] {GetNamedNullableStringTypeReference("name")}))
                    }));

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateResultBuilder_OneNonNullableDataProperty()
        {
            _generator.Generate(
                _codeWriter,
                GetResultBuilderDescriptor(
                    new[]
                    {
                        new PropertyDescriptor(
                            "TheBar",
                            new NonNullTypeDescriptor(
                                new NamedTypeDescriptor(
                                    "Bar",
                                    "BarNamespace",
                                    false,
                                    kind: TypeKind.DataType,
                                    graphQLTypeName: "BarType",
                                    properties: new[]
                                    {
                                        GetNamedNonNullStringTypeReference("name")
                                    })))
                    }));

            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateResultBuilder_OneNullableListOfNullableEntityProperty()
        {
            _generator.Generate(
                _codeWriter,
                GetResultBuilderDescriptor(
                    new[]
                    {
                        new PropertyDescriptor(
                            "Bars",
                            new ListTypeDescriptor(
                                new NamedTypeDescriptor(
                                    "Bar",
                                    "BarNamespace",
                                    false,
                                    graphQLTypeName: "BarType",
                                    kind: TypeKind.EntityType)))
                    }));

            _stringBuilder.ToString().MatchSnapshot();
        }

        private ResultBuilderDescriptor GetResultBuilderDescriptor(
            IReadOnlyList<PropertyDescriptor> properties)
        {
            return new ResultBuilderDescriptor(
                "FooResult",
                new NamedTypeDescriptor(
                    "IFoo",
                    "FooNamespace",
                    true,
                    implementedBy: new[]
                    {
                        new NamedTypeDescriptor(
                            "Foo",
                            "FooNamespace",
                            false,
                            properties: properties)
                    },
                    properties: properties),
                new[]
                {
                    new ValueParserDescriptor(
                        "string",
                        "string",
                        "String"),
                    new ValueParserDescriptor(
                        "int",
                        "int",
                        "Int")
                });
        }
    }
}
