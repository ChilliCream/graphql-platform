using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration;
using Xunit;

namespace StrawberryShake.Other
{
    public partial class ResultTypeGenerationTests
    {
        [Fact]
        public void GenerateEntity_NullableListOfNullable()
        {
            _generator.Generate(
                _codeWriter,
                new NamedTypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    false,
                    kind: TypeKind.EntityType,
                    properties: new[]
                    {
                        new PropertyDescriptor(
                            "Bars",
                            new ListTypeDescriptor(
                                new NamedTypeDescriptor(
                                    "Bar",
                                    "BarNamespace",
                                    false,
                                    kind: TypeKind.EntityType))),
                    }),
                out _);
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateEntity_NullableListOfNonNullable()
        {
            _generator.Generate(
                _codeWriter,
                new NamedTypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    false,
                    kind: TypeKind.EntityType,
                    properties: new[]
                    {
                        new PropertyDescriptor(
                            "Bars",
                            new ListTypeDescriptor(
                                new NonNullTypeDescriptor(
                                    new NamedTypeDescriptor(
                                        "Bar",
                                        "BarNamespace",
                                        false,
                                        kind: TypeKind.EntityType)))),
                    }),
                out _);
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateEntity_NonNullableListOfNullable()
        {
            _generator.Generate(
                _codeWriter,
                new NamedTypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    false,
                    kind: TypeKind.EntityType,
                    properties: new[]
                    {
                        new PropertyDescriptor(
                            "Bars",
                            new NonNullTypeDescriptor(
                                new ListTypeDescriptor(
                                    new NamedTypeDescriptor(
                                        "Bar",
                                        "BarNamespace",
                                        false,
                                        kind: TypeKind.EntityType)))),
                    }),
                out _);
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateEntity_NonNullableListOfNonNullable()
        {
            _generator.Generate(
                _codeWriter,
                new NamedTypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    false,
                    kind: TypeKind.EntityType,
                    properties: new[]
                    {
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
                    }),
                out _);
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateEntity_NullableListOfNullableListOfNullable()
        {
            _generator.Generate(
                _codeWriter,
                new NamedTypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    false,
                    kind: TypeKind.EntityType,
                    properties: new[]
                    {
                        new PropertyDescriptor(
                            "Bars",
                            new ListTypeDescriptor(
                                new ListTypeDescriptor(
                                    new NamedTypeDescriptor(
                                        "Bar",
                                        "BarNamespace",
                                        false,
                                        kind: TypeKind.EntityType)))),
                    }),
                out _);
            _stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void GenerateEntity_NonNullableListOfNonNullableListOfNonNullable()
        {
            _generator.Generate(
                _codeWriter,
                new NamedTypeDescriptor(
                    "Foo",
                    "FooBarNamespace",
                    false,
                    kind: TypeKind.EntityType,
                    properties: new[]
                    {
                        new PropertyDescriptor(
                            "Bars",
                            new NonNullTypeDescriptor(
                                new ListTypeDescriptor(
                                    new NonNullTypeDescriptor(
                                        new ListTypeDescriptor(
                                            new NonNullTypeDescriptor(
                                                new NamedTypeDescriptor(
                                                    "Bar",
                                                    "BarNamespace",
                                                    false,
                                                    kind: TypeKind.EntityType))))))),
                    }),
                out _);
            _stringBuilder.ToString().MatchSnapshot();
        }
    }
}
