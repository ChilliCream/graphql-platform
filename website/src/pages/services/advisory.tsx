import React, { FC } from "react";

import { SiteLayout } from "@/components/layout";
import {
  ContentSection,
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

const AdvisoryPage: FC = () => {
  return (
    <SiteLayout>
      <SEO title="Advisory" />
      <Hero>
        <HeroTitleFirst>Get Quick Access to Experts</HeroTitleFirst>
        <HeroTeaser>
          At ChilliCream, we want you to be successful.
          <br />
          From guidance to embedded experts, find the right level for your
          business.
        </HeroTeaser>
      </Hero>
      <ContentSection noBackground>
        <Plans
          plans={[
            {
              title: "Consulting",
              description:
                "Hourly consulting services to get the help you need at any stage of your project. This is the best way to get started.",
              features: [
                "Mentoring and guidance",
                "Architecture",
                "Troubleshooting",
                "Code Review",
                "Best practices education",
              ],
              ctaText: "Talk to an Expert",
              ctaLink: "mailto:contact@chillicream.com?subject=Consulting",
            },
            {
              title: "Contracting",
              description:
                "Options for teams who don't have the time, bandwidth, and/or expertise to implement their own GraphQL solutions.",
              features: ["Proof of concept", "Implementation"],
              ctaText: "Talk to an Expert",
              ctaLink: "mailto:contact@chillicream.com?subject=Contracting",
            },
          ]}
        />
      </ContentSection>
      <NewsletterSection />
      <MostRecentBlogPostsSection />
    </SiteLayout>
  );
};

export default AdvisoryPage;
