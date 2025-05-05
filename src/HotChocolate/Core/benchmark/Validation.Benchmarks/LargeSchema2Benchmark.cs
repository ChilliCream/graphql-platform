using BenchmarkDotNet.Attributes;
using HotChocolate.Execution;

namespace HotChocolate.Validation.Benchmarks;

[MemoryDiagnoser]
public class LargeSchema2Benchmark : ValidationBenchmarkBase
{
    protected override string SchemaDocumentFile => "__resources__/large-schema-2.graphqls";
    protected override string DocumentFile => "__resources__/large-schema-2-query.graphql";

    [Benchmark]
    public async Task<DocumentValidatorResult> LargeSchema2Validation()
    {
        return await Validator.ValidateAsync(
            schema: Schema,
            document: Document,
            documentId: new OperationDocumentId("large-schema-2-query"),
            contextData: new Dictionary<string, object?>(),
            onlyNonCacheable: false);
    }
}
