using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate.Language;

namespace HotChocolate.Validation.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
public class LargeSchema1Benchmark : ValidationBenchmarkBase
{
    protected override string SchemaDocumentFile => "__resources__/large-schema-1.graphqls";
    protected override string DocumentFile => "__resources__/large-schema-1-query.graphql";

    [Benchmark]
    public DocumentValidatorResult LargeSchema1Validation()
    {
        return Validator.Validate(
            schema: Schema,
            documentId: new OperationDocumentId("large-schema-1-query"),
            document: Document,
            onlyNonCacheable: false);
    }
}
