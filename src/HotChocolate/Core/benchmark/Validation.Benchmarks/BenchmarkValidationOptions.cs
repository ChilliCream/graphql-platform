using HotChocolate.Execution.Configuration;

namespace HotChocolate.Validation.Benchmarks
{
    public class BenchmarkValidationOptions : IValidateQueryOptionsAccessor
    {
        public int? MaxExecutionDepth => null;

        public int? MaxOperationComplexity => null;

        public bool? UseComplexityMultipliers => null;
    }
}
