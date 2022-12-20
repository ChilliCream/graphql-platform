import { graphql } from "gatsby";
import React, { FC } from "react";

import { Layout } from "@/components/layout";
import { SEO } from "@/components/misc/seo";
import { BlogArticles } from "@/components/widgets";
import { GetBlogArticlesQuery } from "@/graphql-types";

interface BlogArticlesTemplateProps {
  pageContext: BlogArticlesTemplatePageContext;
  data: GetBlogArticlesQuery;
}

const BlogArticlesTemplate: FC<BlogArticlesTemplateProps> = ({
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
