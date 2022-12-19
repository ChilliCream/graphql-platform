import React, { FC } from "react";

import { BananaCakePop } from "@/components/images/banana-cake-pop";
import { HotChocolate } from "@/components/images/hot-chocolate";
import { Layout } from "@/components/layout";
import { Link } from "@/components/misc/link";
import {
  ContentContainer,
  ImageContainer,
  Section,
  SectionRow,
  SectionTitle,
} from "@/components/misc/marketing-elements";
import { Hero, Intro, Teaser, Title } from "@/components/misc/page-elements";
import { SEO } from "@/components/misc/seo";
import { Artwork } from "@/components/sprites";

// Artwork
import { SrOnly } from "@/components/misc/sr-only";
import UnderConstructionSvg from "@/images/artwork/under-construction.svg";

const PlatformPage: FC = () => {
  return (
    <Layout>
      <SEO title="Platform" />
      <Intro>
        <Title>The ChilliCream GraphQL Platform</Title>
        <Hero>
          An end-to-end solution to build, manage and access your GraphQL API
        </Hero>
        <Teaser>
          The heart of the ChilliCream platform is Hot Chocolate, our core for
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
              Hot Chocolate is a high-performant GraphQL .NET server and
              provides core libraries for Strawberry Shake, a GraphQL .NET
              client, and the ChilliCream GraphQL tools. No wonder why Hot
              Chocolate is the ChilliCream's platform core.
            </p>
            <Link to="/docs/hotchocolate">
              Learn more<SrOnly> on how to build GraphQL .NET APIs</SrOnly>
            </Link>
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
              Banana Cake Pop is a GraphQL IDE to explore schemas, execute
              operations and get deep performance insights about any GraphQL
              server out there.
            </p>
            <Link to="/docs/bananacakepop">
              Learn more<SrOnly> about our GraphQL IDE</SrOnly>
            </Link>
          </ContentContainer>
        </SectionRow>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer>
            <Artwork {...UnderConstructionSvg} />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Strawberry Shake</SectionTitle>
            <p>
              Strawberry Shake is a GraphQL tool to generate reactive GraphQL
              .NET clients to build modern, state of the art apps in e.g.{" "}
              <Link to="https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor">
                Blazor
              </Link>{" "}
              or{" "}
              <Link to="https://learn.microsoft.com/dotnet/maui/what-is-maui">
                .NET MAUI
              </Link>
              .
            </p>
            <Link to="/docs/strawberryshake">
              Learn more
              <SrOnly> on how to write reactive GraphQL .NET clients</SrOnly>
            </Link>
          </ContentContainer>
        </SectionRow>
      </Section>
    </Layout>
  );
};

export default PlatformPage;
