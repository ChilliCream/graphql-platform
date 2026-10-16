import { BenchmarkExplorer } from "@/src/components/benchmarks/BenchmarkExplorer";
import { BenchmarkHero } from "@/src/components/benchmarks/BenchmarkHero";
import { BenchmarkMethodology } from "@/src/components/benchmarks/BenchmarkMethodology";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { federationBenchmarks as report } from "@/src/data/federationBenchmarks";
import { pageMetadata } from "@/src/helpers/pageMetadata";

const PAGE = {
  title: "GraphQL Federation Router & Gateway Performance Benchmarks",
  description:
    "Compare GraphQL federation router and gateway performance for Fusion, Apollo, Hive, Cosmo, Grafbase, Caliban, and feddi. See throughput, latency, memory and CPU usage, and hardware.",
  path: "/benchmarks/graphql-federation",
} as const;

export const dynamic = "force-static";
export const metadata = pageMetadata(PAGE);

export default function FederationBenchmarksPage() {
  return (
    <>
      <PageStructuredData
        {...PAGE}
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "GraphQL federation benchmarks" },
        ]}
      />
      <BenchmarkHero report={report} />
      <BenchmarkExplorer report={report} />
      <BenchmarkMethodology report={report} />
    </>
  );
}
