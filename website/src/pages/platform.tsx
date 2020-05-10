import { graphql, useStaticQuery } from "gatsby";
import React, { FunctionComponent } from "react";
import { GetPlatformDataQuery } from "../../graphql-types";
import { Hero, Intro, Teaser, Title } from "../components/misc/page-elements";
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
        </Hero>
        <Teaser>...</Teaser>
      </Intro>
    </Layout>
  );
};

export default PlatformPage;
