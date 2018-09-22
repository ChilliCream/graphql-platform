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

        public static ReadOnlyCollection<ArgumentSourceCodeGenerator> OnBeforeInvokeArguments { get; } =
            new List<ArgumentSourceCodeGenerator>(ResolverArguments)
            {
                new DirectiveArgumentSourceCodeGenerator(),
                new DirectiveArgumentArgumentSourceCodeGenerator(),
                new DirectiveObjectArgumentSourceCodeGenerator()
            }.AsReadOnly();

        public static ReadOnlyCollection<ArgumentSourceCodeGenerator> OnInvokeArguments { get; } =
            new List<ArgumentSourceCodeGenerator>(OnBeforeInvokeArguments)
            {
                new ResolverArgumentSourceCodeGenerator(),
                new OnInvokeResultArgumentSourceCodeGenerator(),
            }.AsReadOnly();

        public static ReadOnlyCollection<ArgumentSourceCodeGenerator> OnAfterInvokeArguments { get; } =
            new List<ArgumentSourceCodeGenerator>(OnBeforeInvokeArguments)
            {
                new ResultArgumentSourceCodeGenerator(),
            }.AsReadOnly();

    }
}
