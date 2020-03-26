import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { DocPageFragment } from "../../graphql-types";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";
import { DocPage } from "../components/widgets/doc-page";

interface DocPageTemplateProperties {
  data: DocPageFragment;
  pageContext: DocPageTemplatePageContext;
}

const DocPageTemplate: FunctionComponent<DocPageTemplateProperties> = ({
  data,
}) => {
  return (
    <Layout>
      <SEO title={data.file!.childMarkdownRemark!.frontmatter!.title!} />
      <DocPage data={data} />
    </Layout>
  );
};

export default DocPageTemplate;

export const pageQuery = graphql`
  query getDocPage($originPath: String!) {
    ...DocPage
  }
`;

export interface DocPageTemplatePageContext {
  path: string;
}
