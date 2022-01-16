import { graphql } from "gatsby";
import React, { FC } from "react";
import { DocPageFragment } from "../../graphql-types";
import { DocPage } from "../components/doc-page/doc-page";
import { Layout } from "../components/layout";
import { SEO } from "../components/misc/seo";

export interface DocPageTemplateProps {
  readonly data: DocPageFragment;
  readonly pageContext: { originPath: string };
}

const DocPageTemplate: FC<DocPageTemplateProps> = ({ data, pageContext }) => {
  return (
    <Layout>
      <SEO title={data.file!.childMdx!.frontmatter!.title!} />
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
