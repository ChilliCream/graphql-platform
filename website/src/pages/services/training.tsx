import React, { FC } from "react";
import { useSelector } from "react-redux";

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
  PublicWorkshopSection,
} from "@/components/widgets";
import { State, WorkshopsState } from "@/state";

interface Service {
  readonly kind: "Corporate Training" | "Corporate Workshop";
  readonly description: string;
  readonly perks: string[];
}

const TrainingPage: FC = () => {
  const services: Service[] = [
    {
      kind: "Corporate Training",
      description:
        "Get your team trained in GraphQL, any of our products, and even React/Relay. Beginner Team? Advanced Team? Or Mixed? Don't panic! Our curriculum is designed to teach in-depth and works really well, but isn't set in stone.",
      perks: [
        "Level up their proficiency",
        "Catered to different skills",
        "Overcome challenges they've been wrestling with",
        "Get everybody on the same technical page",
      ],
    },
    {
      kind: "Corporate Workshop",
      description:
        "We will look at how to build a GraphQL server with ASP.NET Core 7 and Hot Chocolate. You will learn how to explore and manage large schemas. Further, we will dive into React and explore how to efficiently build fast and fluent web interfaces using Relay.",
      perks: [
        "Core concepts and advanced",
        "Deepen knowledge of GraphQL API",
        "Work on a real project",
        "Scale and production quirks",
        "Level up your entire team at once",
        "Have Lots of Fun!",
      ],
    },
  ];

  const workshops = useSelector<State, WorkshopsState>((state) =>
    state.workshops.filter(({ active }) => active)
  );

  return (
    <SiteLayout>
      <SEO title="Training" />
      <Hero>
        <HeroTitleFirst>Learning Is Easier From Experts</HeroTitleFirst>
        <HeroTeaser>
          At ChilliCream, we want you to be successful.
          <br />
          We’ll tell you how it is, and what you need to get there.
        </HeroTeaser>
      </Hero>
      <PublicWorkshopSection />
      <ContentSection title="Corporate Offers" noBackground titleSpace="large">
        <Plans
          plans={services.map(({ kind, description, perks }) => ({
            title: kind,
            description,
            features: perks,
            ctaText: "Talk to us",
            ctaLink: "mailto:contact@chillicream.com?subject=Corporate Offers",
          }))}
        />
      </ContentSection>
      <NewsletterSection />
      <MostRecentBlogPostsSection />
    </SiteLayout>
  );
};

export default TrainingPage;
