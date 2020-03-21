import { Disqus } from "gatsby-plugin-disqus";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { Maybe } from "../../../graphql-types";
import { Link } from "../misc/link";

interface BlogArticleProperties {
  author: string;
  authorImageUrl: string;
  authorUrl: string;
  baseUrl: string;
  date: string;
  htmlContent: string;
  path: string;
  readingTime: string;
  tags?: Maybe<string>[] | null;
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
  readingTime,
  tags,
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
      <Metadata>
        <AuthorLink to={authorUrl}>
          <AuthorImage src={authorImageUrl} />
          {author}
        </AuthorLink>{" "}
        ・ {date} ・ {readingTime}
      </Metadata>
      {tags && tags.length > 0 && (
        <Tags>
          {tags.map(tag => (
            <Tag>{tag}</Tag>
          ))}
        </Tags>
      )}
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
  max-width: 800px;
`;

const Title = styled.h1`
  margin-right: 20px;
  margin-left: 20px;
  font-size: 2em;
`;

const Metadata = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 0 20px 20px;
  font-size: 0.778em;
`;

const AuthorLink = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  color: #666;
`;

const AuthorImage = styled.img`
  flex: 0 0 auto;
  margin-right: 0.5em;
  border-radius: 15px;
  width: 30px;
`;

const Tags = styled.ul`
  margin: 0;
  padding: 0 20px 20px;
  list-style-type: none;
`;

const Tag = styled.li`
  display: inline-block;
  margin: 0 5px 0 0;
  border-radius: 4px;
  padding: 5px 15px;
  background-color: #f40010;
  font-size: 0.722em;
  letter-spacing: 0.05em;
  color: #fff;
`;

const Content = styled.div`
  margin: 0 20px 40px;
`;

const DisqusWrapper = styled(Disqus)`
  margin: 0 20px;
`;
