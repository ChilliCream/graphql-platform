import { graphql, useStaticQuery } from "gatsby";
import React, { FunctionComponent } from "react";
import { GetPlatformDataQuery } from "../../graphql-types";
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

const PlatformPage: FunctionComponent = () => {
  const data = useStaticQuery<GetPlatformDataQuery>(graphql`
    query getPlatformData {
      intro: file(relativePath: { eq: "startpage-header.svg" }) {
        publicURL
      }
    }
  `);

  return (
    <Layout>
      <SEO title="Platform" />
      <Intro url={data.intro!.publicURL!}>
        <Title>The ChilliCream GraphQL Platform</Title>
        <Hero>
          An end-to-end solution to build, manage and access your GraphQL API
          with ease
        </Hero>
        <Teaser>
          Bringing all your service and data-sources together is not easy, but
          we make you life easier by providing you the right tools for your
          team.
        </Teaser>
      </Intro>
      <Section>
        <SectionTitle>HotChocolate</SectionTitle>
        <p>
          HotChocolate is our GraphQL Server which is essentially the core for
          all our service and tools.
        </p>
      </Section>
      <Section>
        <SectionTitle>MarshmellowPie</SectionTitle>
        <p></p>
      </Section>
      <Section>
        <SectionTitle>GreenDonut</SectionTitle>
        <p>
          GreenDonut is our DataLoader implementation and is integrated into
          HotChocolate.
        </p>
      </Section>
      <Section>
        <SectionTitle>StrawberryShake</SectionTitle>
        <p>
          Our client for querying any GraphQL endpoint from a .Net application.
        </p>
      </Section>
      <Section>
        <SectionTitle>BananaCakePop</SectionTitle>
        <p>
          Our standalone GraphQL IDE to explore schemas, execute operations and
          get deep performance insights of any GraphQL endpoint.
        </p>
      </Section>
    </Layout>
  );
};

export default PlatformPage;
