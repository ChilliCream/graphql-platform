import { graphql } from "gatsby";
import React, { FC } from "react";
import { DocPageFragment } from "../../graphql-types";
import {
  DocPage,
  useProductInformation,
} from "../components/doc-page/doc-page";
import { Layout } from "../components/layout";
import { SEO } from "../components/misc/seo";

export interface DocPageTemplateProps {
  readonly data: DocPageFragment;
  readonly pageContext: { originPath: string };
}

const DocPageTemplate: FC<DocPageTemplateProps> = ({ data, pageContext }) => {
  const childMdx = data.file!.childMdx!;
  const documentTitle = data.file!.childMdx!.frontmatter!.title!;
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
      <SEO title={title} />
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
