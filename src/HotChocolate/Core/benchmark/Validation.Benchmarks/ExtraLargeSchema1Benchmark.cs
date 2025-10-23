using BenchmarkDotNet.Attributes;
using HotChocolate.Language;

namespace HotChocolate.Validation.Benchmarks;

[MemoryDiagnoser]
public class ExtraLargeSchema1Benchmark : ValidationBenchmarkBase
{
    protected override string SchemaDocumentFile => "__resources__/extra-large-schema-1.graphqls";
    protected override string DocumentFile => "__resources__/extra-large-schema-1-query.graphql";

    [Benchmark]
    public DocumentValidatorResult ExtraLargeSchema1Validation()
    {
        return Validator.Validate(
            schema: Schema,
            documentId: new OperationDocumentId("extra-large-schema-1-query"),
            document: Document,
            onlyNonCacheable: false);
    }
}
