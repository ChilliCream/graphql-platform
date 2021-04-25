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
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";

import UnderConstructionSvg from "../images/under-construction.svg";

const PlatformPage: FunctionComponent = () => {
  return (
    <Layout>
      <SEO title="Platform" />
      <Section>
        <SectionRow>
          <ImageContainer large>
            <HotChocolate />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Hot Chocolate</SectionTitle>
            <p>
              Hot Chocolate is an open-source GraphQL server for the Microsoft
              .NET platform that is compliant with the newest GraphQL 2021 draft
              spec. Hot Chocolate takes the complexity away from building a
              fully-fledged GraphQL server and lets you focus on delivering the
              next big thing.
            </p>
            <Link to="/docs/hotchocolate">Go to documentation</Link>
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
              Banana Cake Pop is a GraphQL IDE which works well any other
              GraphQL server. With Banana Cake pop, you can manage your queries,
              mutations and subscription in a modern web interface with ease.
            </p>
            <Link to="/docs/bananacakepop">Go to documentation</Link>
          </ContentContainer>
        </SectionRow>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer>
            <UnderConstructionSvg />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Strawberry Shake</SectionTitle>
            <p>
              Strawberry Shake is an open-source GraphQL client for the
              Microsoft .NET platform. It generates a strongly typed interface
              from GraphQL files with source generators. Shake removes the
              complexity of state management and lets you interact with local
              and remote data through GraphQL.
            </p>
            <Link to="/docs/strawberryshake">Go to documentation</Link>
          </ContentContainer>
        </SectionRow>
      </Section>
    </Layout>
  );
};

export default PlatformPage;
