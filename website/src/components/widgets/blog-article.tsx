import { Disqus } from "gatsby-plugin-disqus";
import React, { FunctionComponent } from "react";
import styled from "styled-components";

interface BlogArticleProperties {
  author: string;
  date: string;
  htmlContent: string;
  title: string;
}

export const BlogArticle: FunctionComponent<BlogArticleProperties> = ({
  author,
  date,
  htmlContent,
  title,
}) => {
  const disqusConfig = {
    // url: location.href,
    // identifier: location.pathname,
    title: title,
  };

  return (
    <Container>
      <Title>{title}</Title>
      <Subtitle>
        Written by <em>{author}</em>, published on <em>{date}</em>
      </Subtitle>
      <Content dangerouslySetInnerHTML={{ __html: htmlContent }} />
      <DisqusWrapper config={disqusConfig} />
    </Container>
  );
};

const Container = styled.article`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  padding-top: 20px;
  width: 100%;
  max-width: 1100px;
`;

const Title = styled.h1`
  margin: 0 20px;
  font-size: 2em;
`;

const Subtitle = styled.div`
  margin: 10px 20px 0;
  font-size: 0.875em;
  color: #bbb;
`;

const Content = styled.div`
  margin: 20px 20px 40px;

  > p {
    margin-bottom: 1em;
    line-height: 1.5em;
  }

  > h2 {
    font-size: 1.667em;
    line-height: 2em;
  }
`;

const DisqusWrapper = styled(Disqus)`
  margin: 0 20px;
`;
