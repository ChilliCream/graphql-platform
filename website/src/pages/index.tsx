import { graphql, useStaticQuery } from "gatsby";
import React, { FC } from "react";
import { Carousel } from "react-responsive-carousel";
import styled from "styled-components";

import { BananaCakePop } from "@/components/images/banana-cake-pop";
import { BlogPostBananaCakePopApis } from "@/components/images/blog-post-banana-cake-pop-apis";
import { BlogPostGraphQLFusion } from "@/components/images/blog-post-graphql-fusion";
import { BlogPostHotChocolate13 } from "@/components/images/blog-post-hot-chocolate-13";
import { NewsletterMay2024 } from "@/components/images/newsletter-may-2024";
import { Layout } from "@/components/layout";
import { Link } from "@/components/misc/link";
import {
  ContentContainer,
  EnvelopeIcon,
  ImageContainer,
  Section,
  SectionRow,
  SectionTitle,
  SlackIcon,
} from "@/components/misc/marketing-elements";
import { Hero, Intro } from "@/components/misc/page-elements";
import { SEO } from "@/components/misc/seo";
import { Artwork } from "@/components/sprites";
import {
  CompaniesSection,
  MostRecentBlogPostsSection,
} from "@/components/widgets";
import { GetIndexPageDataQuery } from "@/graphql-types";
import { THEME_COLORS } from "@/shared-style";

// Artwork
import { FullstackWorkshop } from "@/components/images/fullstack-workshop";
import { SrOnly } from "@/components/misc/sr-only";
import ContactUsSvg from "@/images/artwork/contact-us.svg";
import DashboardSvg from "@/images/artwork/dashboard.svg";
import GetStartedSvg from "@/images/artwork/get-started.svg";

const IndexPage: FC = () => {
  const data = useStaticQuery<GetIndexPageDataQuery>(graphql`
    query getIndexPageData {
      site {
        siteMetadata {
          tools {
            slack
          }
        }
      }
      docNav: file(
        sourceInstanceName: { eq: "docs" }
        relativePath: { eq: "docs.json" }
      ) {
        products: childrenDocsJson {
          path
          latestStableVersion
        }
      }
    }
  `);
  const latestHcVersion = data.docNav?.products?.find(
    (product) => product?.path === "hotchocolate"
  )?.latestStableVersion;

  return (
    <Layout>
      <SEO title="Home" />
      <Intro>
        <Hero>The Ultimate GraphQL Platform</Hero>
        <Slideshow
          autoPlay
          infiniteLoop
          swipeable
          emulateTouch
          interval={15000}
          showArrows={false}
          showStatus={false}
          showThumbs={false}
        >
          <Slide>
            <Link to="/blog/2024/05/21/newsletter-may">
              <NewsletterMay2024 />
            </Link>
          </Slide>
          <Slide>
            <Link to="/blog/2024/04/01/fullstack-workshop">
              <FullstackWorkshop />
            </Link>
          </Slide>
          <Slide>
            <Link to="/blog/2023/08/15/graphql-fusion">
              <BlogPostGraphQLFusion />
            </Link>
          </Slide>
          <Slide>
            <Link to="/blog/2023/02/08/new-in-hot-chocolate-13">
              <BlogPostHotChocolate13 />
            </Link>
          </Slide>
          <Slide>
            <Link to="/blog/2023/03/15/banana-cake-pop-graphql-apis">
              <BlogPostBananaCakePopApis />
            </Link>
          </Slide>
          <Slide>
            <Link to="/products/bananacakepop">
              <BananaCakePop shadow />
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
            <Artwork {...DashboardSvg} />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>
              What Is the ChilliCream GraphQL Platform?
            </SectionTitle>
            <p>
              It's a new way of defining modern APIs which are strongly typed
              from server to client. Fetch once with no more under- or
              over-fetching, just the right amount.
            </p>
            <Link to="/platform">
              Learn more<SrOnly> about the ChilliCream GraphQL platform</SrOnly>
            </Link>
          </ContentContainer>
        </SectionRow>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer>
            <Artwork {...GetStartedSvg} />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Get Started</SectionTitle>
            <p>
              Creating a GraphQL .NET API with Hot Chocolate is very easy. Check
              out our startup guide and see how simple it is to create your
              first API.
            </p>
            <Link to={`/docs/hotchocolate/${latestHcVersion}`}>
              Learn more<SrOnly> on how to build GraphQL .NET APIs</SrOnly>
            </Link>
          </ContentContainer>
        </SectionRow>
      </Section>
      <CompaniesSection />
      <Section>
        <SectionRow>
          <ImageContainer>
            <Artwork {...ContactUsSvg} />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>What’s Your Story?</SectionTitle>
            <p>
              {
                "We would be thrilled to hear your customer success story with Hot Chocolate! Write us an "
              }
              <a href="mailto:contact@chillicream.com">
                <EnvelopeIcon />
              </a>
              {" or chat with us on "}
              <Link to={data.site!.siteMetadata!.tools!.slack!}>
                <SlackIcon />
              </Link>
              {" to get in touch with us!"}
            </p>
          </ContentContainer>
        </SectionRow>
      </Section>
      <MostRecentBlogPostsSection />
    </Layout>
  );
};

export default IndexPage;

const Slideshow = styled(Carousel)`
  ul,
  li {
    margin: 0;
    padding: 0;
  }

  > .carousel {
    position: relative;

    .control-dots {
      position: absolute;
      bottom: 0;
      width: 100%;

      display: flex;
      flex-direction: row;
      justify-content: center;
      list-style: none;

      > .dot {
        flex: 0 0 26px;
        margin: 0 5px;
        border-radius: 2px;
        height: 6px;
        background-color: ${THEME_COLORS.textContrast};
        opacity: 0.5;
        cursor: pointer;
        transition: background-color 0.2s ease-in-out, opacity 0.2s ease-in-out;

        &.selected {
          background-color: ${THEME_COLORS.textContrast};
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
      margin-bottom: 25px;

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
    border-radius: var(--border-radius);
    padding: 20px;
    background-color: rgba(0, 0, 0, 0.6);
  }

  @media only screen and (min-width: 992px) {
    right: 25%;
    left: 25%;
    margin: 0 auto;
    max-width: 600px;
  }

  @media only screen and (min-width: 1200px) {
    right: 30%;
    left: 30%;
    margin: 0 auto;
    max-width: 800px;
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
    color: ${THEME_COLORS.textContrast};
  }
`;

const SlideDescription = styled.p`
  display: none;
  flex: 0 0 auto;
  margin-bottom: 0;
  font-size: 1.111em;
  color: ${THEME_COLORS.textContrast};

  @media only screen and (min-width: 768px) {
    display: initial;
  }
`;
