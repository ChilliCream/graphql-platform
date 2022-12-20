import { graphql } from "gatsby";
import React, { FC } from "react";
import styled from "styled-components";

import { Link } from "@/components/misc/link";
import { THEME_COLORS } from "@/shared-style";

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
  margin: 0 20px 20px;
  list-style-type: none;

  @media only screen and (min-width: 860px) {
    margin: 0 50px 20px;
  }
`;

const Tag = styled.li`
  display: inline-block;
  margin: 0 5px 5px 0;
  border-radius: var(--border-radius);
  padding: 0;
  background-color: ${THEME_COLORS.primary};
  font-size: 0.722em;
  letter-spacing: 0.05em;
  color: ${THEME_COLORS.textContrast};
`;

const TagLink = styled(Link)`
  display: block;
  padding: 5px 15px;
  color: ${THEME_COLORS.textContrast};
`;
