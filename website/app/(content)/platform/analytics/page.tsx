import { PageStructuredData } from "@/src/components/PageStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { schemaId, schemaRef } from "@/src/helpers/structuredData";

import { ClosingCta } from "./ClosingCta";
import { FindTheCause } from "./FindTheCause";
import { FullOtelBand } from "./FullOtelBand";
import { Hero } from "./Hero";
import { ThreeQuestions } from "./ThreeQuestions";

const PAGE = {
  title: "API Analytics and OpenTelemetry Observability",
  description:
    "Use API analytics and OpenTelemetry traces to investigate latency, errors, throughput, operation impact, and identified client usage in Nitro.",
  path: "/platform/analytics",
  keywords: [
    "API analytics",
    "API analytics with OpenTelemetry",
    "distributed tracing",
    "p95 p99 latency",
    "impact score",
    "per-client usage",
    "operation monitoring",
    "GraphQL API observability",
    ".NET observability",
    "Nitro",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

export default function AnalyticsPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Platform", path: "/platform" },
          { name: "Analytics" },
        ]}
        about={schemaRef(schemaId("/products/nitro", "product"))}
      />
      <Hero />
      <FullOtelBand />
      <ThreeQuestions />
      <FindTheCause />
      <ClosingCta />
    </>
  );
}
