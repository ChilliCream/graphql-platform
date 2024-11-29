import { graphql } from "gatsby";
import React, { FC } from "react";

import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc/seo";
import { AllBlogPosts } from "@/components/widgets";
import { GetBlogArticlesByTagQuery } from "@/graphql-types";

interface BlogTagTemplatePageContext {
  readonly tag: string;
  readonly limit: number;
  readonly skip: number;
  readonly numPages: number;
  readonly currentPage: number;
}

interface BlogTagTemplateProps {
  readonly data: GetBlogArticlesByTagQuery;
  readonly pageContext: BlogTagTemplatePageContext;
}

const BlogTagTemplate: FC<BlogTagTemplateProps> = ({
  pageContext: { tag, currentPage, numPages },
  data: { allMdx },
}) => {
  return (
    <SiteLayout disableStars>
      <SEO title={`Blog Articles By Tag: ${tag}`} />
      <AllBlogPosts
        data={allMdx}
        description={`Blog Articles By Tag: ${tag}`}
        currentPage={currentPage}
        totalPages={numPages}
        basePath={`/blog/tags/${tag}`}
      />
    </SiteLayout>
  );
};

export default BlogTagTemplate;

export const pageQuery = graphql`
  query getBlogArticlesByTag($tag: String, $skip: Int!, $limit: Int!) {
    allMdx(
      limit: $limit
      skip: $skip
      filter: { frontmatter: { tags: { in: [$tag] } } }
      sort: { fields: [frontmatter___date], order: DESC }
    ) {
      ...AllBlogPosts
    }
  }
`;
