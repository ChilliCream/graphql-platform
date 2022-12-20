import { graphql } from "gatsby";
import React, { FC } from "react";
import styled from "styled-components";

import { Link } from "@/components/misc/link";
import { BlogArticleMetadataFragment } from "@/graphql-types";
import { THEME_COLORS } from "@/shared-style";

export interface BlogArticleMetadataProps {
  readonly data: BlogArticleMetadataFragment;
}

export const BlogArticleMetadata: FC<BlogArticleMetadataProps> = ({
  data: { fields, frontmatter },
}) => {
  return (
    <Metadata>
      <AuthorLink to={frontmatter!.authorUrl!}>
        <AuthorImage src={frontmatter!.authorImageUrl!} />
        {frontmatter!.author!}
      </AuthorLink>
      {frontmatter?.date && " ・ " + frontmatter.date}
      {fields?.readingTime?.text && " ・ " + fields.readingTime.text}
    </Metadata>
  );
};

export const BlogArticleMetadataGraphQLFragment = graphql`
  fragment BlogArticleMetadata on Mdx {
    fields {
      readingTime {
        text
      }
    }
    frontmatter {
      author
      authorImageUrl
      authorUrl
      date(formatString: "MMMM DD, YYYY")
    }
  }
`;

const Metadata = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 0 20px 20px;
  font-size: 0.778em;

  @media only screen and (min-width: 860px) {
    margin: 0 50px 20px;
  }
`;

const AuthorLink = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  color: ${THEME_COLORS.text};
`;

const AuthorImage = styled.img`
  flex: 0 0 auto;
  margin-right: 0.5em;
  border-radius: 15px;
  width: 30px;
`;
