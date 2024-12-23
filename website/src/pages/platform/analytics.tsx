import React, { FC } from "react";

import { SiteLayout } from "@/components/layout";
import {
  ContentSection,
  Hero,
  HeroImageContainer,
  HeroLink,
  HeroTeaser,
  HeroTitleFirst,
  HeroTitleSecond,
  NextStepsContentSection,
  SEO,
} from "@/components/misc";
import {
  MostRecentBlogPostsSection,
  NewsletterSection,
} from "@/components/widgets";

// Images
import {
  ANALYTICS_INSIGHTS_IMAGE_WIDTH,
  ANALYTICS_OBSERVABILITY_IMAGE_WIDTH,
  ANALYTICS_OVERVIEW_IMAGE_WIDTH,
  AnalyticsBannerImage,
  AnalyticsInsightsImage,
  AnalyticsObservabilityImage,
  AnalyticsOverviewImage,
} from "../../components/images";

const AnalyticsPage: FC = () => {
  return (
    <SiteLayout>
      <SEO title="Analytics" />
      <Hero>
        <HeroTitleFirst>Instant Insights.</HeroTitleFirst>
        <HeroTitleSecond>Enhanced Performance.</HeroTitleSecond>
        <HeroTeaser>
          Unlock the hidden potential of your system with instant, real-time
          insights. Make informed decisions and gain a deeper understanding of
          your system, driving smarter and more effective outcomes.
        </HeroTeaser>
        <HeroLink to="/docs/nitro/open-telemetry/operation-monitoring">
          Get Started
        </HeroLink>
        <HeroImageContainer>
          <AnalyticsBannerImage />
        </HeroImageContainer>
      </Hero>
      <ContentSection
        title="Overview from Every Angle"
        text={
          <>
            Experience your ecosystem like never before with a single, unified
            view of all your data. Gain a comprehensive understanding of your
            entire distributed system. Monitor the health of your complete
            application and assess the impact of client interactions and
            pressure on subgraphs and make informed decisions based on data.
          </>
        }
        image={<AnalyticsOverviewImage />}
        imagePosition="bottom"
        imageWidth={ANALYTICS_OVERVIEW_IMAGE_WIDTH}
      />
      <ContentSection
        title="APIs Under the Microscope"
        text={
          <>
            Delve deep into your APIs with precision. Obtain detailed
            throughput, error analysis, and latency insights for your GraphQL
            queries. Beyond high-level system overviews, you can also monitor
            telemetry from individual services. Understand how operations impact
            your system and identify areas for performance improvement
            instantly.
          </>
        }
        image={<AnalyticsObservabilityImage />}
        imagePosition="bottom"
        imageWidth={ANALYTICS_OBSERVABILITY_IMAGE_WIDTH}
      />
      <ContentSection
        title="Trace, Detail, Insight."
        text={
          <>
            Explore every trace in depth. With full open telemetry, gain
            performance insights for individual operations. Examine every detail
            of your requests, from traces to spans, attributes and properties.
            Drill into error traces and pinpoint exactly what went wrong,
            providing comprehensive visibility and actionable insights.
          </>
        }
        image={<AnalyticsInsightsImage />}
        imagePosition="bottom"
        imageWidth={ANALYTICS_INSIGHTS_IMAGE_WIDTH}
      />
      <NextStepsContentSection
        title="Begin Your Journey"
        text={
          <>
            Discover a new way to see and manage your data. Start your journey
            now and transform your API experience.
          </>
        }
        primaryLink="mailto:contact@chillicream.com?subject=Demo"
        primaryLinkText="Book a Demo"
        secondaryLink="https://nitro.chillicream.com"
        secondaryLinkText="Launch"
      />
      <NewsletterSection />
      <MostRecentBlogPostsSection />
    </SiteLayout>
  );
};

export default AnalyticsPage;
