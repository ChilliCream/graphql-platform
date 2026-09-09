#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd -- "${script_dir}/../../../../.." && pwd)"
project="${script_dir}/HotChocolate.CostAnalysis.Core.Benchmarks.csproj"
artifacts="${repository_root}/BenchmarkDotNet.Artifacts/cost-analysis-micro"
results="${script_dir}/../results/micro"

dotnet run -c Release --project "${project}" -- \
  --artifacts "${artifacts}" \
  --filter \
  '*ColdCompileEvaluate*' \
  '*WarmEvaluate*' \
  '*AdversarialCorrelatedBooleans*' \
  '*InputShapeEvaluate*' \
  '*RepeatedRejection*' \
  '*LargeSchemaSnapshotBuild*'

find "${artifacts}/results" -maxdepth 1 -type f \
  -name '*-report.csv' \
  -exec cp {} "${results}" \;
