import React, { FC } from "react";

import { SiteLayout } from "@/components/layout";
import { ContentSection, Plans, SEO } from "@/components/misc";
import {
  MostRecentBlogPostsSection,
  NewsletterSection,
} from "@/components/widgets";

const HelpPage: FC = () => {
  return (
    <SiteLayout>
      <SEO title="Help" />
      <ContentSection
        title="In Need of Urgent Help"
        noBackground
        titleSpace="large"
      >
        <Plans
          plans={[
            {
              title: "Community",
              price: 0,
              period: "hour",
              description:
                "Be part of the Community, get help, and help others. Together we're strong.",
              features: ["Public Slack Channel", "6000+ Individuals"],
              ctaText: "Join Slack",
              ctaLink: "https://slack.chillicream.com/",
            },
            {
              title: "Consultancy",
              price: 300,
              period: "hour",
              description:
                "You need immediate help and want to talk to an Expert.",
              features: ["Dedicated Session", "Dedicated Expert"],
              ctaText: "Book a Session",
              ctaLink: "https://calendly.com/chillicream/60min",
            },
            {
              title: "Support",
              price: "custom",
              description: "You need a Support Plan. Here you go.",
              features: [
                "Dedicated Account Manager",
                "Private Slack Channel",
                "E-Mail Support",
                "And More...",
              ],
              ctaText: "Check out Plans",
              ctaLink: "/services/support",
            },
          ]}
        />
      </ContentSection>
      <NewsletterSection />
      <MostRecentBlogPostsSection />
    </SiteLayout>
  );
};

export default HelpPage;
