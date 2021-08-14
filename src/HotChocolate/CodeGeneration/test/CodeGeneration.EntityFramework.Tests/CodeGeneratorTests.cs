using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HotChocolate.Language;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.CodeGeneration.EntityFramework
{
    public class CodeGeneratorTests
    {
        private static readonly Assembly _currentAssembly = Assembly.GetExecutingAssembly();
        private static readonly string _currentNamespace = typeof(CodeGeneratorTests).Namespace!;

        [Fact]
        public void Works()
        {
            // Arrange
            DocumentNode doc = GetDocumentFromFile("Schema");

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

        // TODO:
        // * test for PK behaviour

        private static DocumentNode GetDocumentFromFile(string fileName)
        {
            var resourceName = $"{_currentNamespace}.{fileName}.graphql";

            string content;
            try
            {
                using Stream? stream = _currentAssembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    throw new ArgumentException($"Couldn't read stream of embedded resource '{resourceName}'.");
                }

                using var streamReader = new StreamReader(stream);
                content = streamReader.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Couldn't read embedded resource '{resourceName}'.", ex);
            }

            return Utf8GraphQLParser.Parse(content);
        }
    }
}
