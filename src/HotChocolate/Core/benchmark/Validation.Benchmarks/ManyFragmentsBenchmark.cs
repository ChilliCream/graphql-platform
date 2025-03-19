// using BenchmarkDotNet.Attributes;
// using HotChocolate.Execution;
//
// namespace HotChocolate.Validation.Benchmarks;
//
// [MemoryDiagnoser]
// public class ManyFragmentsBenchmark : ValidationBenchmarkBase
// {
//     protected override string SchemaDocumentFile => "__resources__/many-fragments.graphqls";
//     protected override string DocumentFile => "__resources__/many-fragments-query.graphql";
//
//     [Benchmark]
//     public async Task<DocumentValidatorResult> ManyFragmentsValidation()
//     {
//         return await Validator.ValidateAsync(
//             schema: Schema,
//             document: Document,
//             documentId: new OperationDocumentId("many-fragments-query"),
//             contextData: new Dictionary<string, object?>(),
//             onlyNonCacheable: false);
//     }
// }
