import { Link, PageProps } from "gatsby";
import React, { FC, ReactNode } from "react";
import styled from "styled-components";

import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc/seo";

const productAndVersionPattern = /^\/docs\/([\w-]+)(?:\/(v\d+))?/;

const NotFoundPage: FC<PageProps> = ({ location }) => {
  const path = location.pathname;

  let content: ReactNode = (
    <>
      <Title>NOT FOUND</Title>
      <Content>
        <p>The page you&#39;re looking for doesn&#39;t exist.</p>
        <Link to="/">Return to the homepage</Link>
      </Content>
    </>
  );

  const productMatch = productAndVersionPattern.exec(path);

  if (productMatch) {
    // The user tried to view a documentation page.

    const product = productMatch[1] || "";
    const version = productMatch[2] || "";

    let newUrl = "/docs/" + product;

    if (version) {
      newUrl += "/" + version;
    }

    content = (
      <>
        <Title>NOT FOUND</Title>
        <Content>
          <p>
            The page you&#39;re looking for doesn&#39;t exist for this version
            of the software or it was moved.
          </p>

          <div>
            <Link to="/">Return to the homepage</Link>
            <Separator>&mdash;</Separator>
            <Link to={newUrl}>Return to the documentation</Link>
          </div>
        </Content>
      </>
    );
  }

  return (
    <SiteLayout disableStars>
      <SEO title="404: Not found" />
      <Container>
        <Article>{content}</Article>
      </Container>
    </SiteLayout>
  );
};

export default NotFoundPage;

const Container = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  width: 100%;

  @media only screen and (min-width: 860px) {
    padding: 20px 10px 0;
    max-width: 820px;
  }
`;

const Article = styled.article`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  margin-bottom: 60px;
  padding-bottom: 20px;

  @media only screen and (min-width: 860px) {
    border-radius: var(--border-radius);
  }
`;

const Title = styled.h1`
  margin-top: 20px;
  margin-right: 20px;
  margin-left: 20px;
  font-size: 2rem;

  @media only screen and (min-width: 860px) {
    margin-right: 50px;
    margin-left: 50px;
  }
`;

const Content = styled.div`
  > * {
    padding-right: 20px;
    padding-left: 20px;
    line-height: normal;
  }

  @media only screen and (min-width: 860px) {
    > * {
      padding-right: 50px;
      padding-left: 50px;
    }
  }
`;

const Separator = styled.span`
  margin: 0px 20px;
`;
