import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { Link } from "../misc/link";

interface BlogArticleLinkProperties {
  date: string;
  path: string;
  title: string;
}

export const BlogArticleLink: FunctionComponent<BlogArticleLinkProperties> = ({
  date,
  path,
  title,
}) => {
  return (
    <Link to={path}>
      <Title>{title}</Title>
      <PublishDate>{date}</PublishDate>
    </Link>
  );
};

const Title = styled.h1``;

const PublishDate = styled.div``;
