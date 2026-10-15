import { BenchmarkExplorer } from "@/src/components/benchmarks/BenchmarkExplorer";
import { BenchmarkHero } from "@/src/components/benchmarks/BenchmarkHero";
import { BenchmarkMethodology } from "@/src/components/benchmarks/BenchmarkMethodology";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { federationBenchmarks as report } from "@/src/data/federationBenchmarks";
import { BENCHMARK_REPOSITORY } from "@/src/helpers/benchmarkReport";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

const PAGE = {
  title: "GraphQL Federation Router & Gateway Performance Benchmarks",
  description:
    "Compare GraphQL federation router and gateway performance for Fusion, Apollo, Hive, Cosmo, Grafbase, Caliban, and feddi. See throughput, memory usage, and hardware.",
  path: "/benchmarks/graphql-federation",
} as const;

export const dynamic = "force-static";
export const metadata = pageMetadata(PAGE);

export default function FederationBenchmarksPage() {
  const datasetId = schemaId(PAGE.path, "dataset");
  return (
    <>
      <PageStructuredData
        {...PAGE}
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "GraphQL federation benchmarks" },
        ]}
        mainEntity={schemaRef(datasetId)}
        additionalNodes={[
          {
            "@type": "Dataset",
            "@id": datasetId,
            name: `${report.example ? "Example " : ""}GraphQL federation benchmark report, run ${report.run.number}`,
            description:
              "Throughput and peak gateway RSS measurements for a nested query across four Hot Chocolate subgraphs. Includes real-world and synthetic scenarios; pending scenarios are explicitly marked. See the page for repetition counts and limitations.",
            url: toAbsoluteUrl(PAGE.path),
            creator: schemaRef(ORGANIZATION_ID),
            isBasedOn: `${BENCHMARK_REPOSITORY}/tree/${report.run.reportCommit}/benchmark-runs/${report.run.number}`,
            measurementTechnique:
              "k6 HTTP load testing; median requests per second across measured runs after a discarded warmup",
            variableMeasured: [
              {
                "@type": "PropertyValue",
                name: "Median HTTP throughput",
                unitText: "requests per second",
              },
              {
                "@type": "PropertyValue",
                name: "Peak gateway resident set size",
                unitText: "MiB",
              },
            ],
            distribution: [
              {
                "@type": "DataDownload",
                encodingFormat: "application/json",
                contentUrl: toAbsoluteUrl("/benchmarks/federation/latest.json"),
              },
            ],
          },
        ]}
      />
      <BenchmarkHero report={report} />
      <BenchmarkExplorer report={report} />
      <BenchmarkMethodology report={report} />
    </>
  );
}
