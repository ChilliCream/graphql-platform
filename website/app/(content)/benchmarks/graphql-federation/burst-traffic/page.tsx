import { BenchmarkPage } from "@/src/components/benchmarks/BenchmarkPage";
import { benchmarkMetadata } from "@/src/components/benchmarks/benchmarkMetadata";

export const dynamic = "force-static";
export const metadata = {
  ...benchmarkMetadata(),
  robots: { index: false, follow: true },
};

export default function BurstTrafficBenchmarksPage() {
  return <BenchmarkPage initialScenario="burst" />;
}
