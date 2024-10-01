import { graphql } from "gatsby";
import React, { FC } from "react";

import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc/seo";
import { AllBlogPosts } from "@/components/widgets";
import { GetBlogArticlesQuery } from "@/graphql-types";

interface BlogArticlesTemplatePageContext {
  readonly limit: number;
  readonly skip: number;
  readonly numPages: number;
  readonly currentPage: number;
}

interface BlogArticlesTemplateProps {
  readonly data: GetBlogArticlesQuery;
  readonly pageContext: BlogArticlesTemplatePageContext;
}

const BlogArticlesTemplate: FC<BlogArticlesTemplateProps> = ({
  pageContext: { currentPage, numPages },
  data: { allMdx },
}) => {
  return (
    <SiteLayout disableStars>
      <SEO title="Blog Articles" />
      <AllBlogPosts
        data={allMdx}
        description="All the latest news about ChilliCream and its entire GraphQL Platform."
        currentPage={currentPage}
        totalPages={numPages}
        basePath="/blog"
      />
    </SiteLayout>
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
      ...AllBlogPosts
    }
  }
`;
