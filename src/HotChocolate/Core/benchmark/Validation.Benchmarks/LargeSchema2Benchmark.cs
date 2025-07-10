using BenchmarkDotNet.Attributes;
using HotChocolate.Language;

namespace HotChocolate.Validation.Benchmarks;

[MemoryDiagnoser]
public class LargeSchema2Benchmark : ValidationBenchmarkBase
{
    protected override string SchemaDocumentFile => "__resources__/large-schema-2.graphqls";
    protected override string DocumentFile => "__resources__/large-schema-2-query.graphql";

    [Benchmark]
    public DocumentValidatorResult LargeSchema2Validation()
    {
        return Validator.Validate(
            schema: Schema,
            documentId: new OperationDocumentId("large-schema-2-query"),
            document: Document,
            onlyNonCacheable: false);
    }
}
