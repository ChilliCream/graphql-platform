import { graphql } from "gatsby";
import React, { FC } from "react";
import styled from "styled-components";

import { Link } from "@/components/misc";
import { THEME_COLORS } from "@/style";

export interface BlogArticleTagsProps {
  readonly tags: string[];
}

export const BlogArticleTags: FC<BlogArticleTagsProps> = ({ tags }) => {
  return (
    <>
      {tags.length > 0 && (
        <Tags>
          {tags.map((tag) => (
            <Tag key={tag}>
              <TagLink to={`/blog/tags/${tag}`} className="content-tag">
                {tag}
              </TagLink>
            </Tag>
          ))}
        </Tags>
      )}
    </>
  );
};

export const BlogArticleTagsGraphQLFragment = graphql`
  fragment BlogArticleTags on MdxFrontmatter {
    tags
  }
`;

const Tags = styled.ul`
  margin: 0 0 24px;
  list-style-type: none;
`;

const Tag = styled.li.attrs({
  className: "text-3",
})`
  display: inline-block;
  margin: 0 6px 6px 0;
  padding: 0;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--button-border-radius);
  color: ${THEME_COLORS.text};
  background-image: linear-gradient(
    to right bottom,
    #379dc83d,
    #2b80ad3d,
    #2263903d,
    #1a48743d,
    #112f573d
  );
`;

const TagLink = styled(Link)`
  display: block;
  padding: 4px 12px;
  color: ${THEME_COLORS.textContrast};
`;
