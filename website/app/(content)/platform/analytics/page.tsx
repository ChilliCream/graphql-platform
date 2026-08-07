import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL } from "@/src/helpers/siteUrl";

import { ClosingCta } from "./ClosingCta";
import { FindTheCause } from "./FindTheCause";
import { FullOtelBand } from "./FullOtelBand";
import { Hero } from "./Hero";
import { ThreeQuestions } from "./ThreeQuestions";

export const metadata = pageMetadata({
  title: "API Analytics and OpenTelemetry Observability",
  description:
    "Analyze your APIs with OpenTelemetry: distributed traces, latency and error monitoring, impact scores, and per-client usage across GraphQL, REST, gRPC, and background jobs.",
  path: "/platform/analytics",
  keywords: [
    "API analytics",
    "OpenTelemetry analytics",
    "distributed tracing",
    "p95 p99 latency",
    "impact score",
    "per-client usage",
    "operation monitoring",
    "REST gRPC monitoring",
    ".NET observability",
    "Nitro",
  ],
});

const BREADCRUMB_DATA = {
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  itemListElement: [
    {
      "@type": "ListItem",
      position: 1,
      name: "Home",
      item: `${SITE_URL}/`,
    },
    {
      "@type": "ListItem",
      position: 2,
      name: "Platform",
      item: `${SITE_URL}/platform`,
    },
    {
      "@type": "ListItem",
      position: 3,
      name: "Analytics",
    },
  ],
};

export default function AnalyticsPage() {
  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(BREADCRUMB_DATA) }}
      />
      <Hero />
      <FullOtelBand />
      <ThreeQuestions />
      <FindTheCause />
      <ClosingCta />
    </>
  );
}
