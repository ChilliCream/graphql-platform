import React, { FunctionComponent } from "react";
import {
  CheckIcon,
  ContentContainer,
  EnvelopeIcon,
  ImageContainer,
  List,
  ListItem,
  Section,
  SectionRow,
  SectionTitle,
} from "../components/misc/marketing-elements";
import { Hero, Intro, Teaser, Title } from "../components/misc/page-elements";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";

import ContactUsSvg from "../images/contact-us.svg";
import DeveloperSupportSvg from "../images/developer-support.svg";
import TrainingAndWorkshopSvg from "../images/training-and-workshop.svg";

const SupportPage: FunctionComponent = () => {
  const areaTitle = "Service & Support";

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
        <SectionRow>
          <ImageContainer>
            <DeveloperSupportSvg />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Developer Support</SectionTitle>
            <p>
              Obtain dedicated support for your team and work closely together
              with ChilliCream experts to design, build and ship your GraphQL
              API. Furthermore the ChilliCream experts will assist you with
              integration of existing systems, best practices, application
              life-cycle management, developer workflows and many other things
              which are not listed here.
            </p>
            <List>
              <ListItem>
                <CheckIcon />
                Private Slack Channel with ChilliCream experts for your team
              </ListItem>
              <ListItem>
                <CheckIcon />
                Architecture, Code and Schema reviews
              </ListItem>
            </List>
          </ContentContainer>
        </SectionRow>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer>
            <TrainingAndWorkshopSvg />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Training & Workshop</SectionTitle>
            <p>
              Let your team itself become experts on the ChilliCream GraphQL
              platform with dedicated training sessions and workshops.
            </p>
            <List>
              <ListItem>
                <CheckIcon />
                Learn from ChilliCream experts, what GraphQL is, what it can do
                and how to use it
              </ListItem>
              <ListItem>
                <CheckIcon />
                Private training sessions, customized to your needs and
                requirements
              </ListItem>
            </List>
          </ContentContainer>
        </SectionRow>
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
