"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { SiteLayout } from "@/components/layout";
import {
  Card,
  CardOffer,
  CardsContainer,
  ContentSection,
  Hero,
  HeroTeaser,
  HeroTitleFirst,
  Link,
  NextStepsContentSection,
  SEO,
  SrOnly,
} from "@/components/misc";
import {
  CommunityVisualization,
  MostRecentBlogPostsSection,
  NewsletterSection,
} from "@/components/widgets";
import { RecentBlogPost } from "@/components/widgets/most-recent-blog-posts-section";
import docsConfig from "@/docs/docs.json";

// Images
import {
  ECOSYSTEM_CONTINUOUS_EVOLUTION_IMAGE_WIDTH,
  EcosystemBannerImage,
  EcosystemContinuousEvolutionImage,
} from "../../components/images";

interface EcosystemPageProps {
  recentPosts?: RecentBlogPost[];
}

const EcosystemPage: FC<EcosystemPageProps> = ({ recentPosts }) => {
  const products = docsConfig;
  const latestStableHotChocolateVersion =
    products?.find((t: any) => t?.path === "hotchocolate")?.latestStableVersion ??
    "";

  return (
    <SiteLayout>
      <SEO title="Ecosystem" />
      <BackgroundContainer>
        <EcosystemBannerImage />
      </BackgroundContainer>
      <EcosystemPageHero>
        <HeroTitleFirst>An Ecosystem You Love</HeroTitleFirst>
        <HeroTeaser>
          A harmonious blend of tools and community, dedicated to enhancing
          <br />
          your API journey. Experience simplicity, efficiency,
          <br />
          and collaborative innovation.
        </HeroTeaser>
      </EcosystemPageHero>
      <NextStepsContentSection
        title="Lead by Intuition"
        text={
          <>
            A framework built by developers for developers. Combining ease
            <br />
            of use with high-speed performance, it's designed to elevate your
            projects effortlessly.
          </>
        }
        primaryLink={"/docs/hotchocolate/" + latestStableHotChocolateVersion}
        primaryLinkText="Get Started"
        dense
      />
      <ContentSection
        title="Batteries Included"
        text="Everything you need to build great APIs - and more"
        noBackground
      >
        <Combine>
          <CardsContainer>
            <Card>
              <CardOffer>
                <header>
                  <h5>Authentication Flows</h5>
                </header>
                <p>
                  Choose between various authentication flows like basic, bearer
                  or OAuth 2.
                </p>
              </CardOffer>
            </Card>
            <Card>
              <CardOffer>
                <header>
                  <h5>Organization Workspaces</h5>
                </header>
                <p>
                  Organize your GraphQL APIs and collaborate with colleagues
                  across your organization with ease.
                </p>
              </CardOffer>
            </Card>
            <Card>
              <CardOffer>
                <header>
                  <h5>Document Synchronization</h5>
                </header>
                <p>
                  Keep your documents safe across all your devices and your
                  teams.
                </p>
              </CardOffer>
            </Card>
            <Card>
              <CardOffer>
                <header>
                  <h5>
                    PWA <SrOnly>(Progressive Web Application)</SrOnly> Support
                  </h5>
                </header>
                <p>
                  Use your favorite Browser to install Nitro as a PWA on your
                  Device without requiring administrative privileges.
                </p>
              </CardOffer>
            </Card>
            <Card>
              <CardOffer>
                <header>
                  <h5>Beautiful Themes</h5>
                </header>
                <p>
                  Choose your single preferred theme or let the system
                  automatically switch between dark and light theme.
                </p>
              </CardOffer>
            </Card>
            <Card>
              <CardOffer>
                <header>
                  <h5>GraphQL File Upload</h5>
                </header>
                <p>
                  Implements the latest version of the{" "}
                  <Link to="https://github.com/jaydenseric/graphql-multipart-request-spec">
                    GraphQL multipart request spec
                  </Link>
                  .
                </p>
              </CardOffer>
            </Card>
            <Card>
              <CardOffer>
                <header>
                  <h5>
                    Subscriptions over SSE <SrOnly>(Server-Sent Events)</SrOnly>
                  </h5>
                </header>
                <p>
                  Supports{" "}
                  <Link to="https://github.com/enisdenjo/graphql-sse">
                    GraphQL subscriptions over Server-Sent Events
                  </Link>
                  .
                </p>
              </CardOffer>
            </Card>
            <Card>
              <CardOffer>
                <header>
                  <h5>Performant GraphQL IDE</h5>
                </header>
                <p>
                  Lagging apps can be frustrating. We do not accept that and
                  keep always an eye on performance so that you can get your
                  task done fast.
                </p>
              </CardOffer>
            </Card>
            <Card>
              <CardOffer>
                <header>
                  <h5>
                    Subscriptions over WS <SrOnly>(WebSockets)</SrOnly>
                  </h5>
                </header>
                <p>
                  Supports{" "}
                  <Link to="https://github.com/enisdenjo/graphql-ws">
                    GraphQL subscriptions over WebSocket
                  </Link>{" "}
                  as well as the{" "}
                  <Link to="https://github.com/apollographql/subscriptions-transport-ws/blob/master/PROTOCOL.md">
                    Apollo subscription protocol
                  </Link>
                  .
                </p>
              </CardOffer>
            </Card>
          </CardsContainer>
          <CommunityVisualization />
        </Combine>
      </ContentSection>
      <ContentSection
        title="Continuous Evolution"
        text={
          <>
            Embracing the latest GraphQL specification drafts and future
            updates, this platform ensures users are always at the cutting edge.
            Experience an evolving GraphQL journey, where innovation and
            up-to-date features converge seamlessly.
          </>
        }
        image={<EcosystemContinuousEvolutionImage />}
        imagePosition="bottom"
        imageWidth={ECOSYSTEM_CONTINUOUS_EVOLUTION_IMAGE_WIDTH}
      />
      <NewsletterSection />
      <MostRecentBlogPostsSection posts={recentPosts} />
    </SiteLayout>
  );
};

export default EcosystemPage;

const BackgroundContainer = styled.div`
  position: absolute;
  z-index: -1;
  top: 340px;
  width: 100%;
  max-width: 600px;
  perspective: 1px;
`;

const EcosystemPageHero = styled(Hero)`
  @media only screen and (min-width: 992px) {
    padding-bottom: 340px;
  }
`;

const Combine = styled.div`
  display: flex;
  flex-direction: column;
  gap: 16px;

  @media only screen and (min-width: 992px) {
    gap: 24px;
  }
`;
