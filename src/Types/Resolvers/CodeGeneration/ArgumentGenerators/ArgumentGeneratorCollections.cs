using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal static class ArgumentGeneratorCollections
    {
        public static ReadOnlyCollection<ArgumentSourceCodeGenerator> ResolverArguments { get; } =
            new List<ArgumentSourceCodeGenerator>()
            {
                new CancellationTokenArgumentSourceCodeGenerator(),
                new CustomContextArgumentSourceCodeGenerator(),
                new DataLoaderArgumentSourceCodeGenerator(),
                new ContextArgumentSourceCodeGenerator(),
                new SourceArgumentSourceCodeGenerator(),
                new ServiceArgumentSourceCodeGenerator(),
                new SchemaArgumentSourceCodeGenerator(),
                new QueryDocumentArgumentSourceCodeGenerator(),
                new OperationDefinitionArgumentSourceCodeGenerator(),
                new ObjectTypeArgumentSourceCodeGenerator(),
                new FieldSelectionArgumentSourceCodeGenerator(),
                new FieldArgumentSourceCodeGenerator(),
                new CustomArgumentSourceCodeGenerator()
            }.AsReadOnly();

        public static ReadOnlyCollection<ArgumentSourceCodeGenerator> MiddlewareArguments { get; } =
            new List<ArgumentSourceCodeGenerator>()
            {
                new CancellationTokenArgumentSourceCodeGenerator(),
                new CustomContextArgumentSourceCodeGenerator(),
                new DataLoaderArgumentSourceCodeGenerator(),
                new ContextArgumentSourceCodeGenerator(),
                new SourceArgumentSourceCodeGenerator(),
                new ServiceArgumentSourceCodeGenerator(),
                new SchemaArgumentSourceCodeGenerator(),
                new QueryDocumentArgumentSourceCodeGenerator(),
                new OperationDefinitionArgumentSourceCodeGenerator(),
                new ObjectTypeArgumentSourceCodeGenerator(),
                new FieldSelectionArgumentSourceCodeGenerator(),
                new FieldArgumentSourceCodeGenerator(),
                new CustomArgumentSourceCodeGenerator(),
                new ResolverArgumentSourceCodeGenerator(),
                new ResultArgumentSourceCodeGenerator(),
                new DirectiveArgumentSourceCodeGenerator(),
                new DirectiveObjectArgumentSourceCodeGenerator()
            }.AsReadOnly();
    }
}
