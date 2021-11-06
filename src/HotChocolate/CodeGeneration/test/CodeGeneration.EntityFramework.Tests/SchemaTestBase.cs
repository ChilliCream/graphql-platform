using System.Collections.Generic;
using HotChocolate.Language;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.CodeGeneration.EntityFramework
{
    public abstract class SchemaTestBase
    {
        private readonly string _currentNamespace;

        public SchemaTestBase()
        {
            _currentNamespace = GetType().Namespace!;
        }

        public CodeGeneratorContext Context
        {
            get
            {
                DocumentNode doc = SchemaLoader.GetDocumentFromFile(_currentNamespace, "Schema");

                var docs = new List<DocumentNode>() { doc };

                return new CodeGeneratorContext(
                    "MyEFCore",
                    "EFCoreDatabase",
                    "CompanyName.EFCore",
                    docs);
            }
        }

        protected void WorksImpl()
        {
            // Arrange
            // ...

            // Act
            CodeGenerationResult? result = new EntityFrameworkCodeGenerator().Generate(Context);

            // Assert
            foreach (SourceFile sourceFile in result.SourceFiles)
            {
                Snapshot.Match(sourceFile.Source, new SnapshotNameExtension(sourceFile.Name));
            }
        }

        protected abstract void Works();
    }
}
