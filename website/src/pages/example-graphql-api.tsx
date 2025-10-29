import { SiteLayout } from "@/components/layout";
import { CodeBlock } from "@/components/mdx/code-block";
import { Copy } from "@/components/mdx/copy";
import { Video } from "@/components/mdx/video";
import { IconContainer, LinkButton, SEO } from "@/components/misc";
import { Icon } from "@/components/sprites";
import ArrowRightIconSvg from "@/images/icons/arrow-right.svg";
import CheckIconSvg from "@/images/icons/check.svg";
import { FONT_FAMILY_CODE, MAX_CONTENT_WIDTH, THEME_COLORS } from "@/style";
import React, { FC, useEffect } from "react";
import styled from "styled-components";

const exampleQuery = `query GetProducts {
  products {
    edges {
      node {
        id
        name
        reviews {
          edges {
            node {
              stars
              body
              author {
                name
              }
            }
          }
        }
      }
    }
  }
}`;

const serverUrl = "https://demo.chillicream.cloud/graphql";
const features = [
  "Relay compatible: Includes Connections for paging and the node field",
  "Modern GraphQL: Contains @defer, @stream, and @oneOf",
  "No CORS: Use it directly in your frontend",
  "Subcriptions",
];

const GraphQLExamplePage: FC = () => {
  return (
    <SiteLayout disableStars>
      <SEO
        title="Example GraphQL API"
        description="Explore our example GraphQL API to familiarize yourself with GraphQL and experiment with advanced features like Relay compatibility, @defer/@stream, subscriptions, and more."
      />

      <HeroContainer>
        <PageTitle>Example GraphQL API</PageTitle>
        <PageTeaser>
          Explore and experiment with a fully functional GraphQL API featuring
          advanced capabilities like Relay compatibility, subscriptions, and
          streaming.
        </PageTeaser>

        <EndpointSection>
          <EndpointUrlContainer>
            <EndpointLabel>Endpoint URL:</EndpointLabel>
            <EndpointUrl>{serverUrl}</EndpointUrl>
            <CopyButtonWrapper>
              <Copy content={serverUrl} hideToast />
            </CopyButtonWrapper>
          </EndpointUrlContainer>
          <OpenButton to={serverUrl} prefetch={false}>
            Open in Nitro
            <IconContainer $size={16}>
              <Icon {...ArrowRightIconSvg} />
            </IconContainer>
          </OpenButton>
        </EndpointSection>
      </HeroContainer>

      <SimpleSection>
        <SectionTitle>Example Query</SectionTitle>
        <CodeBlockContainer>
          <CodeBlock
            language="graphql"
            hideLanguageIndicator
            playUrl={serverUrl}
            children={exampleQuery}
          />
        </CodeBlockContainer>
      </SimpleSection>

      <SimpleSection>
        <SectionTitle>Features</SectionTitle>
        <FeaturesList>
          {features.map((feature) => (
            <FeatureItem key={feature}>
              <FeatureIcon>
                <IconContainer $size={20}>
                  <Icon {...CheckIconSvg} />
                </IconContainer>
              </FeatureIcon>
              <FeatureText>{feature}</FeatureText>
            </FeatureItem>
          ))}
        </FeaturesList>
      </SimpleSection>

      <SimpleSection>
        <SectionTitle>Nitro overview</SectionTitle>

        <Video videoId="QPelWd9L9ck" />
      </SimpleSection>
    </SiteLayout>
  );
};

export default GraphQLExamplePage;

const HeroContainer = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  padding-top: 80px;
  padding-bottom: 60px;
  width: 100%;

  @media only screen and (min-width: 992px) {
    padding-top: 100px;
    padding-bottom: 80px;
  }
`;

const PageTitle = styled.h1`
  font-size: 2.5rem;
  font-weight: 600;
  text-align: center;
  margin: 0 0 16px 0;
  padding: 0 16px;

  @media only screen and (min-width: 992px) {
    font-size: 3rem;
  }
`;

const PageTeaser = styled.p`
  font-size: 1.125rem;
  text-align: center;
  margin: 0 0 32px 0;
  padding: 0 16px;
  max-width: 700px;
  line-height: 1.6;
  color: ${THEME_COLORS.textAlt};

  @media only screen and (min-width: 992px) {
    font-size: 1.25rem;
    margin-bottom: 40px;
  }
`;

const EndpointSection = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  margin-top: 0;
  width: 100%;
  max-width: 800px;
  padding: 0 16px;

  @media only screen and (min-width: 768px) {
    flex-direction: row;
    justify-content: center;
    gap: 16px;
    padding: 0;
  }
`;

const EndpointUrlContainer = styled.div`
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px 20px;
  background-color: ${THEME_COLORS.backgroundAlt};
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--box-border-radius);
  flex: 1;
  min-width: 0;

  @media only screen and (min-width: 768px) {
    flex: 1 1 auto;
    max-width: 500px;
  }
`;

const EndpointLabel = styled.span`
  font-size: 0.875rem;
  color: ${THEME_COLORS.textAlt};
  flex-shrink: 0;

  @media only screen and (max-width: 480px) {
    display: none;
  }
`;

const EndpointUrl = styled.code`
  font-family: ${FONT_FAMILY_CODE};
  font-size: 0.875rem;
  color: ${THEME_COLORS.link};
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
`;

const CopyButtonWrapper = styled.div`
  flex-shrink: 0;
  display: flex;
  align-items: center;
`;

const OpenButton = styled(LinkButton)`
  flex-shrink: 0;

  > ${IconContainer} {
    margin-left: 8px;
    margin-right: 0;

    svg {
      fill: currentColor;
      transition: transform 0.2s ease-in-out;
    }
  }

  &:hover > ${IconContainer} svg {
    transform: translateX(2px);
  }
`;

const SimpleSection = styled.section`
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 100%;
  padding: 32px 16px;
  max-width: ${MAX_CONTENT_WIDTH}px;
  margin: 0 auto;

  @media only screen and (min-width: 992px) {
    padding: 40px 24px;
  }

  @media only screen and (min-width: 1246px) {
    padding: 48px 0;
  }
`;

const SectionTitle = styled.h2`
  font-size: 1.5rem;
  font-weight: 600;
  text-align: center;
  margin: 0 0 20px 0;
  color: ${THEME_COLORS.heading};

  @media only screen and (min-width: 992px) {
    font-size: 1.75rem;
    margin-bottom: 24px;
  }
`;

const SectionText = styled.p`
  font-size: 1rem;
  line-height: 1.6;
  text-align: center;
  color: ${THEME_COLORS.text};
  margin: 0;
  max-width: 700px;

  @media only screen and (min-width: 992px) {
    font-size: 1.125rem;
  }
`;

const CodeBlockContainer = styled.div`
  width: 100%;
  max-width: 1000px;
  margin: 0 auto;

  @media only screen and (min-width: 992px) {
    max-width: 1200px;
  }
`;

const FeaturesList = styled.div`
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-top: 0;
  width: 100%;
  max-width: 600px;
`;

const FeatureItem = styled.div`
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 0;
`;

const FeatureIcon = styled.div`
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;

  ${IconContainer} > svg {
    fill: ${THEME_COLORS.primary};
  }
`;

const FeatureText = styled.span`
  font-size: 0.9375rem;
  line-height: 1.5;
  color: ${THEME_COLORS.text};
  flex: 1;
`;
