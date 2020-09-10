import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { Hero, Intro, Teaser, Title } from "../components/misc/page-elements";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";

import CheckSvg from "../images/check.svg";
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
              <li>
                <Check />
                Private Slack Channel with ChilliCream experts for your team
              </li>
              <li>
                <Check />
                Architecture, Code and Schema reviews
              </li>
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
              <li>
                <Check />
                Learn from ChilliCream experts, what GraphQL is, what it can do
                and how to use it
              </li>
              <li>
                <Check />
                Private training sessions, customized to your needs and
                requirements
              </li>
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
              to us today.
            </p>
            <p>
              Write us an email at{" "}
              <a href="mailto:contact@chillicream.com">
                contact(at)chillicream.com
              </a>
            </p>
          </ContentContainer>
        </SectionRow>
      </Section>
    </Layout>
  );
};

export default SupportPage;

const SectionRow = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  justify-content: space-around;
  width: 100%;
  max-width: 1100px;
`;

const Section = styled.section`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 70px 0 50px;
  width: 100%;

  &:nth-child(odd) {
    background-color: #efefef;
  }

  @media only screen and (min-width: 992px) {
    &:nth-child(even) > ${SectionRow} {
      flex-direction: row;
    }

    &:nth-child(odd) > ${SectionRow} {
      flex-direction: row-reverse;
    }
  }
`;

const ImageContainer = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  margin-bottom: 50px;
  padding: 0 20px;
  width: 100%;
  max-width: 380px;

  @media only screen and (min-width: 992px) {
    flex: 0 0 35%;
    box-sizing: initial;
    margin-bottom: initial;
    padding: 0;
    max-width: 280px;
  }
`;

const ContentContainer = styled.div`
  display: flex;
  flex-direction: column;
  padding: 0 40px;

  > p {
    text-align: center;
  }

  @media only screen and (min-width: 992px) {
    flex: 0 0 55%;
    padding: 0;

    > p {
      text-align: initial;
    }
  }
`;

const SectionTitle = styled.h1`
  flex: 0 0 auto;
  font-size: 1.75em;
  color: #667;
  text-align: center;

  @media only screen and (min-width: 768px) {
    margin-bottom: 20px;
  }

  @media only screen and (min-width: 992px) {
    text-align: initial;
  }
`;

const List = styled.ul`
  list-style-type: none;
  align-self: center;

  @media only screen and (min-width: 992px) {
    align-self: initial;
  }
`;

const Check = styled(CheckSvg)`
  margin: 0 10px 5px 0;
  width: 24px;
  height: 24px;
  vertical-align: middle;
  fill: green;
`;
