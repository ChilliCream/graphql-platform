import React, { FC } from "react";
import styled from "styled-components";

import { Link } from "@/components/misc/link";
import { THEME_COLORS } from "@/style";

interface BlogArticleMetadataData {
  fields?: {
    readingTime?: {
      text?: string;
    };
  };
  frontmatter?: {
    author?: string;
    authorImageUrl?: string;
    authorUrl?: string;
    date?: string;
  };
}

export interface BlogArticleMetadataProps {
  readonly data: BlogArticleMetadataData;
}

export const BlogArticleMetadata: FC<BlogArticleMetadataProps> = ({
  data: { fields, frontmatter },
}) => {
  return (
    <Metadata>
      <AuthorLink to={frontmatter?.authorUrl || "#"}>
        <AuthorImage src={frontmatter?.authorImageUrl || ""} />
        {frontmatter?.author || ""}
      </AuthorLink>
      {frontmatter?.date && " ・ " + frontmatter.date}
      {fields?.readingTime?.text && " ・ " + fields.readingTime.text}
    </Metadata>
  );
};

const Metadata = styled.div.attrs({
  className: "text-3",
})`
  display: flex;
  flex-direction: row;
  align-items: center;
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
