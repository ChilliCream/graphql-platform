import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";
import { Carousel } from "react-responsive-carousel";
import styled from "styled-components";
import { GetIndexPageDataQuery } from "../../graphql-types";
import { BananaCakePop } from "../components/images/banana-cake-pop";
import { BlogPostBananaCakePopCloud } from "../components/images/blog-post-banana-cake-pop-cloud";
import { BlogPostEFMeetsGraphQL } from "../components/images/blog-post-ef-meets-graphql";
import { BlogPostHotChocolate12 } from "../components/images/blog-post-hot-chocolate-12";
import { Layout } from "../components/layout";
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
// Logos
import AdditivLogoSvg from "../images/companies/additiv.svg";
import AeiLogoSvg from "../images/companies/aei.svg";
import AtminaLogoSvg from "../images/companies/atmina.svg";
import AutoguruLogoSvg from "../images/companies/autoguru.svg";
import BdnaLogoSvg from "../images/companies/bdna.svg";
import BeyableLogoSvg from "../images/companies/beyable.svg";
import BiqhLogoSvg from "../images/companies/biqh.svg";
import CarmmunityLogoSvg from "../images/companies/carmmunity.svg";
import CompassLogoSvg from "../images/companies/compass.svg";
import E2mLogoSvg from "../images/companies/e2m.svg";
import ExlrtLogoSvg from "../images/companies/exlrt.svg";
import EzeepLogoSvg from "../images/companies/ezeep.svg";
import GiaLogoSvg from "../images/companies/gia.svg";
import HiloLogoSvg from "../images/companies/hilo.svg";
import IncloudLogoSvg from "../images/companies/incloud.svg";
import InfoslipsLogoSvg from "../images/companies/infoslips.svg";
import MotiviewLogoSvg from "../images/companies/motiview.svg";
import OrderinLogoSvg from "../images/companies/orderin.svg";
import PoweredSoftLogoSvg from "../images/companies/powered-soft.svg";
import PushpayLogoSvg from "../images/companies/pushpay.svg";
import Seven2OneLogoSvg from "../images/companies/seven-2-one.svg";
import SolyticLogoSvg from "../images/companies/solytic.svg";
import SonikaLogoSvg from "../images/companies/sonika.svg";
import SpeedwayMotorsLogoSvg from "../images/companies/speedway-motors.svg";
import SplashbackLogoSvg from "../images/companies/splashback.svg";
import SweetGeeksLogoSvg from "../images/companies/sweetgeeks.svg";
import SwissLifeLogoSvg from "../images/companies/swiss-life.svg";
import SytadelleLogoSvg from "../images/companies/sytadelle.svg";
import TravelSoftLogoSvg from "../images/companies/travel-soft.svg";
import XMLogoSvg from "../images/companies/xm.svg";
import ZioskLogoSvg from "../images/companies/ziosk.svg";
// Images
import ContactUsSvg from "../images/contact-us.svg";
import DashboardSvg from "../images/dashboard.svg";
import GetStartedSvg from "../images/get-started.svg";
import { THEME_COLORS } from "../shared-style";

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
      allMdx(
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
                  gatsbyImageData(layout: CONSTRAINED, width: 800, quality: 100)
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
    allMdx: { edges },
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
            <Link to="/blog/2021/11/22/banana-cake-pop-cloud">
              <BlogPostBananaCakePopCloud />
            </Link>
          </Slide>
          <Slide>
            <Link to="https://bananacakepop.com">
              <BananaCakePop shadow />
              <SlideContent>
                <SlideTitle>Try Banana Cake Pop Preview 22</SlideTitle>
                <SlideDescription>
                  Our GraphQL IDE to explore schemas, execute operations and get
                  deep performance insights.
                </SlideDescription>
              </SlideContent>
            </Link>
          </Slide>
          <Slide>
            <Link to="/blog/2021/09/27/hot-chocolate-12">
              <BlogPostHotChocolate12 />
            </Link>
          </Slide>
          <Slide>
            <Link to="/blog/2020/03/18/entity-framework">
              <BlogPostEFMeetsGraphQL />
              <SlideContent>
                <SlideTitle>Entity Framework meets GraphQL</SlideTitle>
                <SlideDescription>
                  Get started with Hot Chocolate and Entity Framework
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
              Creating a GraphQL API with Hot Chocolate is very easy. Check out
              our startup guide and see how simple it is to create your first
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
                const featuredImage =
                  node?.frontmatter!.featuredImage?.childImageSharp
                    ?.gatsbyImageData;

                return (
                  <Article key={`article-${node.id}`}>
                    <Link to={node.frontmatter!.path!}>
                      {featuredImage && (
                        <GatsbyImage
                          image={featuredImage}
                          alt={node.frontmatter!.title}
                        />
                      )}
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
              <Logo width={140}>
                <Link to="https://additiv.com">
                  <AdditivLogoSvg />
                </Link>
              </Logo>
              <Logo width={160}>
                <Link to="https://aeieng.com">
                  <AeiLogoSvg />
                </Link>
              </Logo>
              <Logo width={100}>
                <Link to="https://atmina.de">
                  <AtminaLogoSvg />
                </Link>
              </Logo>
              <Logo width={180}>
                <Link to="https://www.autoguru.com.au">
                  <AutoguruLogoSvg />
                </Link>
              </Logo>
              <Logo width={150}>
                <Link to="https://bdna.com.au">
                  <BdnaLogoSvg />
                </Link>
              </Logo>
              <Logo width={150}>
                <Link to="https://www.beyable.com">
                  <BeyableLogoSvg />
                </Link>
              </Logo>
              <Logo width={100}>
                <Link to="https://www.biqh.com">
                  <BiqhLogoSvg />
                </Link>
              </Logo>
              <Logo width={180}>
                <Link to="https://carmmunity.io">
                  <CarmmunityLogoSvg />
                </Link>
              </Logo>
              <Logo width={180}>
                <Link to="https://www.compass.education">
                  <CompassLogoSvg />
                </Link>
              </Logo>
              <Logo width={90}>
                <Link to="https://www.e2m.energy">
                  <E2mLogoSvg />
                </Link>
              </Logo>
              <Logo width={130}>
                <Link to="https://www.exlrt.com">
                  <ExlrtLogoSvg />
                </Link>
              </Logo>
              <Logo width={100}>
                <Link to="https://www.ezeep.com">
                  <EzeepLogoSvg />
                </Link>
              </Logo>
              <Logo width={120}>
                <Link to="https://gia.ch">
                  <GiaLogoSvg />
                </Link>
              </Logo>
              <Logo width={70}>
                <Link to="https://www.hiloenergie.com">
                  <HiloLogoSvg />
                </Link>
              </Logo>
              <Logo width={200}>
                <Link to="https://www.incloud.de">
                  <IncloudLogoSvg />
                </Link>
              </Logo>
              <Logo width={130}>
                <Link to="https://www.infoslips.com">
                  <InfoslipsLogoSvg />
                </Link>
              </Logo>
              <Logo width={160}>
                <Link to="https://motitech.co.uk">
                  <MotiviewLogoSvg />
                </Link>
              </Logo>
              <Logo width={160}>
                <Link to="https://orderin.co.za">
                  <OrderinLogoSvg />
                </Link>
              </Logo>
              <Logo width={110}>
                <Link to="https://poweredsoft.com">
                  <PoweredSoftLogoSvg />
                </Link>
              </Logo>
              <Logo width={180}>
                <Link to="https://pushpay.com">
                  <PushpayLogoSvg />
                </Link>
              </Logo>
              <Logo width={120}>
                <Link to="https://www.seven2one.de">
                  <Seven2OneLogoSvg />
                </Link>
              </Logo>
              <Logo width={150}>
                <Link to="https://www.solytic.com">
                  <SolyticLogoSvg />
                </Link>
              </Logo>
              <Logo width={130}>
                <Link to="https://sonika.se">
                  <SonikaLogoSvg />
                </Link>
              </Logo>
              <Logo width={120}>
                <Link to="https://www.speedwaymotors.com">
                  <SpeedwayMotorsLogoSvg />
                </Link>
              </Logo>
              <Logo width={180}>
                <Link to="https://splashback.io">
                  <SplashbackLogoSvg />
                </Link>
              </Logo>
              <Logo width={120}>
                <Link to="https://sweetgeeks.dk">
                  <SweetGeeksLogoSvg />
                </Link>
              </Logo>
              <Logo width={110}>
                <Link to="https://www.swisslife.ch">
                  <SwissLifeLogoSvg />
                </Link>
              </Logo>
              <Logo width={160}>
                <Link to="https://www.sytadelle.fr">
                  <SytadelleLogoSvg />
                </Link>
              </Logo>
              <Logo width={180}>
                <Link to="https://travel-soft.com">
                  <TravelSoftLogoSvg />
                </Link>
              </Logo>
              <Logo width={120}>
                <Link to="https://xm.com">
                  <XMLogoSvg />
                </Link>
              </Logo>
              <Logo width={120}>
                <Link to="https://www.ziosk.com">
                  <ZioskLogoSvg />
                </Link>
              </Logo>
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

const Articles = styled.ul`
  display: flex;
  flex-direction: column;
  align-items: stretch;
  justify-content: space-around;
  margin: 0 0 20px;
  list-style-type: none;

  @media only screen and (min-width: 860px) {
    flex-direction: row;
    flex-wrap: wrap;
  }
`;

const Article = styled.li`
  display: flex;
  margin: 20px 0 0;
  width: 100%;
  border-radius: var(--border-radius);
  box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);

  > a {
    flex: 1 1 auto;
  }

  > a > .gatsby-image-wrapper {
    border-radius: var(--border-radius) var(--border-radius) 0 0;
  }

  @media only screen and (min-width: 860px) {
    width: 30%;
  }
`;

const ArticleMetadata = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 15px 20px 7px;
  font-size: 0.778em;
  color: ${THEME_COLORS.text};
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

  > a > svg {
    fill: ${THEME_COLORS.text};
    transition: fill 0.2s ease-in-out;

    &:hover {
      fill: ${THEME_COLORS.heading};
    }
  }
`;
