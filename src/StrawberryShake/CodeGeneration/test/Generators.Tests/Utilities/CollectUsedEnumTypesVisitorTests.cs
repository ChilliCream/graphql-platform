using System.Collections.Generic;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace StrawberryShake.Generators.Utilities
{
    public class CollectUsedEnumTypesVisitorTests
    {
        [Fact]
        public void Collect_EnumTypes_None_Found()
        {
            // arrange
            ISchema schema = CreateSchema();
            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                    {
                        droid(id: ""foo"") {
                            name
                        }
                    }
                ");

            // act
            IReadOnlyList<EnumType> enumTypes =
                CollectUsedEnumTypesVisitor.Collect(schema, document);

            // assert
            Assert.Empty(enumTypes);
        }

        [Fact]
        public void Collect_EnumTypes_Found_One_In_Argument()
        {
            // arrange
            ISchema schema = CreateSchema();
            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                    query getHero($e: Episode) {
                        hero(episode: $e) {
                            name
                        }
                    }
                ");

            // act
            IReadOnlyList<EnumType> enumTypes =
                CollectUsedEnumTypesVisitor.Collect(schema, document);

            // assert
            Assert.Collection(enumTypes,
                t => Assert.Equal("Episode", t.Name));
        }

        [Fact]
        public void Collect_EnumTypes_Found_One_In_Field_Return_Type()
        {
            // arrange
            ISchema schema = CreateSchema();
            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                    {
                        droid(id: ""foo"") {
                            appearsIn
                        }
                    }
                ");

            // act
            IReadOnlyList<EnumType> enumTypes =
                CollectUsedEnumTypesVisitor.Collect(schema, document);

            // assert
            Assert.Collection(enumTypes,
                t => Assert.Equal("Episode", t.Name));
        }

        private ISchema CreateSchema()
        {
            return SchemaBuilder.New()
                .Use(next => context => Task.CompletedTask)
                .AddDocumentFromString(FileResource.Open("StarWars.graphql"))
                .Create();
        }
    }

}
