import { graphql, useStaticQuery } from "gatsby";
import Img, { FluidObject } from "gatsby-image";
import React, { FunctionComponent } from "react";
import { Carousel } from "react-responsive-carousel";
import styled from "styled-components";
import { GetIndexPageDataQuery } from "../../graphql-types";
import { BananaCakePop } from "../components/images/banana-cake-pop";
import { EFMeetsGraphQL } from "../components/images/ef-meets-graphql";
import { Link } from "../components/misc/link";
import {
  ContentContainer,
  EnvelopeIcon,
  ImageContainer,
  Section,
  SectionRow,
  SectionTitle,
  SlackIcon,
} from "../components/misc/marketing-elements";
import { Hero, Intro } from "../components/misc/page-elements";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";

import AtminaLogoSvg from "../images/companies/atmina.svg";
import AutoguruLogoSvg from "../images/companies/autoguru.svg";
import GiaLogoSvg from "../images/companies/gia.svg";
import Seven2OneLogoSvg from "../images/companies/seven-2-one.svg";
import SwissLifeLogoSvg from "../images/companies/swiss-life.svg";
import ContactUsSvg from "../images/contact-us.svg";
import DashboardSvg from "../images/dashboard.svg";
import GetStartedSvg from "../images/get-started.svg";

const IndexPage: FunctionComponent = () => {
  const data = useStaticQuery<GetIndexPageDataQuery>(graphql`
    query getIndexPageData {
      site {
        siteMetadata {
          tools {
            slack
          }
        }
      }
      allMarkdownRemark(
        limit: 3
        filter: { frontmatter: { path: { glob: "/blog/**/*" } } }
        sort: { fields: [frontmatter___date], order: DESC }
      ) {
        edges {
          node {
            id
            fields {
              readingTime {
                text
              }
            }
            frontmatter {
              featuredImage {
                childImageSharp {
                  fluid(maxWidth: 800) {
                    ...GatsbyImageSharpFluid
                  }
                }
              }
              path
              title
              date(formatString: "MMMM DD, YYYY")
            }
          }
        }
      }
    }
  `);
  const {
    allMarkdownRemark: { edges },
  } = data;

  return (
    <Layout>
      <SEO title="Home" />
      <Intro>
        <Hero>The Ultimate GraphQL Platform</Hero>
        <Slideshow
          autoPlay
          infiniteLoop
          swipeable
          interval={15000}
          showStatus={false}
          showThumbs={false}
        >
          <Slide>
            <Link to="/blog/2020/03/18/entity-framework">
              <EFMeetsGraphQL />
              <SlideContent>
                <SlideTitle>Entity Frameworks meets GraphQL</SlideTitle>
                <SlideDescription>
                  Get started with Hot Chocolate and Entity Framework
                </SlideDescription>
              </SlideContent>
            </Link>
          </Slide>
          <Slide>
            <Link to="/docs/bananacakepop">
              <BananaCakePop />
              <SlideContent>
                <SlideTitle>Banana Cake Pop</SlideTitle>
                <SlideDescription>
                  Our GraphQL IDE to explore schemas, execute operations and get
                  deep performance insights.
                </SlideDescription>
              </SlideContent>
            </Link>
          </Slide>
        </Slideshow>
      </Intro>
      <Section>
        <SectionRow>
          <ImageContainer>
            <DashboardSvg />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>
              What is the ChilliCream GraphQL platform?
            </SectionTitle>
            <p>
              It's a new way of defining modern APIs which are strongly typed
              from server to client. Fetch once with no more under- or
              over-fetching, just the right amount.
            </p>
            <Link to="/platform">Learn more</Link>
          </ContentContainer>
        </SectionRow>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer>
            <GetStartedSvg />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Get Started</SectionTitle>
            <p>
              Creating a GraphQL API with Hot Chocolate is very easy. Checkout
              our startup guide and see how simple it's to create your first
              API.
            </p>
            <Link to="/docs/hotchocolate">Learn more</Link>
          </ContentContainer>
        </SectionRow>
      </Section>
      <Section>
        <SectionRow>
          <ContentContainer noImage>
            <SectionTitle centerAlways>From our Blog</SectionTitle>
            <Articles>
              {edges.map(({ node }) => {
                const featuredImage = node?.frontmatter!.featuredImage
                  ?.childImageSharp?.fluid as FluidObject;

                return (
                  <Article key={`article-${node.id}`}>
                    <Link to={node.frontmatter!.path!}>
                      {featuredImage && <Img fluid={featuredImage} />}
                      <ArticleMetadata>
                        {node.frontmatter!.date!} ・{" "}
                        {node.fields!.readingTime!.text!}
                      </ArticleMetadata>
                      <ArticleTitle>{node.frontmatter!.title}</ArticleTitle>
                    </Link>
                  </Article>
                );
              })}
            </Articles>
          </ContentContainer>
        </SectionRow>
      </Section>
      <Section>
        <SectionRow>
          <ContentContainer noImage>
            <SectionTitle centerAlways>Companies who trust us</SectionTitle>
            <Logos>
              <Logo width={180}>
                <AutoguruLogoSvg />
              </Logo>
              <Logo width={100}>
                <AtminaLogoSvg />
              </Logo>
              <Logo width={120}>
                <GiaLogoSvg />
              </Logo>
              <Logo width={120}>
                <Seven2OneLogoSvg />
              </Logo>
              {false && (
                <Logo width={100}>
                  <SwissLifeLogoSvg />
                </Logo>
              )}
            </Logos>
          </ContentContainer>
        </SectionRow>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer>
            <ContactUsSvg />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>What's your story?</SectionTitle>
            <p>
              We would be thrilled to hear your customer success story with Hot
              Chocolate! Write us an{" "}
              <a href="mailto:contact@chillicream.com">
                <EnvelopeIcon />
              </a>{" "}
              or chat with us on{" "}
              <a href={data.site!.siteMetadata!.tools!.slack!}>
                <SlackIcon />
              </a>{" "}
              to get in touch with us!
            </p>
          </ContentContainer>
        </SectionRow>
      </Section>
    </Layout>
  );
};

