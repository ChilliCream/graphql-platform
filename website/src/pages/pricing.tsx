import React, { FC } from "react";

import { SiteLayout } from "@/components/layout";
import {
  ContentSection,
  Hero,
  HeroTeaser,
  HeroTitleFirst,
  HeroTitleSecond,
  Plans,
  SEO,
} from "@/components/misc";
import { FeatureMatrix } from "@/components/misc/feature-matrix";
import {
  MostRecentBlogPostsSection,
  NewsletterSection,
} from "@/components/widgets";

const PricingPage: FC = () => {
  return (
    <SiteLayout>
      <SEO title="Pricing" />
      <Hero>
        <HeroTitleFirst>Fusion Plans</HeroTitleFirst>
        <HeroTitleSecond>For any Scale</HeroTitleSecond>
        <HeroTeaser>Choose the right plan for your Team.</HeroTeaser>
      </Hero>
      <ContentSection
        title="Start for free, and pay as you grow."
        noBackground
        titleSpace="large"
      >
        <Plans
          plans={[
            {
              title: "Shared",
              price: 0,
              period: "month",
              description:
                "For personal or non-commercial projects, to start hacking.",
              features: ["Public Slack Channel"],
              ctaText: "Start for Free",
              ctaLink: "https://nitro.chillicream.com",
            },
            {
              title: "Dedicated",
              price: 400,
              period: "month",
              description:
                "For personal or non-commercial projects, to start hacking.",
              features: ["2 critical incidents", "Private Slack Channel"],
              ctaText: "Contact Sales",
              ctaLink: "mailto:contact@chillicream.com?subject=Dedicated",
            },
            {
              title: "Enterprise",
              price: "custom",
              description:
                "For personal or non-commercial projects, to start hacking.",
              features: ["2 critical incidents", "Private Slack Channel"],
              ctaText: "Contact Sales",
              ctaLink: "mailto:contact@chillicream.com?subject=Enterprise",
            },
          ]}
        />
      </ContentSection>
      {false && (
        <ContentSection
          title="Compare our Plans"
          noBackground
          titleSpace="large"
        >
          <FeatureMatrix
            plans={["Shared", "Dedicated", "Enterprise"]}
            featureGroups={[
              {
                title: "Support",
                features: [
                  {
                    title: "Critical incidents",
                    values: [
                      true,
                      "2 (next business day)",
                      "5 (next business day)",
                    ],
                  },
                  {
                    title: "Critical incidents",
                    values: [false, false, true],
                  },
                ],
              },
            ]}
          />
        </ContentSection>
      )}
      <NewsletterSection />
      <MostRecentBlogPostsSection />
    </SiteLayout>
  );
};

export default PricingPage;
