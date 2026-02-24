"use client";

import React, { useMemo } from "react";
import { MDXRemoteSerializeResult } from "next-mdx-remote";
import semverCoerce from "semver/functions/coerce";
import semverCompare from "semver/functions/compare";
import styled from "styled-components";

import {
  ArticleContent,
  ArticleHeader,
  ArticleTitle,
} from "@/components/article-elements";
import { ArticleLayout, SiteLayout } from "@/components/layout";
import { Link, SEO } from "@/components/misc";
import { THEME_COLORS } from "@/style";
import { ArticleContentFooter } from "@/components/articles/article-content-footer";
import { ArticleTableOfContent } from "@/components/articles/article-table-of-content";
import { DocArticleCommunity } from "@/components/articles/doc-article-community";
import { DocArticleNavigation } from "@/components/articles/doc-article-navigation";
import { ResponsiveArticleMenu } from "@/components/articles/responsive-article-menu";
import { MdxContent } from "./mdx-content";
import type { DocPage, DocsProduct } from "./docs";

interface DocPageViewProps {
  page: DocPage;
  mdxSource: MDXRemoteSerializeResult;
  docsConfig: DocsProduct[];
  slug: string[];
  headings: Array<{ depth: number; value: string }>;
}

export function DocPageView({
  page,
  mdxSource,
  docsConfig,
  slug,
  headings,
}: DocPageViewProps) {
  const productPath = slug[0];
  const versionPath =
    slug.length > 1 && /^v\d+/.test(slug[1]) ? slug[1] : "";
  const selectedPath = "/docs/" + slug.join("/");
  const title = page.frontmatter?.title || slug[slug.length - 1];
  const description = page.frontmatter?.description;

  const navData = { config: { products: docsConfig } };

  const product = useMemo(() => {
    const selectedProduct = docsConfig.find((p) => p.path === productPath);
    return {
      path: productPath,
      name: selectedProduct?.title ?? "",
      version: versionPath,
      stableVersion: selectedProduct?.latestStableVersion ?? "",
      description: selectedProduct?.metaDescription || null,
    };
  }, [docsConfig, productPath, versionPath]);

  return (
    <SiteLayout>
      <SEO title={title} description={page.frontmatter?.description} />
      <ArticleLayout
        navigation={
          <DocArticleNavigation
            data={navData}
            selectedPath={selectedPath}
            selectedProduct={productPath}
            selectedVersion={versionPath}
          />
        }
        aside={
          <>
            <DocArticleCommunity originPath={page.originPath} />
            <ArticleTableOfContent data={{ headings }} />
          </>
        }
      >
        <ArticleHeader>
          <ResponsiveArticleMenu />
          <DocumentationNotes product={product} slug={selectedPath} />
          <ArticleTitle>{title}</ArticleTitle>
        </ArticleHeader>
        <ArticleContent>
          {description && <p>{description}</p>}
          <MdxContent source={mdxSource} />
          <ArticleContentFooter
            lastUpdated={page.lastUpdated || ""}
            lastAuthorName={page.lastAuthorName || ""}
          />
        </ArticleContent>
      </ArticleLayout>
    </SiteLayout>
  );
}

// Version warning banner

interface ProductInformation {
  readonly path: string;
  readonly name: string | null;
  readonly version: string;
  readonly stableVersion: string;
  readonly description: string | null;
}

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

function DocumentationNotes({ product, slug }: DocumentationNotesProps) {
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
          See the <Link to={stableDocsUrl}>latest stable version</Link>{" "}
          instead.
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
}
