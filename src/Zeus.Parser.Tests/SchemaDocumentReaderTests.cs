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
            Assert.Equal("Dog", dogType.Name);
            Assert.Equal(1, dogType.Interfaces.Count);
            Assert.Equal("Pet", dogType.Interfaces.First());
            Assert.Equal(3, dogType.Fields.Count);
            Assert.Equal("name", dogType.Fields["name"].Name);
            Assert.Equal("String!", dogType.Fields["name"].Type.ToString());
            Assert.Equal(0, dogType.Fields["name"].Arguments.Count);
            Assert.Equal("name", dogType.Fields["name"].Name);
            Assert.Equal("String!", dogType.Fields["name"].Type.ToString());
            Assert.Equal(0, dogType.Fields["name"].Arguments.Count);



            Assert.Equal("name", dogType.Fields["name"].Name);
            Assert.Equal("String!", dogType.Fields["name"].Type.ToString());
            Assert.Equal(0, dogType.Fields["name"].Arguments.Count);

        }
    }
}
