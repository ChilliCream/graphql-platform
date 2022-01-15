import React, { FC } from "react";
import styled from "styled-components";
import {
  ContentContainer,
  EnvelopeIcon,
  ImageContainer,
  Section,
  SectionRow,
  SectionTitle,
} from "../components/misc/marketing-elements";
import { Hero, Intro, Teaser, Title } from "../components/misc/page-elements";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";
import ContactUsSvg from "../images/contact-us.svg";
import { SupportCard } from "../components/support/support-card";
import { IsPhablet } from "../shared-style";

type ServiceType = "Consulting" | "Production Support";

interface SupportService {
  readonly service: ServiceType;
  readonly description: string;
  readonly perks: string[];
}

const SupportPage: FC = () => {
  const areaTitle = "Service & Support";

  const supportServices: SupportService[] = [
    {
      service: "Consulting",
      description: "Hourly consulting services to get the help you need at any stage of your project. This is the best way to get started.",
      perks: [
        "Mentoring and guidance",
        "Architecture",
        "Troubleshooting",
        "Code Review",
        "Best practices education"
      ]
    },
    {
      service: "Production Support",
      description: "Options for teams who don't have the time, bandwidth, and/or expertise to implement their own GraphQL solutions.",
      perks: [
        "Proof of concept development",
        "Implementation of HotChocolate or Strawberry Shake"
      ]
    }
  ];

  return (
    <Layout>
      <SEO title={areaTitle} />
      <Intro>
        <Title>{areaTitle}</Title>
        <Hero>Get quick access to ChilliCream experts</Hero>
        <Teaser>
          Efficiency is everything. Make your team more productive and ship your
          product faster to market. Get immediate access to a pool of
          ChilliCream experts which will support you along your journey.
        </Teaser>
      </Intro>
      <Section>
        <CardContainer>{supportServices.map(s =>
          <SupportCard key={s.service} name={s.service} description={s.description} perks={s.perks} />
        )}</CardContainer>
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
              <a href="mailto:contact@chillicream.com">
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


export default SupportPage;

const CardContainer = styled.div`
  margin-top: 1.5rem;
  justify-items: center;

  > :not(:first-child) {
    margin-top: 16px;
  }
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));

  max-width: 800px;
  margin-left: auto;
  margin-right: auto;

  gap: 1.5rem;

  > div {
    margin-top: 0 !important;
  }

  ${IsPhablet(`
    margin-top: 1rem;
    grid-template-columns: minmax(0, 1fr)
  `)}
`;

