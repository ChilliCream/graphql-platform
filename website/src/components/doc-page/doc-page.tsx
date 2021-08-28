import { graphql, Link } from "gatsby";
import { MDXRenderer } from "gatsby-plugin-mdx";
import React, { FC, useCallback, useEffect, useRef } from "react";
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
import { ArticleContentFooter } from "../articles/article-content-footer";
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

interface DocPageProps {
  readonly data: DocPageFragment;
  readonly originPath: string;
}

export const DocPage: FC<DocPageProps> = ({ data, originPath }) => {
  const dispatch = useDispatch();
  const responsiveMenuRef = useRef<HTMLDivElement>(null);

  const { fields, frontmatter, body } = data.file!.childMdx!;
  const slug = fields!.slug!;
  const title = frontmatter!.title!;

  const product = useProductInformation(slug);

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

  if (!product) {
    throw new Error(
      `Product information could not be parsed from slug: '${slug}'`
    );
  }

  return (
    <TabGroupProvider>
      <Container>
        <DocPageNavigation
          data={data}
          selectedPath={slug}
          selectedProduct={product.name}
          selectedVersion={product.version}
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
                <DocumentationNotes product={product} />
                <ArticleTitle>{title}</ArticleTitle>
              </ArticleHeader>
              <ArticleContent>
                <MDXRenderer>{body}</MDXRenderer>

                <ArticleContentFooter
                  lastUpdated={fields!.lastUpdated!}
                  lastAuthorName={fields!.lastAuthorName!}
                />
              </ArticleContent>
            </Article>
            {false && <ArticleComments data={data} path={slug} title={title} />}
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
          lastUpdated
          lastAuthorName
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

const productAndVersionPattern = /^\/docs\/([\w-]+)(?:\/(v\d+))?/;

interface ProductInformation {
  readonly name: string;
  readonly version: string;
}

function useProductInformation(slug: string): ProductInformation | null {
  if (!slug) {
    return null;
  }

  const result = productAndVersionPattern.exec(slug);

  if (!result) {
    return null;
  }

  return {
    name: result[1] || "",
    version: result[2] || "",
  };
}

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
  border-radius: var(--border-radius) var(--border-radius) 0 0;
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
  color: var(--text-color);
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
    fill: var(--text-color);
    transition: fill 0.2s ease-in-out;
  }
`;

const OutdatedDocumentationWarning = styled.div`
  padding: 20px 20px;
  background-color: var(--warning-color);
  color: var(--text-color-contrast);
  line-height: 1.4;

  > br {
    margin-bottom: 16px;
  }

  > a {
    color: white !important;
    font-weight: bold;
    text-decoration: underline;
  }

  @media only screen and (min-width: 820px) {
    padding: 20px 50px;
  }
`;

interface DocumentationNotesProps {
  readonly product: ProductInformation;
}

const DocumentationNotes: FC<DocumentationNotesProps> = ({ product }) => {
  if (product.version === "") {
    return null;
  }

  return (
    <OutdatedDocumentationWarning>
      This is documentation for <strong>{product.version}</strong>, which is no
      longer actively maintained.
      <br />
      For up-to-date documentation, see the{" "}
      <Link to={`/docs/${product.name}`}>latest version</Link>.
    </OutdatedDocumentationWarning>
  );
};
