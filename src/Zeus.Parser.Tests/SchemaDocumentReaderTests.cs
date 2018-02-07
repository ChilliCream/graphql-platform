using System;
using System.Linq;
using Xunit;
using Zeus.Abstractions;

namespace Zeus.Parser.Tests
{
    public class SchemaDocumentReaderTests
    {
        [Fact]
        public void ReadSchemaAndCheckIfAllElementsExist()
        {
            // arrange
            SchemaDocumentReader reader = new SchemaDocumentReader();

            // act
            SchemaDocument schema = reader.Read(Constants.DefaultSchema);

            // assert
            Assert.True(schema.InterfaceTypes.ContainsKey("Pet"));
            Assert.True(schema.ObjectTypes.ContainsKey("Dog"));
            Assert.True(schema.ObjectTypes.ContainsKey("Flee"));
            Assert.True(schema.InputObjectTypes.ContainsKey("VisitingPetInput"));
            Assert.True(schema.EnumTypes.ContainsKey("PetType"));
            Assert.True(schema.UnionTypes.ContainsKey("PetResult"));
            Assert.NotNull(schema.QueryType);
            Assert.NotNull(schema.MutationType);
        }

        [Fact]
        public void ReadSchemaAndValidateObjectTypes()
        {
            // arrange
            SchemaDocumentReader reader = new SchemaDocumentReader();

            // act
            SchemaDocument schema = reader.Read(Constants.DefaultSchema);

            // assert
            ObjectTypeDefinition dogType = schema.ObjectTypes["Dog"];

            // dog type
            Assert.Equal("Dog", dogType.Name);
            Assert.Equal(1, dogType.Interfaces.Count);
            Assert.Equal("Pet", dogType.Interfaces.First());
            Assert.Equal(3, dogType.Fields.Count);

            // dog type fields
            Assert.Equal("name", dogType.Fields["name"].Name);
            Assert.Equal(NamedType.String, dogType.Fields["name"].Type.InnerType());
            Assert.True(dogType.Fields["name"].Type.IsNonNullType());
            Assert.Equal(0, dogType.Fields["name"].Arguments.Count);

            Assert.Equal("flees", dogType.Fields["flees"].Name);
            Assert.Equal(new NamedType("Flee"), dogType.Fields["flees"].Type.ElementType().InnerType());
            Assert.True(dogType.Fields["flees"].Type.IsListType());
            Assert.True(dogType.Fields["flees"].Type.IsNonNullElementType());
            Assert.Equal(1, dogType.Fields["flees"].Arguments.Count);
            Assert.True(dogType.Fields["flees"].Arguments.ContainsKey("max"));
            Assert.Equal(NamedType.Integer, dogType.Fields["flees"].Arguments["max"].Type);
            Assert.Equal("10", dogType.Fields["flees"].Arguments["max"].DefaultValue.ToString());

            Assert.Equal("barks", dogType.Fields["barks"].Name);
            Assert.Equal(new NamedType("Boolean"), dogType.Fields["barks"].Type.InnerType());
            Assert.True(dogType.Fields["barks"].Type.IsNonNullType());
            Assert.Equal(1, dogType.Fields["barks"].Arguments.Count);
            Assert.True(dogType.Fields["barks"].Arguments.ContainsKey("visit"));
            Assert.Equal(new NamedType("VisitingPetInput"), dogType.Fields["barks"].Arguments["visit"].Type);
            Assert.Null(dogType.Fields["barks"].Arguments["visit"].DefaultValue);
        }
    }
}
