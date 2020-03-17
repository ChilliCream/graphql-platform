import { Disqus } from "gatsby-plugin-disqus";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { Link } from "../misc/link";

interface BlogArticleProperties {
  author: string;
  authorImageUrl: string;
  authorUrl: string;
  baseUrl: string;
  date: string;
  htmlContent: string;
  path: string;
  title: string;
}

export const BlogArticle: FunctionComponent<BlogArticleProperties> = ({
  author,
  authorImageUrl,
  authorUrl,
  baseUrl,
  date,
  htmlContent,
  path,
  title,
}) => {
  const disqusConfig = {
    url: baseUrl + path,
    identifier: path,
    title: title,
  };

  return (
    <Container>
      <Title>{title}</Title>
      <Subtitle>
        <Author>
          <Link to={authorUrl}>
            <AuthorImage src={authorImageUrl} /> {author}
          </Link>
        </Author>
        <Date>{date}</Date>
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
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 0 20px 20px;
  font-size: 0.875em;
  color: #666;
`;

const Author = styled.div`
  flex: 1 1 auto;

  > a {
    color: #666;
  }
`;

const AuthorImage = styled.img`
  margin-right: 0.5em;
  border-radius: 20px;
  width: 40px;
  vertical-align: middle;
`;

const Date = styled.div`
  flex: 1 1 auto;
  text-align: right;
`;

const Content = styled.div`
  margin: 0 20px 40px;
`;

const DisqusWrapper = styled(Disqus)`
  margin: 0 20px;
`;
