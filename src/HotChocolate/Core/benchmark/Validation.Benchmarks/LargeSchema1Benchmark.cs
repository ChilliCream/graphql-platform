using BenchmarkDotNet.Attributes;
using HotChocolate.Execution;

namespace HotChocolate.Validation.Benchmarks;

[MemoryDiagnoser]
public class LargeSchema1Benchmark : ValidationBenchmarkBase
{
    protected override string SchemaDocumentFile => "__resources__/large-schema-1.graphqls";
    protected override string DocumentFile => "__resources__/large-schema-1-query.graphql";

    [Benchmark]
    public async Task<DocumentValidatorResult> LargeSchema1Validation()
    {
        return await Validator.ValidateAsync(
            schema: Schema,
            document: Document,
            documentId: new OperationDocumentId("large-schema-1-query"),
            contextData: new Dictionary<string, object?>(),
            onlyNonCacheable: false);
    }
}
