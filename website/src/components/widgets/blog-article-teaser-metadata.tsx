import { graphql } from "gatsby";
import React, { FC } from "react";
import styled from "styled-components";

import { BlogArticleTeaserMetadataFragment } from "@/graphql-types";
import { THEME_COLORS } from "@/style";

export interface BlogArticleTeaserMetadataProps {
  readonly data: BlogArticleTeaserMetadataFragment;
}

export const BlogArticleTeaserMetadata: FC<BlogArticleTeaserMetadataProps> = ({
  data: { fields, frontmatter },
}) => {
  return (
    <Metadata>
      <Author>
        <AuthorImage src={frontmatter!.authorImageUrl!} />
        {frontmatter!.author!}
      </Author>
      <Space>
        <Title>{frontmatter!.title}</Title>
        <Footer>
          {frontmatter!.date!}
          {" ・ "}
          {fields!.readingTime!.text!}
        </Footer>
      </Space>
    </Metadata>
  );
};

export const BlogArticleTeaserMetadataGraphQLFragment = graphql`
  fragment BlogArticleTeaserMetadata on Mdx {
    fields {
      readingTime {
        text
      }
    }
    frontmatter {
      author
      authorImageUrl
      date(formatString: "MMMM DD, YYYY")
      title
    }
  }
`;

const Metadata = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  justify-content: space-between;
  margin: 28px 24px;
`;

const Author = styled.div.attrs({
  className: "text-3",
})`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  margin-bottom: 28px;
  color: ${THEME_COLORS.textAlt};
`;

const AuthorImage = styled.img`
  flex: 0 0 auto;
  margin-right: 8px;
  border-radius: 13px;
  width: 26px;
`;

const Space = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  justify-content: space-between;
`;

const Title = styled.h5`
  margin-bottom: 28px;
`;

const Footer = styled.div.attrs({
  className: "text-3",
})`
  display: flex;
  flex-direction: row;
  align-items: center;
  color: ${THEME_COLORS.textAlt};
`;
