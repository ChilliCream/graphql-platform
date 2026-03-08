import React, { FC } from "react";
import styled from "styled-components";

import { THEME_COLORS } from "@/style";

interface BlogArticleTeaserMetadataData {
  fields?: {
    readingTime?: {
      text?: string;
    };
  };
  frontmatter?: {
    author?: string;
    authorImageUrl?: string;
    date?: string;
    title?: string;
  };
}

export interface BlogArticleTeaserMetadataProps {
  readonly data: BlogArticleTeaserMetadataData;
}

export const BlogArticleTeaserMetadata: FC<BlogArticleTeaserMetadataProps> = ({
  data: { fields, frontmatter },
}) => {
  return (
    <Metadata>
      <Author>
        <AuthorImage
          src={frontmatter?.authorImageUrl || ""}
          alt={frontmatter?.author ? `${frontmatter.author}'s avatar` : ""}
        />
        {frontmatter?.author || ""}
      </Author>
      <Space>
        <Title>{frontmatter?.title || ""}</Title>
        <Footer>
          {frontmatter?.date || ""}
          {" ・ "}
          {fields?.readingTime?.text || ""}
        </Footer>
      </Space>
    </Metadata>
  );
};

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

const AuthorImage = styled.img.attrs({
  width: 26,
  height: 26,
  loading: "lazy" as const,
  decoding: "async" as const,
})`
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

const Title = styled.div`
  font-weight: 700;
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
