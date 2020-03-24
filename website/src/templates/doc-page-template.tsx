import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { DocPageFragment } from "../../graphql-types";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";
import { DocPage } from "../components/widgets/doc-page";

interface DocPageTemplateProperties {
  data: DocPageFragment;
}

const DocPageTemplate: FunctionComponent<DocPageTemplateProperties> = ({
  data,
}) => {
  return (
    <Layout>
      <SEO title={data.markdownRemark!.frontmatter!.title!} />
      <DocPage data={data} />
    </Layout>
  );
};

export default DocPageTemplate;

export const pageQuery = graphql`
  query getDocPage($path: String!) {
    ...DocPage
  }
`;
