import React, { FC } from "react";
import styled, { css } from "styled-components";
import { Layout } from "@/components/layout";
import {
  ContentContainer,
  EnvelopeIcon,
  ImageContainer,
  Section,
  SectionRow,
  SectionTitle,
} from "@/components/misc/marketing-elements";
import { Hero, Intro, Teaser, Title } from "@/components/misc/page-elements";
import { SEO } from "@/components/misc/seo";
import { SupportCard } from "@/components/misc/support-card";
import ContactUsSvg from "@/images/contact-us.svg";
import { IsPhablet } from "@/shared-style";

type ServiceKind = "Corporate Training" | "Corporate Workshop";

export interface Service {
  readonly kind: ServiceKind;
  readonly description: string;
  readonly perks: string[];
}

const TrainingPage: FC = () => {
  const areaTitle = "Training";

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

  return (
    <Layout>
      <SEO title={areaTitle} />
      <Intro>
        <Title>{areaTitle}</Title>
        <Hero>Learning Is Easier From Experts</Hero>
        <Teaser>
          At ChilliCream, we want you to be successful.
          <br />
          We’ll tell you how it is, and what you need to get there.
        </Teaser>
      </Intro>
      <Section>
        <CardContainer>
          {services.map(({ kind, description, perks }) => (
            <SupportCard
              key={kind}
              name={kind}
              description={description}
              perks={perks}
            />
          ))}
        </CardContainer>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer>
            <ContactUsSvg />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Get in Touch</SectionTitle>
            <p>
              Want to learn more? Get the right help for your team and reach out
              to us today. Write us an{" "}
              <a href="mailto:contact@chillicream.com?subject=Training">
                <EnvelopeIcon />
              </a>{" "}
              and we will come back to you shortly!
            </p>
          </ContentContainer>
        </SectionRow>
      </Section>
    </Layout>
  );
};

export default TrainingPage;

const CardContainer = styled.div`
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1.5rem;
  justify-items: center;
  margin-top: 1.5rem auto 0;
  max-width: 800px;

  > :not(:first-child) {
    margin-top: 16px;
  }

  > div {
    margin-top: 0 !important;
  }

  ${IsPhablet(css`
    margin-top: 1rem;
    grid-template-columns: minmax(0, 1fr);
  `)}
`;
