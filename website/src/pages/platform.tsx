import React, { FunctionComponent } from "react";
import { Hero, Intro, Teaser, Title } from "../components/misc/page-elements";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";

const PlatformPage: FunctionComponent = () => {
  return (
    <Layout>
      <SEO title="Platform" />
      <Intro>
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
