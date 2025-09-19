import { graphql } from "gatsby";
import React, { FC } from "react";

import { DocArticle, useProductInformation } from "@/components/articles";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc/seo";
import { SrOnly } from "@/components/misc/sr-only";
import { DocArticleFragment } from "@/graphql-types";

export interface DocArticleTemplateProps {
  readonly data: DocArticleFragment;
  readonly pageContext: {
    readonly originPath: string;
  };
}

const DocArticleTemplate: FC<DocArticleTemplateProps> = ({
  data,
  pageContext,
}) => {
  const childMdx = data.file!.childMdx!;
  const documentTitle = data.file!.childMdx!.frontmatter!.title!;
  const description = data.file!.childMdx!.frontmatter!.description;
  const product = useProductInformation(
    childMdx.fields!.slug!,
    data.productsConfig?.products
  );

  let title = documentTitle;

  if (title && product && product.name) {
    title += " - " + product.name;

    if (product.version !== product.stableVersion) {
      title += " " + product.version;
    }
  }

  return (
    <SiteLayout disableStars>
      <SEO
        title={title}
        description={description || product?.description || undefined}
      />
      {product && (
        <>
          <SrOnly className="product-name">{product.name}</SrOnly>
          <SrOnly className="product-version">{product.version}</SrOnly>
        </>
      )}
      <DocArticle data={data} originPath={pageContext.originPath} />
    </SiteLayout>
  );
};

export default DocArticleTemplate;

export const pageQuery = graphql`
  query getDocArticle($originPath: String!) {
    ...DocArticle
  }
`;
