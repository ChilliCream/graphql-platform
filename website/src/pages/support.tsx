import { graphql, useStaticQuery } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetSupportDataQuery } from "../../graphql-types";
import {
  Hero,
  Intro,
  Section,
  SectionTitle,
  Teaser,
  Title,
} from "../components/misc/page-elements";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";

import CheckSvg from "../images/check.svg";

const SupportPage: FunctionComponent = () => {
  const areaTitle = "Service & Support";
  const data = useStaticQuery<GetSupportDataQuery>(graphql`
    query getSupportData {
      intro: file(relativePath: { eq: "startpage-header.svg" }) {
        publicURL
      }
    }
  `);

  return (
    <Layout>
      <SEO title={areaTitle} />
      <Intro url={data.intro!.publicURL!}>
        <Title>{areaTitle}</Title>
        <Hero>Get quick access to ChilliCream experts</Hero>
        <Teaser>
          Efficiency is everything. Make your team more productive and ship your
          product faster to market. Get immediate access to a pool of
          ChilliCream experts which will support you along your journey.
        </Teaser>
      </Intro>
      <Section>
        <SectionTitle>Developer Support</SectionTitle>
        <p>
          Obtain dedicated support for your team and work closely together with
          ChilliCream experts to design, build and ship your GraphQL API.
          Furthermore the ChilliCream experts will assist you with integration
          of existing systems, best practices, application life-cycle
          management, developer workflows and many other things which are not
          listed here.
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
      </Section>
      <Section>
        <SectionTitle>Training & Workshop</SectionTitle>
        <p>
          Let your team itself become experts on the ChilliCream GraphQL
          platform with dedicated training sessions and workshops.
        </p>
        <List>
          <li>
            <Check />
            Learn from ChilliCream experts, what GraphQL is, what it can do and
            how to use it
          </li>
          <li>
            <Check />
            Private training sessions, customized to your needs and requirements
          </li>
        </List>
      </Section>
      <Section>
        <SectionTitle>Get in Touch</SectionTitle>
        <p>
          Want to learn more? Get the right help for your team and reach out to
          us today.
        </p>
        <p>
          Write us an email at{" "}
          <a href="mailto:contact@chillicream.com">
            contact(at)chillicream.com
          </a>
        </p>
      </Section>
    </Layout>
  );
};

export default SupportPage;

const List = styled.ul`
  list-style-type: none;
`;

const Check = styled(CheckSvg)`
  margin: 0 10px 5px 0;
  width: 24px;
  height: 24px;
  vertical-align: middle;
  fill: green;
`;
