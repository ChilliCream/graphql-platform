import { SiteLayout } from "@/components/layout";
import { CodeBlock } from "@/components/mdx/code-block";
import { Copy } from "@/components/mdx/copy";
import { Video } from "@/components/mdx/video";
import { IconContainer, Link, LinkButton, SEO } from "@/components/misc";
import { Icon } from "@/components/sprites";
import ArrowRightIconSvg from "@/images/icons/arrow-right.svg";
import CheckIconSvg from "@/images/icons/check.svg";
import { IsMobile, IsPhablet, IsTablet, THEME_COLORS } from "@/style";
import React, { FC } from "react";
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
  "Subscriptions",
];

const GraphQLExamplePage: FC = () => {
  return (
    <SiteLayout>
      <SEO
        title="Example GraphQL API"
        description="Explore our example GraphQL API to familiarize yourself with GraphQL and experiment with advanced features like Relay compatibility, @defer/@stream, subscriptions, and more."
      />

      <Layout>
        <HeaderLeft>
          <PageTitle>Example GraphQL API</PageTitle>

          <UrlContainer>
            <Link to={serverUrl}>{serverUrl}</Link>
            <Copy content={serverUrl} hideToast />
          </UrlContainer>
        </HeaderLeft>

        <HeaderRight>
          <OpenButtonContainer>
            <OpenButton to={serverUrl} prefetch={false}>
              Open in Nitro
              <IconContainer $size={16}>
                <Icon {...ArrowRightIconSvg} />
              </IconContainer>
            </OpenButton>
          </OpenButtonContainer>
        </HeaderRight>

        <LeftContent>
          <Title>Example Query</Title>
          <CodeBlockContainer>
            <CodeBlock
              language="graphql"
              hideLanguageIndicator
              playUrl={serverUrl}
              children={exampleQuery}
            />
          </CodeBlockContainer>

          <Title>Features</Title>
          <Features>
            {features.map((feature) => (
              <Feature key={`plan-feature-${feature}`}>
                <IconContainer $size={14} style={{ flex: "0 0 auto" }}>
                  <Icon {...CheckIconSvg} />
                </IconContainer>
                {feature}
              </Feature>
            ))}
          </Features>
        </LeftContent>
        <RightContent>
          <Title>Get to know Nitro</Title>

          <Video videoId="Nf7nX2H_iiM" />
        </RightContent>
      </Layout>
    </SiteLayout>
  );
};

export default GraphQLExamplePage;

const Layout = styled.div`
  display: grid;
  grid-template-columns: 1fr 720px 440px 1fr;
  grid-template-rows: 1fr;
  gap: 20px;

  ${IsTablet(`
    grid-template-columns: 1fr;
  `)}

  width: 100%;
  height: 100%;
  padding-top: 26px;
  overflow: visible;
`;

const HeaderLeft = styled.div`
  grid-row: 1;
  grid-column: 2;

  ${IsTablet(`
    grid-column: 1;
  `)}
`;

const HeaderRight = styled.div`
  grid-row: 1;
  grid-column: 3;

  ${IsTablet(`
    grid-row: 2;
    grid-column: 1;
  `)}
`;

const OpenButtonContainer = styled.div`
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100%;

  ${IsTablet(`
    justify-content: flex-start;
    width: 100%;
  `)}
`;

const LeftContent = styled.div`
  grid-row: 2;
  grid-column: 2;

  ${IsTablet(`
    grid-row: 3;
    grid-column: 1;
  `)}
`;

const RightContent = styled.div`
  grid-row: 2;
  grid-column: 3;

  ${IsTablet(`
    grid-row: 4;
    grid-column: 1;
  `)}
`;

const UrlContainer = styled.div`
  display: flex;
`;

const PageTitle = styled.h1`
  font-size: 2.5rem;
  font-weight: normal;
`;

const Title = styled.h2`
  margin-right: 16px;
  margin-bottom: 16px;
  margin-left: 16px;
  font-size: 2rem;
  font-weight: normal;

  @media only screen and (min-width: 860px) {
    margin-right: 0;
    margin-left: 0;
  }
`;

const OpenButton = styled(LinkButton)`
  flex-shrink: 0;
  justify-content: center;

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

  ${IsTablet(`
    width: 100%;
  `)}
`;

const CodeBlockContainer = styled.div`
  width: 100%;
  max-width: 1000px;
  margin: 0 auto;

  @media only screen and (min-width: 992px) {
    max-width: 1200px;
  }
`;

const Features = styled.ul.attrs({
  className: "text-2",
})`
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: flex-start;
  grid-row: 3;
  box-sizing: border-box;
  margin: 0;
  list-style-type: none;
`;

const Feature = styled.li`
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 8px;

  ${IconContainer} > svg {
    fill: ${THEME_COLORS.text};
  }
`;
