import React, { FC } from "react";

import { NitroBannerImage } from "@/components/images";
import { SiteLayout } from "@/components/layout";
import {
  Hero,
  HeroImageContainer,
  HeroTeaser,
  HeroTitleFirst,
  SEO,
} from "@/components/misc";
import {
  CompaniesSection,
  MostRecentBcpBlogPostsSection,
  NewsletterSection,
} from "@/components/widgets";

const TITLE = "Nitro / GraphQL IDE";

const NitroPage: FC = () => {
  return (
    <SiteLayout>
      <SEO
        title={TITLE}
        description="Nitro is an incredible, beautiful, and feature-rich GraphQL IDE / API Cockpit for developers that works with any GraphQL APIs."
      />
      <Hero>
        <HeroTitleFirst>Nitro</HeroTitleFirst>
        <HeroTeaser>
          The first version of Nitro will be available within the next 48 hours.
        </HeroTeaser>
        <HeroImageContainer>
          <NitroBannerImage />
        </HeroImageContainer>
      </Hero>
      <CompaniesSection />
      <NewsletterSection />
      <MostRecentBcpBlogPostsSection />
    </SiteLayout>
  );
};

export default NitroPage;
