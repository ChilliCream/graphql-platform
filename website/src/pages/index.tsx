import { graphql, useStaticQuery } from "gatsby";
import React, { FunctionComponent } from "react";
import { Carousel } from "react-responsive-carousel";
import styled from "styled-components";
import { GetStartpageDataQuery } from "../../graphql-types";
import BananaCakepop from "../components/images/banana-cakepop";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";

const IndexPage: FunctionComponent = () => {
  const data = useStaticQuery<GetStartpageDataQuery>(graphql`
    query getStartpageData {
      arrowLeft: file(relativePath: { eq: "arrow-left.svg" }) {
        publicURL
      }
      arrowRight: file(relativePath: { eq: "arrow-right.svg" }) {
        publicURL
      }
      bg: file(relativePath: { eq: "startpage-header.svg" }) {
        publicURL
      }
    }
  `);

  return (
    <Layout>
      <SEO title="Home" />
      <Intro url={data.bg!.publicURL!}>
        <Title>The Ultimate GraphQL Platform</Title>
        <Slideshow
          arrowLeftUrl={data.arrowLeft!.publicURL!}
          arrowRightUrl={data.arrowRight!.publicURL!}
          autoPlay
          showStatus={false}
          showThumbs={false}
        >
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

const Intro = styled.section<{ url: string }>`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 25px;
  width: 100%;
  background-image: url("${props => props.url}");
  background-attachment: scroll;
  background-position-x: 50%;
  background-position-y: 100%;
  background-repeat: no-repeat;
  background-size: cover;

  @media only screen and (min-width: 992px) {
    padding: 60px 0;
  }
`;

const Slideshow = styled(Carousel)<{
  arrowLeftUrl: string;
  arrowRightUrl: string;
}>`
  flex: 0 0 auto;
  width: 100%;

  > .carousel {
    position: relative;

    > .control-next,
    > .control-prev {
      position: absolute;
      z-index: 10;
      top: 0;
      display: block;
      width: 40px;
      height: 100%;
      opacity: 0.5;
      background-attachment: scroll;
      background-position-x: 50%;
      background-position-y: 50%;
      background-repeat: no-repeat;
      background-size: 80%;
      transition: background-size 0.2s ease-in-out, opacity 0.2s ease-in-out;

      &:hover {
        opacity: 0.6;
        background-size: 90%;
      }

      &.control-next {
        right: 0;
        background-image: url("${props => props.arrowRightUrl}");
      }

      &.control-prev {
        left: 0;
        background-image: url("${props => props.arrowLeftUrl}");
      }

      @media only screen and (min-width: 992px) {
        &.control-next {
          right: 40px;
        }

        &.control-prev {
          left: 40px;
        }
      }
    }

    .control-dots {
      display: flex;
      flex-direction: row;
      justify-content: center;
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
        min-width: 100%;
      }
    }
  }
`;

const Slide = styled.div`
  margin: 0 auto;
  width: 80%;

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
  padding: 20px;

  @media only screen and (min-width: 768px) {
    position: absolute;
    right: 20%;
    bottom: 20%;
    left: 20%;
    display: flex;
    flex-direction: column;
    border-radius: 5px;
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
  margin-bottom: 10px;
  font-size: 1.667em;
  text-align: center;

  @media only screen and (min-width: 768px) {
    text-align: initial;
    color: #fff;
  }
`;

const SlideDescription = styled.p`
  flex: 0 0 auto;
  font-size: 1.111em;
  text-align: center;

  @media only screen and (min-width: 768px) {
    text-align: initial;
    color: #fff;
  }
`;

const Title = styled.h1`
  flex: 0 0 auto;
  margin-bottom: 20px;
  font-size: 2.222em;
  text-align: center;
  color: #fff;

  @media only screen and (min-width: 768px) {
    margin-bottom: 20px;
  }
`;
