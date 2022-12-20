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
import { Artwork } from "@/components/sprites";
import { IsPhablet } from "@/shared-style";

// Artwork
import ContactUsSvg from "@/images/artwork/contact-us.svg";

interface Service {
  readonly kind: "Consulting" | "Production Support";
  readonly description: string;
  readonly perks: string[];
}

const AvisoryPage: FC = () => {
  const areaTitle = "Advisory";

  const services: Service[] = [
    {
      kind: "Consulting",
      description:
        "Hourly consulting services to get the help you need at any stage of your project. This is the best way to get started.",
      perks: [
        "Mentoring and guidance",
        "Architecture",
        "Troubleshooting",
        "Code Review",
        "Best practices education",
      ],
    },
    {
      kind: "Production Support",
      description:
        "Options for teams who don't have the time, bandwidth, and/or expertise to implement their own GraphQL solutions.",
      perks: [
        "Proof of concept development",
        "Implementation of HotChocolate or Strawberry Shake",
      ],
    },
  ];

  return (
    <Layout>
      <SEO title={areaTitle} />
      <Intro>
        <Title>{areaTitle}</Title>
        <Hero>Get Quick Access to Experts</Hero>
        <Teaser>
          At ChilliCream, we want you to be successful.
          <br />
          From guidance to embedded experts, find the right level for your
          business.
        </Teaser>
      </Intro>
      <Section>
        <CardsContainer>
          {services.map(({ kind, description, perks }) => (
            <SupportCard
              key={kind}
              name={kind}
              description={description}
              perks={perks}
            />
          ))}
        </CardsContainer>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer>
            <Artwork {...ContactUsSvg} />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Get in Touch</SectionTitle>
            <p>
              Want to learn more? Get the right help for your team and reach out
              to us today. Write us an{" "}
              <a href="mailto:contact@chillicream.com?subject=Advisory">
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

export default AvisoryPage;

const CardsContainer = styled.div`
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
