import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { GetBlogArticlesQuery } from "../../graphql-types";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";
import { BlogArticles } from "../components/widgets/blog-articles";

interface BlogArticlesTemplateProperties {
  pageContext: BlogArticlesTemplatePageContext;
  data: GetBlogArticlesQuery;
}

const BlogArticlesTemplate: FunctionComponent<BlogArticlesTemplateProperties> = ({
  pageContext: { currentPage, numPages },
  data: { allMdx },
}) => {
  return (
    <Layout>
      <SEO title="Blog Articles" />
      <BlogArticles
        currentPage={currentPage}
        data={allMdx!}
        totalPages={numPages}
      />
    </Layout>
  );
};

export default BlogArticlesTemplate;

export const pageQuery = graphql`
  query getBlogArticles($skip: Int!, $limit: Int!) {
    allMdx(
      limit: $limit
      skip: $skip
      filter: { frontmatter: { path: { glob: "/blog/**/*" } } }
      sort: { fields: [frontmatter___date], order: DESC }
    ) {
      ...BlogArticles
    }
  }
`;

export interface BlogArticlesTemplatePageContext {
  limit: number;
  skip: number;
  numPages: number;
  currentPage: number;
}
