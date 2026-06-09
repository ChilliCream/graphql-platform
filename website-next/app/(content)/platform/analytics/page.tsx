import { Picture } from "@/src/design-system/Picture";

import { ContentSection } from "@/src/components/ContentSection";
import { NextStepsSection } from "@/src/components/NextStepsSection";
import { PageHero } from "@/src/components/PageHero";
import { SolidButton } from "@/src/design-system/Button";

export default function AnalyticsPage() {
  return (
    <>
      <PageHero
        title="Instant Insights."
        subtitle="Enhanced Performance."
        teaser="Unlock the hidden potential of your system with instant, real-time insights. Make informed decisions and gain a deeper understanding of your system, driving smarter and more effective outcomes."
      />
      <div className="flex justify-center">
        <SolidButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Get Started
        </SolidButton>
      </div>
      <div className="mt-20 flex justify-center">
        <Picture
          src="/images/analytics/banner.png"
          alt="Instant Insights. Enhanced Performance."
          width={1200}
          height={700}
          sizes="(max-width: 1280px) 100vw, 1200px"
          priority
          className="h-auto w-full max-w-[1200px] rounded-2xl"
        />
      </div>
      <ContentSection
        title="Overview from Every Angle"
        text="Experience your ecosystem like never before with a single, unified view of all your data. Gain a comprehensive understanding of your entire distributed system. Monitor the health of your complete application and assess the impact of client interactions and pressure on subgraphs and make informed decisions based on data."
        imageSrc="/images/analytics/overview.png"
        imageAlt="Overview from Every Angle"
        imageMaxWidth={1200}
      />
      <ContentSection
        title="APIs Under the Microscope"
        text="Delve deep into your APIs with precision. Obtain detailed throughput, error analysis, and latency insights for your GraphQL queries. Beyond high-level system overviews, you can also monitor telemetry from individual services. Understand how operations impact your system and identify areas for performance improvement instantly."
        imageSrc="/images/analytics/observability.png"
        imageAlt="APIs Under the Microscope"
        imageMaxWidth={1200}
      />
      <ContentSection
        title="Trace, Detail, Insight."
        text="Explore every trace in depth. With full open telemetry, gain performance insights for individual operations. Examine every detail of your requests, from traces to spans, attributes and properties. Drill into error traces and pinpoint exactly what went wrong, providing comprehensive visibility and actionable insights."
        imageSrc="/images/analytics/insights.png"
        imageAlt="Trace, Detail, Insight"
        imageMaxWidth={1200}
      />
      <NextStepsSection
        title="Begin Your Journey"
        text="Discover a new way to see and manage your data. Start your journey now and transform your API experience."
        primaryLink="/services/support/contact?subject=Schedule+a+Demo"
        primaryLinkText="Book a Demo"
        secondaryLink="https://nitro.chillicream.com"
        secondaryLinkText="Launch"
      />
    </>
  );
}
