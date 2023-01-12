import { SrOnly } from "@/components/misc/sr-only";
import { graphql } from "gatsby";
import React, { FC } from "react";

import { DocPage, useProductInformation } from "@/components/doc-page/doc-page";
import { Layout } from "@/components/layout";
import { SEO } from "@/components/misc/seo";
import { DocPageFragment } from "@/graphql-types";

export interface DocPageTemplateProps {
  readonly data: DocPageFragment;
  readonly pageContext: { originPath: string };
}

const DocPageTemplate: FC<DocPageTemplateProps> = ({ data, pageContext }) => {
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
    <Layout>
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
      <DocPage data={data} originPath={pageContext.originPath} />
    </Layout>
  );
};

export default DocPageTemplate;

export const pageQuery = graphql`
  query getDocPage($originPath: String!) {
    ...DocPage
  }
`;
