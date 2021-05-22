import { graphql } from "gatsby";
import { MDXRenderer } from "gatsby-plugin-mdx";
import React, {
  FunctionComponent,
  useCallback,
  useEffect,
  useRef,
} from "react";
import { useDispatch } from "react-redux";
import styled from "styled-components";
import { DocPageFragment } from "../../../graphql-types";
import ListAltIconSvg from "../../images/list-alt.svg";
import NewspaperIconSvg from "../../images/newspaper.svg";
import {
  DocPageDesktopGridColumns,
  IsDesktop,
  IsPhablet,
  IsSmallDesktop,
  IsTablet,
} from "../../shared-style";
import { useObservable } from "../../state";
import { toggleAside, toggleTOC } from "../../state/common";
import { Article } from "../articles/article";
import { ArticleComments } from "../articles/article-comments";
import {
  ArticleContent,
  ArticleHeader,
  ArticleTitle,
} from "../articles/article-elements";
import { ArticleSections } from "../articles/article-sections";
import { TabGroupProvider } from "../mdx/tabs/tab-groups";
import {
  ArticleWrapper,
  ArticleWrapperElement,
} from "./doc-page-article-wrapper";
import { Aside, DocPageAside } from "./doc-page-aside";
import { DocPageCommunity } from "./doc-page-community";
import { DocPageLegacy } from "./doc-page-legacy";
import { DocPageNavigation, Navigation } from "./doc-page-navigation";

interface DocPageProperties {
  readonly data: DocPageFragment;
  readonly originPath: string;
}

export const DocPage: FunctionComponent<DocPageProperties> = ({
  data,
  originPath,
}) => {
  const dispatch = useDispatch();
  const { fields, frontmatter, body } = data.file!.childMdx!;
  const slug = fields!.slug!.substring(1);
  const path = `/docs/${slug}`;
  const productAndVersionPattern = /^([\w-]*?)\/(v\d+)?/g;
  const result = productAndVersionPattern.exec(slug);
  const selectedProduct = result![1]! || "";
  const selectedVersion = (result && result[2]) || "";
  const title = frontmatter!.title!;
  const responsiveMenuRef = useRef<HTMLDivElement>(null);

  const hasScrolled$ = useObservable((state) => {
    return state.common.yScrollPosition > 20;
  });

  const handleToggleTOC = useCallback(() => {
    dispatch(toggleTOC());
  }, []);

  const handleToggleAside = useCallback(() => {
    dispatch(toggleAside());
  }, []);

  useEffect(() => {
    const classes = responsiveMenuRef.current?.className ?? "";

    const subscription = hasScrolled$.subscribe((hasScrolled) => {
      if (responsiveMenuRef.current) {
        responsiveMenuRef.current.className =
          classes + (hasScrolled ? " scrolled" : "");
      }
    });

    return () => {
      subscription.unsubscribe();
    };
  }, [hasScrolled$]);

  return (
    <TabGroupProvider>
      <Container>
        <DocPageNavigation
          data={data}
          selectedPath={path}
          selectedProduct={selectedProduct}
          selectedVersion={selectedVersion}
        />
        <ArticleWrapper>
          <ArticleContainer>
            <Article>
              {false && <DocPageLegacy />}
              <ArticleHeader kind="doc">
                <ResponsiveMenuWrapper>
                  <ResponsiveMenu ref={responsiveMenuRef}>
                    <Button onClick={handleToggleTOC} className="toc-toggle">
                      <ListAltIconSvg /> Table of contents
                    </Button>
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
                <MDXRenderer>{body}</MDXRenderer>
              </ArticleContent>
            </Article>
            {false && <ArticleComments data={data} path={path} title={title} />}
          </ArticleContainer>
        </ArticleWrapper>
        <DocPageAside>
          <DocPageCommunity data={data} originPath={originPath} />
          <ArticleSections data={data.file!.childMdx!} />
        </DocPageAside>
      </Container>
    </TabGroupProvider>
  );
};

export const DocPageGraphQLFragment = graphql`
  fragment DocPage on Query {
    file(
      sourceInstanceName: { eq: "docs" }
      relativePath: { eq: $originPath }
    ) {
      childMdx {
        fields {
          slug
        }
        frontmatter {
          title
        }
        body
        ...ArticleSections
      }
    }
    ...ArticleComments
    ...DocPageCommunity
    ...DocPageNavigation
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
  grid-column: 3;

  ${IsSmallDesktop(`
      grid-column: 1;
  `)};

  ${IsPhablet(`
    width: 100%;
    padding: 0;
  `)}
`;

const Container = styled.div`
  display: grid;
  ${DocPageDesktopGridColumns};
  ${IsSmallDesktop(`
    grid-template-columns: 250px 1fr;
    width: auto;
  `)}

  ${IsTablet(`
    grid-template-columns: 1fr;
  `)}

  grid-template-rows: 1fr;
  width: 100%;
  height: 100%;
  overflow: visible;

  ${Navigation} {
    grid-row: 1;
    grid-column: 2;

    ${IsSmallDesktop(`
      grid-column: 1;
    `)}
  }

  ${ArticleWrapperElement} {
    grid-row: 1;
    grid-column: 1 / 6;

    ${IsSmallDesktop(`
      grid-column: 2 / 5;
    `)}

    ${IsTablet(`
      grid-column: 1 / 5;
    `)}
  }

  ${Aside} {
    grid-row: 1;
    grid-column: 4;

    ${IsPhablet(`
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
  border-radius: 4px 4px 0 0;
  background: linear-gradient(
    180deg,
    #ffffff 30%,
    rgba(255, 255, 255, 0.75) 100%
  );
  transition: all 100ms linear 0s;

  &.scrolled {
    top: 60px;
  }

  ${IsPhablet(`
    left: 0;
    width: auto;
    right: 0;
    margin-left: 0;
    margin-right: 0;
    top: 60px;
  `)}

  ${IsDesktop(`
    display: none;
  `)}

  ${IsSmallDesktop(`
    > .toc-toggle {
      display: none;
    }
  `)}

  ${IsTablet(`
    > .toc-toggle {
      display: initial;
    }
  `)}
`;

const Button = styled.button`
  display: flex;
  flex-direction: row;
  align-items: center;
  color: #666;
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
    fill: #666;
    transition: fill 0.2s ease-in-out;
  }
`;
