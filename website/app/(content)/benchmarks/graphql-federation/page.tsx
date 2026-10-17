import { BenchmarkExplorer } from "@/src/components/benchmarks/BenchmarkExplorer";
import { BenchmarkHeader } from "@/src/components/benchmarks/BenchmarkHeader";
import { BenchmarkTestSetup } from "@/src/components/benchmarks/BenchmarkTestSetup";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import {
  federationBenchmarkHistory as history,
  federationBenchmarks as report,
} from "@/src/data/federationBenchmarks";
import { pageMetadata } from "@/src/helpers/pageMetadata";

const PAGE = {
  title: "GraphQL Federation Benchmarks",
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
        dateModified={
          report.run.completedAt ?? report.run.startedAt ?? undefined
        }
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "GraphQL Federation Benchmarks" },
        ]}
      />
      <article>
        <BenchmarkHeader report={report} />
        <div className="mt-10">
          <BenchmarkExplorer report={report} history={history} />
        </div>
        <BenchmarkTestSetup report={report} />
      </article>
    </>
  );
}
