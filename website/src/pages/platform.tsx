import React, { FunctionComponent } from "react";
import { BananaCakePop } from "../components/images/banana-cake-pop";
import { HotChocolate } from "../components/images/hot-chocolate";
import { Link } from "../components/misc/link";
import {
  ContentContainer,
  ImageContainer,
  Section,
  SectionRow,
  SectionTitle,
} from "../components/misc/marketing-elements";
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
        <Teaser>
          The heart of the ChilliCream platform is Hot Chocolate our core for
          the GraphQL client and server. The ChilliCream platform provides
          developer tools and services to speed up the entire development
          process.
        </Teaser>
      </Intro>
      <Section>
        <SectionRow>
          <ImageContainer large>
            <HotChocolate />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Hot Chocolate</SectionTitle>
            <p>
              Hot Chocolate is the GraphQL server, but also provides core
              libraries for Strawberry Shake, which is our GraphQL client, and
              our GraphQL tools. No wonder why Hot Chocolate is the
              ChilliCream's platform core.
            </p>
            <Link to="/docs/hotchocolate">Learn more</Link>
          </ContentContainer>
        </SectionRow>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer large>
            <BananaCakePop />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Banana Cake Pop</SectionTitle>
            <p>
              Banana Cake Pop is our tool to explore schemas, execute operations
              and get deep performance insights about any GraphQL server out
              there.
            </p>
            <Link to="/docs/bananacakepop">Learn more</Link>
          </ContentContainer>
        </SectionRow>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer large>
            <HotChocolate />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Strawberry Shake</SectionTitle>
            <p>...</p>
            <Link to="/docs/strawberryshake">Learn more</Link>
          </ContentContainer>
        </SectionRow>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer large>
            <HotChocolate />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Marshmallow Pie</SectionTitle>
            <p>...</p>
            <Link to="/docs/marshmellowpie">Learn more</Link>
          </ContentContainer>
        </SectionRow>
      </Section>
    </Layout>
  );
};

export default PlatformPage;
