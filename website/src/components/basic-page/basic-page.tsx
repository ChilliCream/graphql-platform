import { graphql } from "gatsby";
import { MDXRenderer } from "gatsby-plugin-mdx";
import React, { FC, useCallback, useEffect, useRef } from "react";
import { useDispatch } from "react-redux";
import styled, { css } from "styled-components";

import { Article } from "@/components/articles/article";
import { ArticleContentFooter } from "@/components/articles/article-content-footer";
import {
  ArticleContent,
  ArticleHeader,
  ArticleTitle,
  ScrollContainer,
} from "@/components/articles/article-elements";
import { ArticleSections } from "@/components/articles/article-sections";
import { TabGroupProvider } from "@/components/mdx/tabs";
import { BasicPageFragment } from "@/graphql-types";
import NewspaperIconSvg from "@/images/newspaper.svg";
import {
  BasicPageDesktopGridColumns,
  IsDesktop,
  IsPhablet,
  IsSmallDesktop,
  IsTablet,
  THEME_COLORS,
} from "@/shared-style";
import { useObservable } from "@/state";
import { toggleAside } from "@/state/common";
import {
  ArticleWrapper,
  ArticleWrapperElement,
} from "./basic-page-article-wrapper";
import { Aside, BasicPageAside } from "./basic-page-aside";

export interface BasicPageProps {
  readonly data: BasicPageFragment;
}

export const BasicPage: FC<BasicPageProps> = ({ data }) => {
  const dispatch = useDispatch();
  const responsiveMenuRef = useRef<HTMLDivElement>(null);

  const { fields, frontmatter, body } = data.file!.childMdx!;
  const title = frontmatter!.title!;
  const description = frontmatter!.description;

  const hasScrolled$ = useObservable((state) => {
    return state.common.yScrollPosition > 20;
  });

  const handleToggleAside = useCallback(() => {
    dispatch(toggleAside());
  }, []);

  useEffect(() => {
    const subscription = hasScrolled$.subscribe((hasScrolled) => {
      responsiveMenuRef.current?.classList.toggle("scrolled", hasScrolled);
    });

    return () => {
      subscription.unsubscribe();
    };
  }, [hasScrolled$]);

  return (
    <TabGroupProvider>
      <Container>
        <ArticleWrapper>
          <ArticleContainer>
            <Article>
              <ArticleHeader kind="basic">
                <ResponsiveMenuWrapper>
                  <ResponsiveMenu ref={responsiveMenuRef}>
                    <Button
                      onClick={handleToggleAside}
                      className="aside-toggle"
                    >
                      <NewspaperIconSvg /> About this article
                    </Button>
                  </ResponsiveMenu>
                </ResponsiveMenuWrapper>
                <ArticleTitle>{title}</ArticleTitle>
              </ArticleHeader>
              <ArticleContent>
                {description && <p>{description}</p>}
                <MDXRenderer>{body}</MDXRenderer>
                <ArticleContentFooter
                  lastUpdated={fields!.lastUpdated!}
                  lastAuthorName={fields!.lastAuthorName!}
                />
              </ArticleContent>
            </Article>
          </ArticleContainer>
        </ArticleWrapper>
        <BasicPageAside>
          <ScrollContainer>
            <ArticleSections data={data.file!.childMdx!} />
          </ScrollContainer>
        </BasicPageAside>
      </Container>
    </TabGroupProvider>
  );
};

export const BasicPageGraphQLFragment = graphql`
  fragment BasicPage on Query {
    file(
      sourceInstanceName: { eq: "basic" }
      relativePath: { eq: $originPath }
    ) {
      childMdx {
        fields {
          slug
          lastUpdated
          lastAuthorName
        }
        frontmatter {
          title
          description
        }
        body
        ...ArticleSections
      }
    }
  }
`;

const ResponsiveMenuWrapper = styled.div`
  position: absolute;
  left: 0;
  right: 0;
`;

const ArticleContainer = styled.div`
  padding: 20px;
  grid-row: 1;
  grid-column: 2;

  ${IsSmallDesktop(css`
    grid-column: 1;
  `)};

  ${IsPhablet(css`
    width: 100%;
    padding: 0;
  `)}
`;

const Container = styled.div`
  display: grid;

  ${BasicPageDesktopGridColumns};

  ${IsSmallDesktop(css`
    grid-template-columns: 1fr;
    width: auto;
  `)}

  ${IsTablet(css`
    grid-template-columns: 1fr;
  `)}

  grid-template-rows: 1fr;
  width: 100%;
  height: 100%;
  overflow: visible;

  ${ArticleWrapperElement} {
    grid-row: 1;
    grid-column: 1 / 5;

    ${IsSmallDesktop(css`
      grid-column: 2 / 4;
    `)}

    ${IsTablet(css`
      grid-column: 1 / 5;
    `)}
  }

  ${Aside} {
    grid-row: 1;
    grid-column: 3;

    ${IsPhablet(css`
      grid-column: 1;
    `)}
  }
`;

const ResponsiveMenu = styled.div`
  position: fixed;
  display: flex;
  z-index: 3;
  box-sizing: border-box;
  flex-direction: row;
  align-items: center;
  top: 80px;
  margin: 0 auto;
  width: 820px;
  height: 60px;
  padding: 0 20px;
  border-radius: var(--border-radius) var(--border-radius) 0 0;
  background: linear-gradient(
    180deg,
    #ffffff 30%,
    rgba(255, 255, 255, 0.75) 100%
  );
  transition: all 100ms linear 0s;

  &.scrolled {
    top: 60px;
    border-radius: 0;
  }

  ${IsPhablet(css`
    left: 0;
    width: auto;
    right: 0;
    margin: 0;
    top: 60px;
  `)}

  ${IsDesktop(css`
    display: none;
  `)}
`;

const Button = styled.button`
  display: flex;
  flex-direction: row;
  align-items: center;
  color: ${THEME_COLORS.text};
  transition: color 0.2s ease-in-out;

  &.aside-toggle {
    margin-left: auto;
  }

  &:hover {
    color: #000;

    > svg {
      fill: #000;
    }
  }

  > svg {
    margin-right: 5px;
    width: 16px;
    height: 16px;
    fill: ${THEME_COLORS.text};
    transition: fill 0.2s ease-in-out;
  }
`;
