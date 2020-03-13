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
      <PublishDate>{date}</PublishDate>
      <Author>{author}</Author>
      <Content dangerouslySetInnerHTML={{ __html: htmlContent }} />
      <Disqus config={disqusConfig} />
    </Container>
  );
};

const Container = styled.article`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  width: 100%;
  max-width: 1100px;
`;

const Title = styled.h1`
  font-size: 2em;
`;

const PublishDate = styled.div``;

const Author = styled.div``;

const Content = styled.div``;
