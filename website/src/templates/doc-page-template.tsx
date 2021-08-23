import { graphql } from "gatsby";
import React, { FC } from "react";
import { DocPageFragment } from "../../graphql-types";
import { DocPage } from "../components/doc-page/doc-page";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";

interface DocPageTemplateProps {
  data: DocPageFragment;
  pageContext: { originPath: string };
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
