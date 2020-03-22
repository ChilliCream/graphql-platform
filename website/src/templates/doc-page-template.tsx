import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { GetDocPageQuery } from "../../graphql-types";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";
import { DocPage } from "../components/widgets/doc-page";

interface DocPageTemplateProperties {
  data: GetDocPageQuery;
}

const DocPageTemplate: FunctionComponent<DocPageTemplateProperties> = ({
  data: { markdownRemark, site },
}) => {
  const { fields, frontmatter, html } = markdownRemark!;

  return (
    <Layout>
      <SEO title={frontmatter!.title!} />
      <DocPage
        baseUrl={site!.siteMetadata!.baseUrl!}
        htmlContent={html!}
        path={frontmatter!.path!}
        readingTime={fields!.readingTime!.text!}
        title={frontmatter!.title!}
        twitterAuthor={site!.siteMetadata!.author!}
      />
    </Layout>
  );
};

export default DocPageTemplate;

export const pageQuery = graphql`
  query getDocPage($path: String!) {
    markdownRemark(frontmatter: { path: { eq: $path } }) {
      fields {
        readingTime {
          text
        }
      }
      frontmatter {
        path
        title
      }
      html
    }
    site {
      siteMetadata {
        author
        baseUrl
      }
    }
  }
`;
