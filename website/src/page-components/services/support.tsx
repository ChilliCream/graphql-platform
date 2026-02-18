"use client";

import React, { FC } from "react";

import { SiteLayout } from "@/components/layout";
import {
  ContentSection,
  FeatureMatrix,
  Hero,
  HeroTeaser,
  HeroTitleFirst,
  Plans,
  SEO,
} from "@/components/misc";
import {
  MostRecentBlogPostsSection,
  NewsletterSection,
} from "@/components/widgets";
import { RecentBlogPost } from "@/components/widgets/most-recent-blog-posts-section";

interface Plans {
  readonly title: string;
  readonly price: string;
  readonly billed: string;
  readonly description: string;
  readonly action: {
    readonly message: string;
    readonly url: string;
  };
  readonly scope: string;
  readonly checklist: string[];
}

interface SupportPageProps {
  recentPosts?: RecentBlogPost[];
}

const SupportPage: FC<SupportPageProps> = ({ recentPosts }) => {
  return (
    <SiteLayout>
      <SEO title="Support" />
      <Hero>
        <HeroTitleFirst>Expert Help When You Need It</HeroTitleFirst>
        <HeroTeaser>
          At ChilliCream, we want you to be successful.
          <br />
          Our Support plans are designed to give you peace of mind on every
          project.
        </HeroTeaser>
      </Hero>
      <ContentSection title="Support Plans" noBackground titleSpace="large">
        <Plans
          plans={[
            {
              title: "Community",
              price: 0,
              period: "month",
              description:
                "For personal or non-commercial projects, to start hacking.",
              features: ["Public Slack Channel"],
              ctaText: "Join Slack",
              ctaLink: "https://slack.chillicream.com/",
            },
            {
              title: "Startup",
              price: 450,
              period: "month",
              description:
                "For small teams with moderate bandwidth and projects of low to medium complexity.",
              features: ["Private Slack Channel", "2 critical incidents"],
              ctaText: "Contact Us",
              ctaLink: "/services/support/contact?plan=Startup",
            },
            {
              title: "Business",
              price: 1300,
              period: "month",
              description: "For larger teams with business-critical projects.",
              features: [
                "Private Slack Channel",
                "5 critical incidents",
                "2 non-critical incidents",
                "Email support",
              ],
              ctaText: "Contact Us",
              ctaLink: "/services/support/contact?plan=Business",
            },
            {
              title: "Enterprise",
              price: "custom",
              description:
                "For the whole organization, all your teams and business units, and with tailor made SLAs.",
              features: [
                "Private Slack Channel",
                "Unlimited critical incidents",
                "10 non-critical incidents",
                "Phone support",
                "Dedicated account manager",
                "Status reviews",
              ],
              ctaText: "Contact Us",
              ctaLink: "/services/support/contact?plan=Enterprise",
            },
          ]}
        />
      </ContentSection>
      <ContentSection
        title="Compare our Support Plans"
        noBackground
        titleSpace="large"
      >
        <FeatureMatrix
          plans={["Community", "Startup", "Business", "Enterprise"]}
          featureGroups={[
            {
              title: "Support",
              features: [
                {
                  title: "Critical Incidents",
                  values: [
                    false,
                    "2 (next business day)",
                    "5 (next business day)",
                    "∞ (24 hours)",
                  ],
                },
                {
                  title: "Non-critical Incidents",
                  values: [
                    false,
                    false,
                    "5 (3 business days)",
                    "10 (next business day)",
                  ],
                },
                {
                  title: "Public Slack Channel",
                  values: [true, true, true, true],
                },
                {
                  title: "Private Slack Channel",
                  values: [false, true, true, true],
                },
                {
                  title: "Private Issue Tracking Board",
                  values: [false, false, true, true],
                },
                {
                  title: "Email Support",
                  values: [false, false, true, true],
                },
                {
                  title: "Phone Support",
                  values: [false, false, false, true],
                },
                {
                  title: "Dedicated Account Manager",
                  values: [false, false, false, true],
                },
                {
                  title: "Status Reviews",
                  values: [false, false, false, true],
                },
              ],
            },
          ]}
        />
      </ContentSection>
      <NewsletterSection />
      <MostRecentBlogPostsSection posts={recentPosts} />
    </SiteLayout>
  );
};

export default SupportPage;
