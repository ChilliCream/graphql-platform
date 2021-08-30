using System.Collections.Generic;
using HotChocolate.Language;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.CodeGeneration.EntityFramework.Key
{
    public class KeyTests
    {
        private static readonly string _currentNamespace = typeof(KeyTests).Namespace!;

        [Fact]
        public void Works()
        {
            // Arrange
            DocumentNode doc = SchemaLoader.GetDocumentFromFile(_currentNamespace, "Schema");

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
