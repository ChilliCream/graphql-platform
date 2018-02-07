using System;
using System.Linq;
using Xunit;

namespace Zeus.Abstractions.Tests
{
    public class SchemaObjectTests
    {
        [Fact]
        public void EnumTypeDefinitionTest()
        {
            // act
            EnumTypeDefinition typeDefinition = new EnumTypeDefinition("Foo", new[] { "A", "B" });

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal("A", typeDefinition.Values.First());
            Assert.Equal("B", typeDefinition.Values.Last());
            Assert.Equal(2, typeDefinition.Values.Count);

            string expectedStringRepresentation = $"enum Foo{Environment.NewLine}{{{Environment.NewLine}  A{Environment.NewLine}  B{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

        [Fact]
        public void InputObjectTypeDefinitionWithOneInputFieldTest()
        {
            // act
            InputObjectTypeDefinition typeDefinition = new InputObjectTypeDefinition(
                "Foo", new[] { new InputValueDefinition("a", new NamedType("String"), null) });

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal(1, typeDefinition.Fields.Count);
            Assert.True(typeDefinition.Fields.ContainsKey("a"));
            Assert.Equal("a", typeDefinition.Fields["a"].Name);
            Assert.Equal(new NamedType("String"), typeDefinition.Fields["a"].Type);
            Assert.Null(typeDefinition.Fields["a"].DefaultValue);

            string expectedStringRepresentation = $"input Foo{Environment.NewLine}{{{Environment.NewLine}  a: String{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

        [Fact]
        public void InputObjectTypeDefinitionWithTwoInputFieldsTest()
        {
            // act
            InputObjectTypeDefinition typeDefinition = new InputObjectTypeDefinition(
                "Foo", new[] {
                    new InputValueDefinition("a", new NamedType("String"), null),
                    new InputValueDefinition("b", new NamedType("Int"), null) });

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal(1, typeDefinition.Fields.Count);
            Assert.True(typeDefinition.Fields.ContainsKey("a"));
            Assert.Equal("a", typeDefinition.Fields["a"].Name);
            Assert.Equal(new NamedType("String"), typeDefinition.Fields["a"].Type);
            Assert.Null(typeDefinition.Fields["a"].DefaultValue);

            string expectedStringRepresentation = $"input Foo{Environment.NewLine}{{{Environment.NewLine}  a: String{Environment.NewLine}  b: Int{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

        [Fact]
        public void FieldDefinitionWithoutArgumentTest()
        {
            // act
            FieldDefinition fieldDefinition = new FieldDefinition(
                "foo", new NamedType("String"), null);

            // assert
            Assert.Equal("foo", fieldDefinition.Name);
            Assert.Equal(new NamedType("String"), fieldDefinition.Type);
            Assert.Empty(fieldDefinition.Arguments);

            string expectedStringRepresentation = $"foo: String";
            Assert.Equal(expectedStringRepresentation, fieldDefinition.ToString());
        }

        [Fact]
        public void FieldDefinitionWithOneArgumentTest()
        {
            // act
            FieldDefinition fieldDefinition = new FieldDefinition(
                "foo", new NamedType("String"),
                new[] { new InputValueDefinition("a", new NamedType("Int"), null) });

            // assert
            Assert.Equal("foo", fieldDefinition.Name);
            Assert.Equal(new NamedType("String"), fieldDefinition.Type);
            Assert.Equal(1, fieldDefinition.Arguments.Count);

            string expectedStringRepresentation = $"foo(a: Int): String";
            Assert.Equal(expectedStringRepresentation, fieldDefinition.ToString());
        }

        [Fact]
        public void FieldDefinitionWithTwoArgumentsTest()
        {
            // act
            FieldDefinition fieldDefinition = new FieldDefinition(
                "foo", new NamedType("String"),
                new[] {
                    new InputValueDefinition("a", new NamedType("Int"), null),
                    new InputValueDefinition("b", new NamedType("Boolean"), null) });

            // assert
            Assert.Equal("foo", fieldDefinition.Name);
            Assert.Equal(new NamedType("String"), fieldDefinition.Type);
            Assert.Equal(2, fieldDefinition.Arguments.Count);

            string expectedStringRepresentation = $"foo(a: Int, b: Boolean): String";
            Assert.Equal(expectedStringRepresentation, fieldDefinition.ToString());
        }

        [Fact]
        public void FieldDefinitionWithTwoArgumentsAndDefaultValueTest()
        {
            // act
            FieldDefinition fieldDefinition = new FieldDefinition(
                "foo", new NamedType("String"),
                new[] {
                    new InputValueDefinition("a", new NamedType("Int"), new IntegerValue(1)),
                    new InputValueDefinition("b", new NamedType("Boolean"), null) });

            // assert
            Assert.Equal("foo", fieldDefinition.Name);
            Assert.Equal(new NamedType("String"), fieldDefinition.Type);
            Assert.Equal(2, fieldDefinition.Arguments.Count);

            string expectedStringRepresentation = $"foo(a: Int = 1, b: Boolean): String";
            Assert.Equal(expectedStringRepresentation, fieldDefinition.ToString());
        }

        [Fact]
        public void InterfaceTypeDefinitionWithOneFieldTest()
        {
            // act
            InterfaceTypeDefinition typeDefinition = new InterfaceTypeDefinition(
                "Foo", new[] { new FieldDefinition("a", NamedType.String, null) });

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal(1, typeDefinition.Fields.Count);
            Assert.True(typeDefinition.Fields.ContainsKey("a"));
            Assert.Equal("a", typeDefinition.Fields["a"].Name);
            Assert.Equal(NamedType.String, typeDefinition.Fields["a"].Type);
            Assert.Equal(0, typeDefinition.Fields["a"].Arguments.Count);

            string expectedStringRepresentation = $"interface Foo{Environment.NewLine}{{{Environment.NewLine}  a: String{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

        [Fact]
        public void InterfaceTypeDefinitionWithOneFieldWithArgumentTest()
        {
            // act
            InterfaceTypeDefinition typeDefinition = new InterfaceTypeDefinition(
                "Foo", new[] { new FieldDefinition("a", NamedType.String,
                    new[] { new InputValueDefinition("b", NamedType.Integer) }) });

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal(1, typeDefinition.Fields.Count);
            Assert.True(typeDefinition.Fields.ContainsKey("a"));
            Assert.Equal("a", typeDefinition.Fields["a"].Name);
            Assert.Equal(NamedType.String, typeDefinition.Fields["a"].Type);
            Assert.Equal(1, typeDefinition.Fields["a"].Arguments.Count);

            string expectedStringRepresentation = $"interface Foo{Environment.NewLine}{{{Environment.NewLine}  a(b: Int): String{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

        [Fact]
        public void InterfaceTypeDTypeDefinitionWithTwoInputFieldsTest()
        {
            // act
            InterfaceTypeDefinition typeDefinition = new InterfaceTypeDefinition(
                "Foo", new[] {
                    new FieldDefinition("a", NamedType.String),
                    new FieldDefinition("b", NamedType.Integer) });

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal(2, typeDefinition.Fields.Count);
            Assert.True(typeDefinition.Fields.ContainsKey("a"));
            Assert.Equal("a", typeDefinition.Fields["a"].Name);
            Assert.Equal(new NamedType("String"), typeDefinition.Fields["a"].Type);
            Assert.Equal(0, typeDefinition.Fields["a"].Arguments.Count);

            string expectedStringRepresentation = $"interface Foo{Environment.NewLine}{{{Environment.NewLine}  a: String{Environment.NewLine}  b: Int{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

        [InlineData("Int")]
        [InlineData("String")]
        [Theory]
        public void ListTypeTest(string typeName)
        {
            // act
            IType listTyp = new ListType(new NamedType(typeName));
            IType nonNullListTyp = new NonNullType(new ListType(new NamedType(typeName)));
            IType nonNullElementListTyp = new ListType(new NonNullType(new NamedType(typeName)));
            IType nonNullElementNonNullListTyp = new NonNullType(new ListType(new NonNullType(new NamedType(typeName))));

            // assert
            Assert.Equal($"[{typeName}]", listTyp.ToString());
            Assert.Equal(new NamedType(typeName), listTyp.ElementType());
            Assert.True(listTyp.IsListType());
            Assert.False(listTyp.ElementType().IsListType());
            Assert.False(listTyp.IsNonNullType());
            Assert.False(listTyp.IsNonNullElementType());
            Assert.False(listTyp.IsScalarType());

            Assert.Equal($"[{typeName}]!", nonNullListTyp.ToString());
            Assert.Equal(new NamedType(typeName), nonNullListTyp.ElementType());
            Assert.True(nonNullListTyp.IsListType());
            Assert.False(nonNullListTyp.ElementType().IsListType());
            Assert.True(nonNullListTyp.IsNonNullType());
            Assert.False(nonNullListTyp.IsNonNullElementType());
            Assert.False(nonNullListTyp.IsScalarType());

            Assert.Equal($"[{typeName}!]", nonNullElementListTyp.ToString());
            Assert.Equal(new NonNullType(new NamedType(typeName)), nonNullElementListTyp.ElementType());
            Assert.Equal(new NamedType(typeName), nonNullElementListTyp.ElementType().InnerType());
            Assert.True(nonNullElementListTyp.IsListType());
            Assert.False(nonNullElementListTyp.ElementType().IsListType());
            Assert.False(nonNullElementListTyp.IsNonNullType());
            Assert.True(nonNullElementListTyp.IsNonNullElementType());
            Assert.False(nonNullElementListTyp.IsScalarType());

            Assert.Equal($"[{typeName}!]!", nonNullElementNonNullListTyp.ToString());
            Assert.Equal(new NonNullType(new NamedType(typeName)), nonNullElementNonNullListTyp.ElementType());
            Assert.Equal(new NamedType(typeName), nonNullElementNonNullListTyp.ElementType().InnerType());
            Assert.True(nonNullElementNonNullListTyp.IsListType());
            Assert.False(nonNullElementNonNullListTyp.ElementType().IsListType());
            Assert.True(nonNullElementNonNullListTyp.IsNonNullType());
            Assert.True(nonNullElementNonNullListTyp.IsNonNullElementType());
            Assert.False(nonNullElementNonNullListTyp.IsScalarType());
        }

        [InlineData("Foo", false)]
        [InlineData("String", true)]
        [Theory]
        public void NamedTypeTest(string typeName, bool isScalarType)
        {
            // act
            IType namedType = new NamedType(typeName);
            IType nonNullNamedType = new NonNullType(new NamedType(typeName));

            // assert
            Assert.Equal($"{typeName}", namedType.ToString());
            Assert.Equal(new NamedType(typeName), namedType.InnerType());
            Assert.False(namedType.IsListType());
            Assert.False(namedType.IsNonNullType());
            Assert.False(namedType.IsNonNullElementType());
            Assert.Equal(isScalarType, namedType.IsScalarType());

            Assert.Equal($"{typeName}!", nonNullNamedType.ToString());
            Assert.Equal(new NamedType(typeName), nonNullNamedType.InnerType());
            Assert.False(nonNullNamedType.IsListType());
            Assert.True(nonNullNamedType.IsNonNullType());
            Assert.False(nonNullNamedType.IsNonNullElementType());
            Assert.Equal(isScalarType, nonNullNamedType.IsScalarType());
        }

        [Fact]
        public void ObjectTypeDefinitionWithOneFieldTest()
        {
            // act
            ObjectTypeDefinition typeDefinition = new ObjectTypeDefinition(
                "Foo", new[] { new FieldDefinition("a", NamedType.String, null) });

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal(0, typeDefinition.Interfaces.Count);
            Assert.Equal(1, typeDefinition.Fields.Count);
            Assert.True(typeDefinition.Fields.ContainsKey("a"));
            Assert.Equal("a", typeDefinition.Fields["a"].Name);
            Assert.Equal(NamedType.String, typeDefinition.Fields["a"].Type);
            Assert.Equal(0, typeDefinition.Fields["a"].Arguments.Count);

            string expectedStringRepresentation = $"type Foo{Environment.NewLine}{{{Environment.NewLine}  a: String{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

        [Fact]
        public void ObjectTypeDefinitionWithOneFieldAndInterfaceTest()
        {
            // act
            ObjectTypeDefinition typeDefinition = new ObjectTypeDefinition(
                "Foo", new[] { new FieldDefinition("a", NamedType.String, null) },
                new[] { "IFoo" });

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal(1, typeDefinition.Interfaces.Count);
            Assert.True(typeDefinition.Interfaces.Contains("IFoo"));
            Assert.Equal(1, typeDefinition.Fields.Count);
            Assert.True(typeDefinition.Fields.ContainsKey("a"));
            Assert.Equal("a", typeDefinition.Fields["a"].Name);
            Assert.Equal(NamedType.String, typeDefinition.Fields["a"].Type);
            Assert.Equal(0, typeDefinition.Fields["a"].Arguments.Count);

            string expectedStringRepresentation = $"type Foo implements IFoo{Environment.NewLine}{{{Environment.NewLine}  a: String{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

        [Fact]
        public void ObjectTypeDefinitionWithOneFieldAndTwoInterfacesTest()
        {
            // act
            ObjectTypeDefinition typeDefinition = new ObjectTypeDefinition(
                "Foo", new[] { new FieldDefinition("a", NamedType.String, null) },
                new[] { "IFooA", "IFooB" });

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal(2, typeDefinition.Interfaces.Count);
            Assert.True(typeDefinition.Interfaces.Contains("IFooA"));
            Assert.True(typeDefinition.Interfaces.Contains("IFooB"));
            Assert.Equal(1, typeDefinition.Fields.Count);
            Assert.True(typeDefinition.Fields.ContainsKey("a"));
            Assert.Equal("a", typeDefinition.Fields["a"].Name);
            Assert.Equal(NamedType.String, typeDefinition.Fields["a"].Type);
            Assert.Equal(0, typeDefinition.Fields["a"].Arguments.Count);

            string expectedStringRepresentation = $"type Foo implements IFooA, IFooB{Environment.NewLine}{{{Environment.NewLine}  a: String{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

        [Fact]
        public void ObjectTypeDefinitionWithOneFieldWithArgumentTest()
        {
            // act
            ObjectTypeDefinition typeDefinition = new ObjectTypeDefinition(
                "Foo", new[] { new FieldDefinition("a", NamedType.String,
                    new[] { new InputValueDefinition("b", NamedType.Integer) }) });

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal(0, typeDefinition.Interfaces.Count);
            Assert.Equal(1, typeDefinition.Fields.Count);
            Assert.True(typeDefinition.Fields.ContainsKey("a"));
            Assert.Equal("a", typeDefinition.Fields["a"].Name);
            Assert.Equal(NamedType.String, typeDefinition.Fields["a"].Type);
            Assert.Equal(1, typeDefinition.Fields["a"].Arguments.Count);

            string expectedStringRepresentation = $"type Foo{Environment.NewLine}{{{Environment.NewLine}  a(b: Int): String{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

        [Fact]
        public void ObjectTypeTypeDefinitionWithTwoInputFieldsTest()
        {
            // act
            ObjectTypeDefinition typeDefinition = new ObjectTypeDefinition(
                "Foo", new[] {
                    new FieldDefinition("a", NamedType.String),
                    new FieldDefinition("b", NamedType.Integer) });

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal(0, typeDefinition.Interfaces.Count);
            Assert.Equal(2, typeDefinition.Fields.Count);
            Assert.True(typeDefinition.Fields.ContainsKey("a"));
            Assert.Equal("a", typeDefinition.Fields["a"].Name);
            Assert.Equal(new NamedType("String"), typeDefinition.Fields["a"].Type);
            Assert.Equal(0, typeDefinition.Fields["a"].Arguments.Count);

            string expectedStringRepresentation = $"type Foo{Environment.NewLine}{{{Environment.NewLine}  a: String{Environment.NewLine}  b: Int{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

        [Fact]
        public void UnionTypeDefinitionTest()
        {
            // act
            UnionTypeDefinition typeDefinition = new UnionTypeDefinition("Foo", NamedType.String, NamedType.Boolean);

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal(2, typeDefinition.Types.Count);
            Assert.True(typeDefinition.Types.Contains(NamedType.String));
            Assert.True(typeDefinition.Types.Contains(NamedType.Boolean));

            string expectedStringRepresentation = $"union Foo = {NamedType.String} | {NamedType.Boolean}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

    }
}
