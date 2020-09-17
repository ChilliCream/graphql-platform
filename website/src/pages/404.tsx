import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";

const NotFoundPage: FunctionComponent = () => (
  <Layout>
    <SEO title="404: Not found" />
    <Container>
      <Article>
        <Title>NOT FOUND</Title>
        <Content>
          <p>You just hit a route that doesn&#39;t exist... the sadness.</p>
        </Content>
      </Article>
    </Container>
  </Layout>
);

export default NotFoundPage;

const Container = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  width: 100%;
  max-width: 820px;

  @media only screen and (min-width: 820px) {
    padding: 20px 10px 0;
  }
`;

const Article = styled.article`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  margin-bottom: 60px;
  padding-bottom: 20px;

  @media only screen and (min-width: 820px) {
    border-radius: 4px;
    box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);
  }
`;

const Title = styled.h1`
  margin-top: 20px;
  margin-right: 20px;
  margin-left: 20px;
  font-size: 2em;

  @media only screen and (min-width: 820px) {
    margin-right: 50px;
    margin-left: 50px;
  }
`;

const Content = styled.div`
  > * {
    padding-right: 20px;
    padding-left: 20px;
  }

  @media only screen and (min-width: 820px) {
    > * {
      padding-right: 50px;
      padding-left: 50px;
    }
  }
`;
