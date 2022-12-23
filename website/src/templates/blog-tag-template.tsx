import { graphql } from "gatsby";
import React, { FC } from "react";

import { Layout } from "@/components/layout";
import { SEO } from "@/components/misc/seo";
import { BlogArticles } from "@/components/widgets";
import { GetBlogArticlesByTagQuery } from "@/graphql-types";

interface BlogTagTemplateProps {
  pageContext: {
    tag: string;
  };
  data: GetBlogArticlesByTagQuery;
}

const BlogTagTemplate: FC<BlogTagTemplateProps> = ({
  pageContext: { tag },
  data: { allMdx },
}) => {
  return (
    <Layout>
      <SEO title={`Blog Articles By Tag: ${tag}`} />
      <BlogArticles data={allMdx!} />
    </Layout>
  );
};

export default BlogTagTemplate;

export const pageQuery = graphql`
  query getBlogArticlesByTag($tag: String) {
    allMdx(
      limit: 100
      filter: { frontmatter: { tags: { in: [$tag] } } }
      sort: { fields: [frontmatter___date], order: DESC }
    ) {
      ...BlogArticles
    }
  }
`;
