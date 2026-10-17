import { BenchmarkPage } from "@/src/components/benchmarks/BenchmarkPage";
import { BENCHMARK_PAGE } from "@/src/components/benchmarks/benchmarkRoutes";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const dynamic = "force-static";
export const metadata = pageMetadata(BENCHMARK_PAGE);

export default function FederationBenchmarksPage() {
  return <BenchmarkPage initialScenario="constant-latency" />;
}
