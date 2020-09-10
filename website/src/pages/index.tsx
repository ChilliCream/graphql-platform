import React, { FunctionComponent } from "react";
import { Carousel } from "react-responsive-carousel";
import styled from "styled-components";
import { BananaCakepop } from "../components/images/banana-cakepop";
import { EFMeetsGraphQL } from "../components/images/ef-meets-graphql";
import { Link } from "../components/misc/link";
import { Hero, Intro } from "../components/misc/page-elements";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";

const IndexPage: FunctionComponent = () => {
  return (
    <Layout>
      <SEO title="Home" />
      <Intro>
        <Hero>The Ultimate GraphQL Platform</Hero>
        <Slideshow
          autoPlay
          infiniteLoop
          swipeable
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
            <BananaCakepop />
            <SlideContent>
              <SlideTitle>Banana Cakepop</SlideTitle>
              <SlideDescription>
                Our tool to explore schemas, execute operations and get deep
                performance insights.
              </SlideDescription>
            </SlideContent>
          </Slide>
        </Slideshow>
      </Intro>
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
        background-color: #000;
        opacity: 0.5;
        cursor: pointer;
        transition: background-color 0.2s ease-in-out, opacity 0.2s ease-in-out;

        &.selected {
          background-color: #f40010;
          opacity: 1;

          &:hover {
            opacity: 1;
          }
        }

        &:hover {
          opacity: 0.6;
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
