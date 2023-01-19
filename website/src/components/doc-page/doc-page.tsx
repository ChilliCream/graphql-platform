import { graphql, Link } from "gatsby";
import { MDXRenderer } from "gatsby-plugin-mdx";
import React, { FC, useCallback, useEffect, useMemo, useRef } from "react";
import { useDispatch } from "react-redux";
import semverCoerce from "semver/functions/coerce";
import semverCompare from "semver/functions/compare";
import styled, { css } from "styled-components";

import { Article } from "@/components/articles/article";
import { ArticleComments } from "@/components/articles/article-comments";
import { ArticleContentFooter } from "@/components/articles/article-content-footer";
import {
  ArticleContent,
  ArticleHeader,
  ArticleTitle,
} from "@/components/articles/article-elements";
import { ArticleSections } from "@/components/articles/article-sections";
import { TabGroupProvider } from "@/components/mdx/tabs";
import { DocPageFragment, DocsJson, Maybe } from "@/graphql-types";
import {
  DocPageDesktopGridColumns,
  IsDesktop,
  IsPhablet,
  IsSmallDesktop,
  IsTablet,
  THEME_COLORS,
} from "@/shared-style";
import { useObservable } from "@/state";
import { toggleAside, toggleTOC } from "@/state/common";

// Icons
import ListAltIconSvg from "@/images/list-alt.svg";
import NewspaperIconSvg from "@/images/newspaper.svg";

import {
  ArticleWrapper,
  ArticleWrapperElement,
} from "./doc-page-article-wrapper";
import { Aside, DocPageAside } from "./doc-page-aside";
import { DocPageCommunity } from "./doc-page-community";
import { DocPageLegacy } from "./doc-page-legacy";
import {
  DocPageNavigation,
  Navigation,
  ScrollContainer,
} from "./doc-page-navigation";

export interface DocPageProps {
  readonly data: DocPageFragment;
  readonly originPath: string;
}

export const DocPage: FC<DocPageProps> = ({ data, originPath }) => {
  const dispatch = useDispatch();
  const responsiveMenuRef = useRef<HTMLDivElement>(null);

  const { fields, frontmatter, body } = data.file!.childMdx!;
  const slug = fields!.slug!;
  const title = frontmatter!.title!;
  const description = frontmatter!.description;

  const product = useProductInformation(slug, data.productsConfig?.products);

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
    const subscription = hasScrolled$.subscribe((hasScrolled) => {
      responsiveMenuRef.current?.classList.toggle("scrolled", hasScrolled);
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
          selectedProduct={product.path}
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
                <DocumentationNotes product={product} slug={slug} />
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
            {false && <ArticleComments data={data} path={slug} title={title} />}
          </ArticleContainer>
        </ArticleWrapper>
        <DocPageAside>
          <DocPageCommunity data={data} originPath={originPath} />
          <ScrollContainer>
            <ArticleSections data={data.file!.childMdx!} />
          </ScrollContainer>
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
          description
        }
        body
        ...ArticleSections
      }
    }
    productsConfig: file(
      sourceInstanceName: { eq: "docs" }
      relativePath: { eq: "docs.json" }
    ) {
      products: childrenDocsJson {
        path
        title
        description
        metaDescription
        latestStableVersion
      }
    }
    ...ArticleComments
    ...DocPageCommunity
    ...DocPageNavigation
  }
`;

const productAndVersionPattern = /^\/docs\/([\w-]+)(?:\/(v\d+))?/;

interface ProductInformation {
  readonly path: string;
  readonly name: string | null;
  readonly version: string;
  readonly stableVersion: string;
  readonly description: string | null;
}

type Product = Pick<
  DocsJson,
  "path" | "title" | "description" | "metaDescription" | "latestStableVersion"
>;

export function useProductInformation(
  slug: string,
  products: Maybe<Array<Maybe<Product>>> | undefined
): ProductInformation | null {
  if (!slug) {
    return null;
  }

  const result = productAndVersionPattern.exec(slug);

  if (!result) {
    return null;
  }

  const selectedPath = result[1] || "";
  const selectedVersion = result[2] || "";
  let stableVersion = "";

  const selectedProduct = products?.find((p) => p?.path === selectedPath);

  if (selectedProduct) {
    stableVersion = selectedProduct.latestStableVersion || "";
  }

  return {
    path: selectedPath,
    name: selectedProduct?.title ?? "",
    version: selectedVersion,
    stableVersion,
    description: selectedProduct?.metaDescription || null,
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

  ${DocPageDesktopGridColumns};

  ${IsSmallDesktop(css`
    grid-template-columns: 250px 1fr;
    width: auto;
  `)}

  ${IsTablet(css`
    grid-template-columns: 1fr;
  `)}

  grid-template-rows: 1fr;
  width: 100%;
  height: 100%;
  overflow: visible;

  ${Navigation} {
    grid-row: 1;
    grid-column: 2;

    ${IsSmallDesktop(css`
      grid-column: 1;
    `)}
  }

  ${ArticleWrapperElement} {
    grid-row: 1;
    grid-column: 1 / 6;

    ${IsSmallDesktop(css`
      grid-column: 2 / 5;
    `)}

    ${IsTablet(css`
      grid-column: 1 / 5;
    `)}
  }

  ${Aside} {
    grid-row: 1;
    grid-column: 4;

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

  ${IsSmallDesktop(css`
    > .toc-toggle {
      display: none;
    }
  `)}

  ${IsTablet(css`
    > .toc-toggle {
      display: initial;
    }
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

const DocumentationVersionWarning = styled.div`
  padding: 20px 20px;
  background-color: ${THEME_COLORS.warning};
  color: ${THEME_COLORS.textContrast};
  line-height: 1.4;

  > br {
    margin-bottom: 16px;
  }

  > a {
    color: white !important;
    font-weight: 600;
    text-decoration: underline;
  }

  @media only screen and (min-width: 860px) {
    padding: 20px 50px;
  }
`;

interface DocumentationNotesProps {
  readonly product: ProductInformation;
  readonly slug: string;
}

type DocumentationVersionType = "stable" | "experimental" | "outdated" | null;

const DocumentationNotes: FC<DocumentationNotesProps> = ({ product, slug }) => {
  const versionType = useMemo<DocumentationVersionType>(() => {
    const parsedCurrentVersion = semverCoerce(product.version);
    const parsedStableVersion = semverCoerce(product.stableVersion);

    if (parsedCurrentVersion && parsedStableVersion) {
      const curVersion = parsedCurrentVersion.version;
      const stableVersion = parsedStableVersion.version;

      const result = semverCompare(curVersion, stableVersion);

      if (result === 0) {
        return "stable";
      }

      if (result === 1) {
        return "experimental";
      }

      if (result === -1) {
        return "outdated";
      }
    }

    return null;
  }, [product.stableVersion, product.version]);

  if (versionType !== null) {
    const stableDocsUrl = slug.replace(
      "/" + product.version,
      "/" + product.stableVersion
    );

    if (versionType === "experimental") {
      return (
        <DocumentationVersionWarning>
          This is documentation for <strong>{product.version}</strong>, which is
          currently in preview.
          <br />
          See the <Link to={stableDocsUrl}>latest stable version</Link> instead.
        </DocumentationVersionWarning>
      );
    }

    if (versionType === "outdated") {
      return (
        <DocumentationVersionWarning>
          This is documentation for <strong>{product.version}</strong>, which is
          no longer actively maintained.
          <br />
          For up-to-date documentation, see the{" "}
          <Link to={stableDocsUrl}>latest stable version</Link>.
        </DocumentationVersionWarning>
      );
    }
  }

  return null;
};