export default IndexPage;

const Slideshow = styled(Carousel)`
  flex: 0 0 auto;
  width: 100%;

  ul,
  li {
    margin: 0;
    padding: 0;
  }

  > .carousel {
    position: relative;

    > .control-next,
    > .control-prev {
      display: none;
    }

    .control-dots {
      display: flex;
      flex-direction: row;
      justify-content: center;
      margin-top: 20px;
      list-style: none;

      > .dot {
        flex: 0 0 26px;
        margin: 0 5px;
        border-radius: 2px;
        height: 6px;
        background-color: #fff;
        opacity: 0.5;
        cursor: pointer;
        transition: background-color 0.2s ease-in-out, opacity 0.2s ease-in-out;

        &.selected {
          background-color: #fff;
          opacity: 1;

          &:hover {
            opacity: 1;
          }
        }

        &:hover {
          opacity: 0.85;
        }
      }
    }

    .slider {
      position: relative;
      display: flex;
      list-style: none;

      > .slide {
        position: relative;
        display: flex;
        align-items: center;
        justify-content: center;
        min-width: 100%;
      }
    }
  }
`;

const Slide = styled.div`
  margin: 0 auto;
  width: 100%;

  .gatsby-image-wrapper {
    display: flex;
    align-items: center;
    justify-content: center;
  }

  @media only screen and (min-width: 992px) {
    width: 800px;
  }

  @media only screen and (min-width: 1200px) {
    width: 1000px;
  }
`;

const SlideContent = styled.div`
  display: flex;
  flex-direction: column;

  @media only screen and (min-width: 768px) {
    position: absolute;
    right: 20%;
    bottom: 20%;
    left: 20%;
    display: flex;
    flex-direction: column;
    border-radius: 5px;
    padding: 20px;
    background-color: rgba(0, 0, 0, 0.6);
  }

  @media only screen and (min-width: 992px) {
    right: 25%;
    left: 25%;
  }

  @media only screen and (min-width: 1200px) {
    right: 30%;
    left: 30%;
  }
`;

const SlideTitle = styled.h2`
  flex: 0 0 auto;
  margin-top: 10px;
  font-size: 1em;
  text-align: center;

  @media only screen and (min-width: 768px) {
    margin-top: 0;
    margin-bottom: 10px;
    font-size: 1.667em;
    text-align: initial;
    color: #fff;
  }
`;

const SlideDescription = styled.p`
  display: none;
  flex: 0 0 auto;
  margin-bottom: 0;
  font-size: 1.111em;
  color: #fff;

  @media only screen and (min-width: 768px) {
    display: initial;
  }
`;

const Articles = styled.ul`
  display: flex;
  flex-direction: column;
  align-items: stretch;
  justify-content: space-around;
  margin: 0 0 20px;
  list-style-type: none;

  @media only screen and (min-width: 820px) {
    flex-direction: row;
    flex-wrap: wrap;
  }
`;

const Article = styled.li`
  display: flex;
  margin: 20px 0 0;
  width: 100%;
  border-radius: 4px;
  box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);

  > a {
    flex: 1 1 auto;
  }

  > a > .gatsby-image-wrapper {
    border-radius: 4px 4px 0 0;
  }

  @media only screen and (min-width: 820px) {
    width: 30%;
  }
`;

const ArticleMetadata = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 15px 20px 7px;
  font-size: 0.778em;
  color: #667;
`;

const ArticleTitle = styled.h1`
  margin: 0 20px 15px;
  font-size: 1em;
`;

const Logos = styled.div`
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  align-items: center;
  justify-content: center;
`;

const Logo = styled.div<{ width?: number }>`
  flex: 0 0 auto;
  margin: 30px;
  width: ${({ width }) => width || 160}px;

  > svg {
    fill: #667;
    transition: fill 0.2s ease-in-out;

    &:hover {
      fill: #333;
    }
  }
`;
