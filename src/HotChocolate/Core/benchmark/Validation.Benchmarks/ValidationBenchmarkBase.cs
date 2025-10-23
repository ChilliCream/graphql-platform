using BenchmarkDotNet.Attributes;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation.Benchmarks;

public abstract class ValidationBenchmarkBase
{
    protected abstract string SchemaDocumentFile { get; }
    protected abstract string DocumentFile { get; }

    protected Schema Schema = null!;
    protected DocumentValidator Validator = null!;
    protected DocumentNode Document = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var schemaDocument = Utf8GraphQLParser.Parse(File.ReadAllText(SchemaDocumentFile));
        var schemaBuilder = SchemaBuilder.New();

        // Register stubs for custom scalars.
        var customScalars = schemaDocument.Definitions.OfType<ScalarTypeDefinitionNode>()
            .Where(s => !Scalars.IsBuiltIn(s.Name.Value));

        foreach (var scalar in customScalars)
        {
            schemaBuilder.AddType(new AnyType(scalar.Name.Value));
        }

        Schema = schemaBuilder
            .AddDocument(schemaDocument)
            .Use(next => next)
            .Create();

        Validator = DocumentValidatorBuilder.New().AddDefaultRules().Build();
        Document = Utf8GraphQLParser.Parse(File.ReadAllText(DocumentFile));
    }
}
