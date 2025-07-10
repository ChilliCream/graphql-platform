// using BenchmarkDotNet.Attributes;
// using HotChocolate.Language;
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
//     public DocumentValidatorResult ManyFragmentsValidation()
//     {
//         return Validator.Validate(
//             schema: Schema,
//             documentId: new OperationDocumentId("many-fragments-query"),
//             document: Document,
//             onlyNonCacheable: false);
//     }
// }
