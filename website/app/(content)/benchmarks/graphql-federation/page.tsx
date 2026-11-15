import { BenchmarkPage } from "@/src/components/benchmarks/BenchmarkPage";
import { benchmarkMetadata } from "@/src/components/benchmarks/benchmarkMetadata";

export const dynamic = "force-static";
export const metadata = benchmarkMetadata();

export default function FederationBenchmarksPage() {
  return <BenchmarkPage initialScenario="constant-latency" />;
}
