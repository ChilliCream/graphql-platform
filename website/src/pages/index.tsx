import { graphql, useStaticQuery } from "gatsby";
import React, { FunctionComponent } from "react";
import { Carousel } from "react-responsive-carousel";
import styled from "styled-components";
import { GetStartpageHeaderQuery } from "../../graphql-types";
import BananaCakepop from "../components/images/banana-cakepop";
import SEO from "../components/misc/seo";
import Layout from "../components/structure/layout";

const IndexPage: FunctionComponent = () => {
  const data = useStaticQuery<GetStartpageHeaderQuery>(graphql`
    query getStartpageHeader {
      file(relativePath: { eq: "startpage-header.svg" }) {
        publicURL
      }
    }
  `);

  return (
    <Layout>
      <SEO title="Home" />
      <Intro url={data.file!.publicURL!}>
        <Title>The Ulitimate GraphQL Platform</Title>
        <Slideshow autoPlay showStatus={false} showThumbs={false}>
          <Slide>
            <BananaCakepop />
          </Slide>
        </Slideshow>
      </Intro>
    </Layout>
  );
};

export default IndexPage;

const Intro = styled.section<{ url: string }>`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 0;
  background-image: url("${props => props.url}");
  background-attachment: scroll;
  background-position-x: 50%;
  background-position-y: 100%;
  background-repeat: no-repeat;
  background-size: cover;
`;

const Slideshow = styled(Carousel)`
  flex: 0 0 auto;
  width: 100%;

  > .control-next {
  }

  > .control-prev {
  }
`;

const Slide = styled.div`
  margin: 0 auto;
  width: 100%;

  @media only screen and (min-width: 992px) {
    width: 900px;
  }

  @media only screen and (min-width: 1200px) {
    width: 1100px;
  }
`;

const Title = styled.h1`
  flex: 0 0 auto;
  font-size: 3em;
  color: #fff;
`;
