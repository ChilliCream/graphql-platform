import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { Link } from "../misc/link";

interface BlogArticleMetadataProperties {
  author: string;
  authorImageUrl: string;
  authorUrl: string;
  date: string;
  readingTime: string;
}

export const BlogArticleMetadata: FunctionComponent<BlogArticleMetadataProperties> = ({
  author,
  authorImageUrl,
  authorUrl,
  date,
  readingTime,
}) => {
  return (
    <Metadata>
      <AuthorLink to={authorUrl}>
        <AuthorImage src={authorImageUrl} />
        {author}
      </AuthorLink>{" "}
      ・ {date} ・ {readingTime}
    </Metadata>
  );
};

const Metadata = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 0 20px 20px;
  font-size: 0.778em;

  @media only screen and (min-width: 800px) {
    margin: 0 50px 20px;
  }
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
