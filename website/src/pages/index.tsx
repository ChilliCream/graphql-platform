import { graphql, useStaticQuery } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetStartpageHeaderQuery } from "../../graphql-types";
import Layout from "../components/structure/layout";
import SEO from "../components/misc/seo";

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
      </Intro>
    </Layout>
  );
};

export default IndexPage;

const Intro = styled.section<{ url: string }>`
  display: flex;
  justify-content: center;
  padding: 60px 0;
  height: 700px;
  background-image: url("${props => props.url}");
  background-attachment: scroll;
  background-position-x: 50%;
  background-position-y: 100%;
  background-repeat: no-repeat;
  background-size: cover;
`;

const Title = styled.h1`
  font-size: 3em;
  color: #fff;
`;
