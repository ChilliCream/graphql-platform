import { graphql } from "gatsby";
import { Disqus } from "gatsby-plugin-disqus";
import React, { FC } from "react";
import styled from "styled-components";

import { ArticleCommentsFragment } from "@/graphql-types";

export interface ArticleCommentsProps {
  readonly data: ArticleCommentsFragment;
  readonly path: string;
  readonly title: string;
}

export const ArticleComments: FC<ArticleCommentsProps> = ({
  data,
  path,
  title,
}) => {
  const disqusConfig = {
    url: data.site!.siteMetadata!.siteUrl! + path,
    identifier: path,
    title,
  };

  return <DisqusWrapper config={disqusConfig} />;
};

export const ArticleCommentsGraphQLFragment = graphql`
  fragment ArticleComments on Query {
    site {
      siteMetadata {
        siteUrl
      }
    }
  }
`;

const DisqusWrapper = styled(Disqus)`
  margin: 0 20px 60px;

  @media only screen and (min-width: 860px) {
    margin: 0 50px 60px;
  }
`;
