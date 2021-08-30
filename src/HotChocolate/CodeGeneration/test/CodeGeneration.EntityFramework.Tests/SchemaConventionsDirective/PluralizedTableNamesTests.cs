using System.Collections.Generic;
using HotChocolate.Language;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.CodeGeneration.EntityFramework.SchemaConventionsDirective
{
    public class PluralizedTableNamesTests
    {
        [Fact]
        public void Respects_UsePluralizedTableNames()
        {
            // Arrange
            DocumentNode doc = Utf8GraphQLParser.Parse(@"
                schema
                    @schemaConventions(usePluralizedTableNames: false)
                {
                    query: Query
                }

                type Query {
                    booking: Booking
                }

                type Booking {
                    id: Int!
                }");

            var docs = new List<DocumentNode>() { doc };

            var context = new CodeGeneratorContext(
                "MyEFCore",
                "EFCoreDatabase",
                "CompanyName.EFCore",
                docs);

            // Act
            CodeGenerationResult? result = new EntityFrameworkCodeGenerator().Generate(context);

            // Assert
            foreach (SourceFile sourceFile in result.SourceFiles)
            {
                Snapshot.Match(sourceFile.Source, new SnapshotNameExtension(sourceFile.Name));
            }
        }

        [Fact]
        public void Respects_TableDirective()
        {
            // Arrange
            DocumentNode doc = Utf8GraphQLParser.Parse(@"
                type Booking @table(name: ""BookingCustom"") {
                    id: Int!
                }");

            var docs = new List<DocumentNode>() { doc };

            var context = new CodeGeneratorContext(
                "MyEFCore",
                "EFCoreDatabase",
                "CompanyName.EFCore",
                docs);

            // Act
            CodeGenerationResult? result = new EntityFrameworkCodeGenerator().Generate(context);

            // Assert
            foreach (SourceFile sourceFile in result.SourceFiles)
            {
                Snapshot.Match(sourceFile.Source, new SnapshotNameExtension(sourceFile.Name));
            }
        }
    }
}
