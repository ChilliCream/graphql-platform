using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate.Language;

namespace HotChocolate.Validation.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
public class ManyFragmentsBenchmark : ValidationBenchmarkBase
{
    protected override string SchemaDocumentFile => "__resources__/many-fragments.graphqls";
    protected override string DocumentFile => "__resources__/many-fragments-query.graphql";

    [Benchmark]
    public DocumentValidatorResult ManyFragmentsValidation()
    {
        return Validator.Validate(
            schema: Schema,
            documentId: new OperationDocumentId("many-fragments-query"),
            document: Document,
            onlyNonCacheable: false);
    }
}
