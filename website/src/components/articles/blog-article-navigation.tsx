import { graphql } from "gatsby";
import React, { FC } from "react";
import styled from "styled-components";

import { ScrollContainer } from "@/components/article-elements";
import { IconContainer, Link } from "@/components/misc";
import { Icon } from "@/components/sprites";
import { BlogArticleNavigationFragment } from "@/graphql-types";
import { FONT_FAMILY_HEADING, THEME_COLORS } from "@/style";

// Icons
import Grid2IconSvg from "@/images/icons/grid-2.svg";
import {
  NavigationItem,
  NavigationLink,
  NavigationList,
  NavigationTitle,
} from "./article-navigation-elements";

export interface BlogArticleNavigationProps {
  readonly data: BlogArticleNavigationFragment;
  readonly selectedPath: string;
}

export const BlogArticleNavigation: FC<BlogArticleNavigationProps> = ({
  data,
  selectedPath,
}) => {
  const posts: Post[] =
    data.latestPosts.posts.map((post) => ({
      slug: post.fields?.slug ?? "",
      title: post.frontmatter?.title ?? "",
    })) ?? [];

  return (
    <>
      <Header>
        <BackButton to="/blog">
          Overview
          <IconContainer $size={14}>
            <Icon {...Grid2IconSvg} />
          </IconContainer>
        </BackButton>
      </Header>
      <ScrollContainer>
        <NavigationTitle>Latest Blog Posts</NavigationTitle>
        <NavigationList>
          {posts.map(({ slug, title }) => {
            return (
              <NavigationItem key={slug} active={selectedPath === slug}>
                <NavigationLink to={slug}>{title}</NavigationLink>
              </NavigationItem>
            );
          })}
        </NavigationList>
      </ScrollContainer>
    </>
  );
};

interface Post {
  readonly slug: string;
  readonly title: string;
}

export const BlogArticleNavigationGraphQLFragment = graphql`
  fragment BlogArticleNavigation on Query {
    latestPosts: allMdx(
      limit: 10
      filter: { frontmatter: { path: { regex: "//blog(/.*)?/" } } }
      sort: { order: DESC, fields: [frontmatter___date] }
    ) {
      posts: nodes {
        fields {
          slug
        }
        frontmatter {
          title
        }
      }
    }
  }
`;

const BackButton = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  box-sizing: border-box;
  border: 2px solid ${THEME_COLORS.primaryButtonBorder};
  border-radius: var(--button-border-radius);
  width: 100%;
  height: 38px;
  padding-right: 10px;
  padding-left: 10px;
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 0.875rem;
  font-weight: 500;
  color: ${THEME_COLORS.primaryButtonText};
  background-color: ${THEME_COLORS.primaryButton};
  transition: background-color 0.2s ease-in-out, color 0.2s ease-in-out;

  > ${IconContainer} {
    margin-left: auto;
    padding-left: 6px;

    > svg {
      fill: ${THEME_COLORS.primaryButtonText};
    }
  }

  :hover {
    color: ${THEME_COLORS.primaryButtonHoverText};
    background-color: ${THEME_COLORS.primaryButtonHover};

    > ${IconContainer} > svg {
      fill: ${THEME_COLORS.primaryButtonHoverText};
    }
  }
`;

const Header = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 6px 14px 20px 14px;

  @media only screen and (min-width: 1070px) {
    margin: 0 0 20px 0;
  }
`;
