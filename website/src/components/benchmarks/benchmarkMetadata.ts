import type { Metadata } from "next";
import {
  BENCHMARK_DATA,
  BENCHMARK_PAGE,
} from "@/src/components/benchmarks/benchmarkRoutes";
import { benchmarkPageDescription } from "@/src/components/benchmarks/benchmarkView";
import { federationBenchmarks } from "@/src/data/federationBenchmarks";
import { pageMetadata } from "@/src/helpers/pageMetadata";

/** Returns the benchmark page metadata, including alternate links to the published JSON data. */
export function benchmarkMetadata(): Metadata {
  const metadata = pageMetadata({
    ...BENCHMARK_PAGE,
    description: benchmarkPageDescription(federationBenchmarks),
  });
  return {
    ...metadata,
    alternates: {
      ...metadata.alternates,
      types: {
        "application/json": Object.values(BENCHMARK_DATA).map((file) => ({
          url: file.path,
          title: file.title,
        })),
      },
    },
  };
}
