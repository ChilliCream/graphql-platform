import { Link } from "@/components/misc";
import React, { FC, ReactNode, useMemo } from "react";
import semverCoerce from "semver/functions/coerce";
import semverCompare from "semver/functions/compare";
import styled from "styled-components";

import {
  ArticleContent,
  ArticleHeader,
  ArticleTitle,
} from "@/components/article-elements";
import { ArticleLayout } from "@/components/layout";
import { THEME_COLORS } from "@/style";
import { ArticleContentFooter } from "./article-content-footer";
import { ArticleTableOfContent } from "./article-table-of-content";
import { DocArticleCommunity } from "./doc-article-community";
import { DocArticleLegacy } from "./doc-article-legacy";
import { DocArticleNavigation } from "./doc-article-navigation";
import { ResponsiveArticleMenu } from "./responsive-article-menu";

interface DocArticleMdx {
  fields?: {
    slug?: string;
    lastUpdated?: string;
    lastAuthorName?: string;
  };
  frontmatter?: {
    title?: string;
    description?: string;
  };
  headings?: Array<{
    depth?: number;
    value?: string;
  } | null>;
}

interface Product {
  path?: string;
  title?: string;
  description?: string;
  metaDescription?: string;
  latestStableVersion?: string;
}

interface DocArticleData {
  file?: {
    childMdx?: DocArticleMdx;
  };
  productsConfig?: {
    products?: Array<Product | null>;
  };
}

export interface DocArticleProps {
  readonly data: DocArticleData;
  readonly originPath: string;
  readonly content: ReactNode;
}

export const DocArticle: FC<DocArticleProps> = ({
  data,
  originPath,
  content,
}) => {
  const { fields, frontmatter } = data.file?.childMdx || {};
  const slug = fields?.slug || "";
  const title = frontmatter?.title || "";
  const description = frontmatter?.description;

  const product = useProductInformation(slug, data.productsConfig?.products);

  if (!product) {
    throw new Error(
      `Product information could not be parsed from slug: '${slug}'`
    );
  }

  return (
    <ArticleLayout
      navigation={
        <DocArticleNavigation
          data={data}
          selectedPath={slug}
          selectedProduct={product.path}
          selectedVersion={product.version}
        />
      }
      aside={
        <>
          <DocArticleCommunity originPath={originPath} />
          <ArticleTableOfContent data={data.file?.childMdx || {}} />
        </>
      }
    >
      {false && <DocArticleLegacy />}
      <ArticleHeader>
        <ResponsiveArticleMenu />
        <DocumentationNotes product={product} slug={slug} />
        <ArticleTitle>{title}</ArticleTitle>
      </ArticleHeader>
      <ArticleContent>
        {description && <p>{description}</p>}
        {content}
        <ArticleContentFooter
          lastUpdated={fields?.lastUpdated || ""}
          lastAuthorName={fields?.lastAuthorName || ""}
        />
      </ArticleContent>
    </ArticleLayout>
  );
};

const productAndVersionPattern = /^\/docs\/([\w-]+)(?:\/(v\d+))?/;

interface ProductInformation {
  readonly path: string;
  readonly name: string | null;
  readonly version: string;
  readonly stableVersion: string;
  readonly description: string | null;
}

export function useProductInformation(
  slug: string,
  products?: Array<Product | null>
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
