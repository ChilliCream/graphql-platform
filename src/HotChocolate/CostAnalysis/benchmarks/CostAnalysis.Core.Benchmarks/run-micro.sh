#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd -- "${script_dir}/../../../../.." && pwd)"
project="${script_dir}/HotChocolate.CostAnalysis.Core.Benchmarks.csproj"
artifacts="${repository_root}/BenchmarkDotNet.Artifacts/cost-analysis-micro"
results="${script_dir}/../results/micro"
reports=(
  "HotChocolate.CostAnalysis.AdversarialCorrelatedBooleansBenchmark-report.csv"
  "HotChocolate.CostAnalysis.CostPlanBenchmark-report.csv"
  "HotChocolate.CostAnalysis.LargeSchemaSnapshotBenchmark-report.csv"
)

rm -rf "${artifacts}"
for report in "${reports[@]}"; do
  rm -f "${results}/${report}"
done

dotnet run -c Release --project "${project}" -- \
  --artifacts "${artifacts}" \
  --filter \
  '*ColdCompileEvaluate*' \
  '*WarmEvaluate*' \
  '*AdversarialCorrelatedBooleans*' \
  '*InputShapeEvaluate*' \
  '*RepeatedRejection*' \
  '*LargeSchemaSnapshotBuild*'

for report in "${reports[@]}"; do
  source="${artifacts}/results/${report}"
  if [ ! -s "${source}" ]; then
    echo "Missing benchmark report: ${source}" >&2
    exit 1
  fi
  cp "${source}" "${results}/${report}"
done
