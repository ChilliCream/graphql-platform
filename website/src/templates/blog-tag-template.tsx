import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { GetBlogArticlesByTagQuery } from "../../graphql-types";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";
import { BlogArticles } from "../components/widgets/blog-articles";

interface BlogTagTemplateProperties {
  pageContext: {
    tag: string;
  };
  data: GetBlogArticlesByTagQuery;
}

const BlogTagTemplate: FunctionComponent<BlogTagTemplateProperties> = ({
  pageContext: { tag },
  data: { allMarkdownRemark },
}) => {
  return (
    <Layout>
      <SEO title={`Blog Articles By Tag: ${tag}`} />
      <BlogArticles data={allMarkdownRemark!} />
    </Layout>
  );
};

export default BlogTagTemplate;

export const pageQuery = graphql`
  query getBlogArticlesByTag($tag: String) {
    allMarkdownRemark(
      limit: 100
      filter: { frontmatter: { tags: { in: [$tag] } } }
      sort: { fields: [frontmatter___date], order: DESC }
    ) {
      ...BlogArticles
    }
  }
`;
